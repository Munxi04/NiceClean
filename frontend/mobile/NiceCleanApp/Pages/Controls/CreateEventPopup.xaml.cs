using CommunityToolkit.Maui.Views;
using NiceCleanApp.Services;

namespace NiceCleanApp.Pages.Controls;

/// <summary>
/// Popup that lets the current user organise a clean-walk event at an existing pollution pin.
/// </summary>
public partial class CreateEventPopup : Popup
{
    private readonly IClient _apiClient;
    private readonly int _hostUserId;
    private readonly IReadOnlyList<Pin> _pins;

    private readonly TaskCompletionSource<bool> _tcs = new();

    /// <summary>Awaitable result — true when an event was successfully created.</summary>
    public Task<bool> Result => _tcs.Task;

    public CreateEventPopup(IReadOnlyList<Pin> availablePins, int hostUserId, IClient apiClient)
    {
        InitializeComponent();
        _apiClient   = apiClient;
        _hostUserId  = hostUserId;
        _pins = availablePins;

        PopulatePinPicker();

        // Default to tomorrow at 10:00
        EventDatePicker.Date = DateTime.Today.AddDays(1);
        EventTimePicker.Time = new TimeSpan(10, 0, 0);
    }

    // ──────────────────────────────────────────────
    // Setup
    // ──────────────────────────────────────────────

    private void PopulatePinPicker()
    {
        foreach (var pin in _pins)
        {
            var label = string.IsNullOrWhiteSpace(pin.LocationName)
                ? $"{pin.PollutionType} at {pin.Latitude:F4}, {pin.Longitude:F4}"
                : $"{pin.LocationName} - {pin.PollutionType}";

            PinPicker.Items.Add(label);
        }

        if (_pins.Count > 0)
            PinPicker.SelectedIndex = 0;
    }

    // ──────────────────────────────────────────────
    // Button handlers
    // ──────────────────────────────────────────────

    private async void OnCreateClicked(object? sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        if (PinPicker.SelectedIndex < 0)
        {
            ShowError("Please select a pollution spot.");
            return;
        }

        var selectedPin = _pins[PinPicker.SelectedIndex];
        var startTime = EventDatePicker.Date + EventTimePicker.Time;

        if (startTime <= DateTime.Now)
        {
            ShowError("Please choose a future date and time.");
            return;
        }

        SetBusy(true);
        try
        {
            if ( startTime != null)
            {
                await _apiClient.EventPOSTAsync(new EventCreateDto
                {
                    PinId = selectedPin.Id,
                    HostUserId = _hostUserId,
                    StartTime = new DateTimeOffset((DateTime)startTime, TimeZoneInfo.Local.GetUtcOffset((DateTime)startTime))
                });

                _tcs.SetResult(true);
            }
            else
            {
                ShowError("Invalid date/time.");
            }
            _ = CloseAsync();
        }
        catch (ApiException<ProblemDetails> ex)
        {
            ShowError(ex.Result?.Detail ?? "Could not create event.");
        }
        catch (ApiException ex)
        {
            ShowError($"Server error ({ex.StatusCode}).");
        }
        catch (Exception ex)
        {
            ShowError($"Unexpected error: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void OnCloseClicked(object? sender, EventArgs e)
    {
        _tcs.TrySetResult(false);
        _ = CloseAsync();
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private void ShowError(string message)
    {
        ErrorLabel.Text      = message;
        ErrorLabel.IsVisible = true;
    }

    private void SetBusy(bool busy)
    {
        CreateButton.IsEnabled  = !busy;
        BusyIndicator.IsRunning = busy;
        BusyIndicator.IsVisible = busy;
    }
}
