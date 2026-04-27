using CommunityToolkit.Maui.Views;
using NiceCleanApp.Services;

namespace NiceCleanApp.Pages.Controls;

/// <summary>
/// Shows full details for a single event.
/// Participants can join; the host can advance the event status (Pending→Ongoing→Ended).
/// </summary>
public partial class EventDetailPopup : Popup
{
    private readonly Event _event;
    private readonly IClient _apiClient;
    private readonly int _currentUserId;

    private readonly TaskCompletionSource<bool> _tcs = new();

    /// <summary>Awaitable result — true when a change was made (join / status update).</summary>
    public Task<bool> Result => _tcs.Task;

    public EventDetailPopup(Event @event, IClient apiClient, int currentUserId)
    {
        InitializeComponent();
        _event         = @event;
        _apiClient     = apiClient;
        _currentUserId = currentUserId;

        _ = PopulateAsync();
    }

    // ──────────────────────────────────────────────
    // Population
    // ──────────────────────────────────────────────

    private async Task PopulateAsync()
    {
        // Header
        TitleLabel.Text = $"Event #{_event.EventId}";

        // Status
        (StatusDot.Color, StatusLabel.Text, var headerColor) = _event.EventStatus switch
        {
            EventStatus.Pending  => (Color.FromArgb("#FF9900"), "Pending",  Color.FromArgb("#CC7700")),
            EventStatus.Ongoing  => (Color.FromArgb("#3B6D11"), "Ongoing",  Color.FromArgb("#3B6D11")),
            EventStatus.Ended    => (Color.FromArgb("#888888"), "Ended",    Color.FromArgb("#666666")),
            _                    => (Color.FromArgb("#888888"), "Unknown",  Color.FromArgb("#666666"))
        };
        HeaderGrid.BackgroundColor = headerColor;

        DateLabel.Text = _event.Date.LocalDateTime.ToString("dddd d MMMM yyyy · HH:mm");
        HostLabel.Text = $"User #{_event.HostUserId}";

        // Fetch pin details
        try
        {
            var pin = await _apiClient.PinGETAsync(_event.PinId);
            SubtitleLabel.Text = string.IsNullOrWhiteSpace(pin.LocationName)
                ? $"{pin.Latitude:F5}, {pin.Longitude:F5}"
                : pin.LocationName;

            LocationLabel.Text = string.IsNullOrWhiteSpace(pin.LocationName)
                ? $"{pin.Latitude:F4}, {pin.Longitude:F4}"
                : pin.LocationName;

            PollutionLabel.Text = $"{pin.PollutionType} · {pin.Severity}";
        }
        catch
        {
            SubtitleLabel.Text  = $"Pin #{_event.PinId}";
            LocationLabel.Text  = $"Pin #{_event.PinId}";
            PollutionLabel.Text = "—";
        }

        ConfigureActionButtons();
    }

    private void ConfigureActionButtons()
    {
        bool isHost   = _currentUserId == _event.HostUserId;
        bool isEnded  = _event.EventStatus == EventStatus.Ended;

        // ── Participant: show Join if event is active, user is not host and event is joinable ──
        JoinButton.IsVisible = !isHost && !isEnded; // TODO: also check if user is already a participant (requires extra API call or data in event)

        // ── Host controls ──
        HostActions.IsVisible = isHost && !isEnded;
        StartButton.IsVisible = isHost && _event.EventStatus == EventStatus.Pending;
        EndButton.IsVisible   = isHost && _event.EventStatus == EventStatus.Ongoing;
    }

    // ──────────────────────────────────────────────
    // Participant: join
    // ──────────────────────────────────────────────

    private async void OnJoinClicked(object? sender, EventArgs e)
    {
        SetBusy(true);
        try
        {
            await _apiClient.JoinAsync(_event.EventId, new ParticipationDto
            {
                UserId = _currentUserId
            });

            ShowFeedback("You're in! See you at the clean walk 🌿", isError: false);
            JoinButton.IsVisible = false;
            _tcs.TrySetResult(true);
        }
        catch (ApiException<ProblemDetails> ex)
        {
            ShowFeedback(ex.Result?.Detail ?? "Could not join event.", isError: true);
        }
        catch (ApiException ex)
        {
            ShowFeedback($"Server error ({ex.StatusCode}).", isError: true);
        }
        catch (Exception ex)
        {
            ShowFeedback($"Unexpected error: {ex.Message}", isError: true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    // ──────────────────────────────────────────────
    // Host: start / end
    // ──────────────────────────────────────────────

    private async void OnStartClicked(object? sender, EventArgs e)
        => await UpdateStatusAsync(EventStatus.Ongoing);

    private async void OnEndClicked(object? sender, EventArgs e)
        => await UpdateStatusAsync(EventStatus.Ended);

    private async Task UpdateStatusAsync(EventStatus newStatus)
    {
        SetBusy(true);
        try
        {
            await _apiClient.StatusAsync(_event.EventId, newStatus, _currentUserId);

            var message = newStatus == EventStatus.Ongoing
                ? "Event is now live! 🚀"
                : "Event ended. Thanks for cleaning up! 🌱";

            ShowFeedback(message, isError: false);

            // Hide host controls — status changed
            StartButton.IsVisible = false;
            EndButton.IsVisible   = false;
            StatusDot.Color       = newStatus == EventStatus.Ongoing
                ? Color.FromArgb("#3B6D11")
                : Color.FromArgb("#888888");
            StatusLabel.Text = newStatus.ToString();

            _tcs.TrySetResult(true);
        }
        catch (ApiException<ProblemDetails> ex)
        {
            ShowFeedback(ex.Result?.Detail ?? "Could not update status.", isError: true);
        }
        catch (ApiException ex)
        {
            ShowFeedback($"Server error ({ex.StatusCode}).", isError: true);
        }
        catch (Exception ex)
        {
            ShowFeedback($"Unexpected error: {ex.Message}", isError: true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    // ──────────────────────────────────────────────
    // Close
    // ──────────────────────────────────────────────

    private void OnCloseClicked(object? sender, EventArgs e)
    {
        _tcs.TrySetResult(false);
        _ = CloseAsync();
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private void ShowFeedback(string message, bool isError)
    {
        FeedbackLabel.Text      = message;
        FeedbackLabel.TextColor = isError
            ? Color.FromArgb("#E53935")
            : Color.FromArgb("#3B6D11");
        FeedbackLabel.IsVisible = true;
    }

    private void SetBusy(bool busy)
    {
        JoinButton.IsEnabled    = !busy;
        StartButton.IsEnabled   = !busy;
        EndButton.IsEnabled     = !busy;
        BusyIndicator.IsRunning = busy;
        BusyIndicator.IsVisible = busy;
    }
}
