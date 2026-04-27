using CommunityToolkit.Maui.Extensions;
using NiceCleanApp.Pages.Controls;
using NiceCleanApp.Services;

namespace NiceCleanApp.Pages;

/// <summary>
/// Displays the list of clean-walk events and lets users create new ones or view details.
/// </summary>
public partial class EventsPage : ContentPage
{
    private readonly IClient _apiClient;
    private readonly IUserSession _userSession;

    // Full list kept in memory for client-side filtering
    private List<EventViewModel> _allViewModels = [];
    private EventStatus? _activeFilter = null;

    public EventsPage(IClient apiClient, IUserSession userSession)
    {
        InitializeComponent();
        _apiClient = apiClient;
        _userSession = userSession;
    }

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadEventsAsync();
    }

    // ──────────────────────────────────────────────
    // Data loading
    // ──────────────────────────────────────────────

    private async Task LoadEventsAsync()
    {
        SetBusy(true);
        try
        {
            // Fetch events and pins in parallel
            var eventsTask = _apiClient.EventAllAsync();
            var pinsTask   = _apiClient.PinAllAsync();
            await Task.WhenAll(eventsTask, pinsTask);

            var events = eventsTask.Result;
            var pinMap = pinsTask.Result.ToDictionary(p => p.Id);

            _allViewModels = events
                .OrderBy(e => e.EventStatus)
                .ThenBy(e => e.Date)
                .Select(e => BuildViewModel(e, pinMap))
                .ToList();

            ApplyFilter();
        }
        catch (ApiException ex) when (ex.StatusCode == 204)
        {
            // No events — empty state handled below
            _allViewModels = [];
            ApplyFilter();
        }
        catch (Exception ex)
        {
            await AppShell.DisplaySnackbarAsync($"Could not load events: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private static EventViewModel BuildViewModel(
        Event e,
        IReadOnlyDictionary<int, Pin> pinMap)
    {
        pinMap.TryGetValue(e.PinId, out var pin);

        var locationDisplay = pin?.LocationName is { Length: > 0 } name
            ? $"📍 {name}"
            : pin != null
                ? $"📍 {pin.Latitude:F4}, {pin.Longitude:F4}"
                : $"📍 Pin #{e.PinId}";

        var typeIcon = pin?.PollutionType switch
        {
            PollutionType.Plastic   => "🛢️",
            PollutionType.Glass     => "🍾",
            PollutionType.Furniture => "🪑",
            _                       => "⚠️"
        };

        return new EventViewModel
        {
            Event           = e,
            LocationDisplay = locationDisplay,
            TypeIcon        = typeIcon,
            DateDisplay     = $"🗓  {e.Date.LocalDateTime:dddd dd MMM yyyy · HH:mm}",
            HostDisplay     = $"👤 Host user #{e.HostUserId}",
            StatusLabel     = e.EventStatus.ToString(),
            StatusColor     = e.EventStatus switch
            {
                EventStatus.Pending  => Color.FromArgb("#FF9900"),
                EventStatus.Ongoing  => Color.FromArgb("#3B6D11"),
                EventStatus.Ended    => Color.FromArgb("#888888"),
                _                    => Color.FromArgb("#888888")
            }
        };
    }

    // ──────────────────────────────────────────────
    // Filtering
    // ──────────────────────────────────────────────

    private void ApplyFilter()
    {
        var filtered = _activeFilter is null
            ? _allViewModels
            : _allViewModels.Where(vm => vm.Event.EventStatus == _activeFilter).ToList();

        EmptyState.IsVisible        = filtered.Count == 0;
        EventsCollection.IsVisible  = filtered.Count > 0;
        EventsCollection.ItemsSource = filtered;
    }

    private void OnFilterAll(object? sender, EventArgs e)
    {
        _activeFilter = null;
        HighlightFilter(FilterAllBtn);
        ApplyFilter();
    }

    private void OnFilterPending(object? sender, EventArgs e)
    {
        _activeFilter = EventStatus.Pending;
        HighlightFilter(FilterPendingBtn);
        ApplyFilter();
    }

    private void OnFilterOngoing(object? sender, EventArgs e)
    {
        _activeFilter = EventStatus.Ongoing;
        HighlightFilter(FilterOngoingBtn);
        ApplyFilter();
    }

    private void HighlightFilter(Button active)
    {
        foreach (var btn in new[] { FilterAllBtn, FilterPendingBtn, FilterOngoingBtn })
        {
            btn.BackgroundColor = btn == active
                ? Color.FromArgb("#3B6D11")
                : Colors.Transparent;

            btn.SetAppThemeColor(Button.TextColorProperty,
                light: btn == active ? Colors.White : Color.FromArgb("#333333"),
                dark:  btn == active ? Colors.White : Color.FromArgb("#EEEEEE"));
        }
    }

    // ──────────────────────────────────────────────
    // Event tap → detail popup
    // ──────────────────────────────────────────────

    private async void OnEventTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not EventViewModel vm) return;

        var currentUserId = _userSession.CurrentUser?.Id ?? 0;
        var popup = new EventDetailPopup(vm.Event, _apiClient, currentUserId);
        this.ShowPopup(popup);
        var changed = await popup.Result;

        if (changed)
            await LoadEventsAsync();
    }

    // ──────────────────────────────────────────────
    // Create event → popup
    // ──────────────────────────────────────────────

    private async void OnCreateEventClicked(object? sender, EventArgs e)
    {
        var currentUserId = _userSession.CurrentUser?.Id ?? 0;
        if (currentUserId == 0)
        {
            await AppShell.DisplaySnackbarAsync("Error: not logged in.");
            return;
        }

        List<Pin> pins;
        try
        {
            pins = (await _apiClient.PinAllAsync())
                .Where(p => p.Status != PinStatus.Cleaned && p.Status != PinStatus.Deleted)
                .OrderBy(p => p.LocationName)
                .ToList();
        }
        catch
        {
            pins = [];
        }

        if (pins.Count == 0)
        {
            await DisplayAlertAsync(
                "No pins available",
                "There are no active pollution pins to create an event for. Report a pollution spot first!",
                "OK");
            return;
        }

        var popup = new CreateEventPopup(pins, currentUserId, _apiClient);
        this.ShowPopup(popup);
        var created = await popup.Result;

        if (created)
        {
            await AppShell.DisplaySnackbarAsync("Event created! 🎉");
            await LoadEventsAsync();
        }
    }

    // ──────────────────────────────────────────────
    // Navigation
    // ──────────────────────────────────────────────

    private async void OnBackClicked(object? sender, EventArgs e)
        => await Navigation.PopModalAsync();

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private void SetBusy(bool busy)
    {
        BusyIndicator.IsRunning = busy;
        BusyIndicator.IsVisible = busy;
        if (busy)
        {
            EmptyState.IsVisible       = false;
            EventsCollection.IsVisible = false;
        }
    }
}

// ──────────────────────────────────────────────────
// View-model (display-only, no INotifyPropertyChanged needed)
// ──────────────────────────────────────────────────

public sealed class EventViewModel
{
    public Event  Event           { get; init; } = null!;
    public string LocationDisplay { get; init; } = string.Empty;
    public string TypeIcon        { get; init; } = string.Empty;
    public string DateDisplay     { get; init; } = string.Empty;
    public string HostDisplay     { get; init; } = string.Empty;
    public string StatusLabel     { get; init; } = string.Empty;
    public Color  StatusColor     { get; init; } = Colors.Gray;
}
