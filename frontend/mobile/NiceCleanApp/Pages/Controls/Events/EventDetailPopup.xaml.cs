using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using NiceCleanApp.Services;

namespace NiceCleanApp.Pages.Controls;

/// <summary>
/// Shows full details for a single event.
/// Participants can join/leave; the host can advance status
/// (Pending → Ongoing → Ended). On ending, a <see cref="EventReportPopup"/>
/// is shown and the result is submitted via <c>POST api/Report</c>.
/// </summary>
public partial class EventDetailPopup : Popup
{
    private readonly EventResponseDto _event;
    private readonly IClient _apiClient;
    private readonly int _currentUserId;

    private readonly TaskCompletionSource<bool> _tcs = new();

    /// <summary>Awaitable result — true when a change was made.</summary>
    public Task<bool> Result => _tcs.Task;

    public EventDetailPopup(EventResponseDto @event, IClient apiClient, int currentUserId)
    {
        InitializeComponent();
        _event = @event;
        _apiClient = apiClient;
        _currentUserId = currentUserId;

        _ = PopulateAsync();
    }

    // ──────────────────────────────────────────────
    // Population
    // ──────────────────────────────────────────────

    private async Task PopulateAsync()
    {
        TitleLabel.Text = $"Event #{_event.EventId}";

        (StatusDot.Color, StatusLabel.Text, var headerColor) = _event.EventStatus switch
        {
            EventStatus.Pending => (Color.FromArgb("#FF9900"), "Pending", Color.FromArgb("#CC7700")),
            EventStatus.Ongoing => (Color.FromArgb("#3B6D11"), "Ongoing", Color.FromArgb("#3B6D11")),
            EventStatus.Ended => (Color.FromArgb("#888888"), "Ended", Color.FromArgb("#666666")),
            _ => (Color.FromArgb("#888888"), "Unknown", Color.FromArgb("#666666"))
        };
        HeaderGrid.BackgroundColor = headerColor;

        DateLabel.Text = _event.Date.LocalDateTime.ToString("dddd d MMMM yyyy · HH:mm");
        HostLabel.Text = $"Host {_event.HostNickname}";
        ParticipantsLabel.Text = $"Participants {_event.ParticipantCount}";

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
            SubtitleLabel.Text = $"Pin #{_event.PinId}";
            LocationLabel.Text = $"Pin #{_event.PinId}";
            PollutionLabel.Text = "—";
        }

        await ConfigureActionButtonsAsync();
    }

    private async Task ConfigureActionButtonsAsync()
    {
        bool isHost = _currentUserId == _event.HostUserId;
        bool isEnded = _event.EventStatus == EventStatus.Ended;
        bool isParticipant = await _apiClient.HasJoinedAsync(_event.EventId, _currentUserId);

        JoinButton.IsVisible = !isHost && !isEnded && !isParticipant;
        LeaveButton.IsVisible = !isHost && !isEnded && isParticipant;

        HostActions.IsVisible = isHost && !isEnded;
        StartButton.IsVisible = isHost && _event.EventStatus == EventStatus.Pending;
        EndButton.IsVisible = isHost && _event.EventStatus == EventStatus.Ongoing;
    }

    // ──────────────────────────────────────────────
    // Participant: join / leave
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
            await ConfigureActionButtonsAsync();
            _tcs.TrySetResult(true);
        }
        catch (ApiException<ProblemDetails> ex) { ShowFeedback(ex.Result?.Detail ?? "Could not join event.", isError: true); }
        catch (ApiException ex) { ShowFeedback($"Server error ({ex.StatusCode}).", isError: true); }
        catch (Exception ex) { ShowFeedback($"Unexpected error: {ex.Message}", isError: true); }
        finally { SetBusy(false); }
    }

    private async void OnLeaveClicked(object? sender, EventArgs e)
    {
        SetBusy(true);
        try
        {
            await _apiClient.RemoveAsync(_event.EventId, _currentUserId);
            ShowFeedback("You've left the event.", isError: false);
            await ConfigureActionButtonsAsync();
            _tcs.TrySetResult(true);
        }
        catch (ApiException<ProblemDetails> ex) { ShowFeedback(ex.Result?.Detail ?? "Could not leave event.", isError: true); }
        catch (ApiException ex) { ShowFeedback($"Server error ({ex.StatusCode}).", isError: true); }
        catch (Exception ex) { ShowFeedback($"Unexpected error: {ex.Message}", isError: true); }
        finally { SetBusy(false); }
    }

    // ──────────────────────────────────────────────
    // Host: start
    // ──────────────────────────────────────────────

    private async void OnStartClicked(object? sender, EventArgs e)
    {
        SetBusy(true);
        try
        {
            await _apiClient.StatusAsync(_event.EventId, EventStatus.Ongoing, _currentUserId);
            ShowFeedback("Event is now live! 🚀", isError: false);
            StartButton.IsVisible = false;
            EndButton.IsVisible = true;
            StatusDot.Color = Color.FromArgb("#3B6D11");
            StatusLabel.Text = "Ongoing";
            _tcs.TrySetResult(true);
        }
        catch (ApiException<ProblemDetails> ex) { ShowFeedback(ex.Result?.Detail ?? "Could not start event.", isError: true); }
        catch (ApiException ex) { ShowFeedback($"Server error ({ex.StatusCode}).", isError: true); }
        catch (Exception ex) { ShowFeedback($"Unexpected error: {ex.Message}", isError: true); }
        finally { SetBusy(false); }
    }

    // ──────────────────────────────────────────────
    // Host: end  →  report popup  →  POST api/Report
    // ──────────────────────────────────────────────

    private async void OnEndClicked(object? sender, EventArgs e)
    {
        SetBusy(true);
        try
        {
            // Mark the event as Ended.
            await _apiClient.StatusAsync(_event.EventId, EventStatus.Ended, _currentUserId);

            StartButton.IsVisible = false;
            EndButton.IsVisible = false;
            HostActions.IsVisible = false;
            StatusDot.Color = Color.FromArgb("#888888");
            StatusLabel.Text = "Ended";
            _tcs.TrySetResult(true);
        }
        catch (ApiException<ProblemDetails> ex) { ShowFeedback(ex.Result?.Detail ?? "Could not end event.", isError: true); SetBusy(false); return; }
        catch (ApiException ex) { ShowFeedback($"Server error ({ex.StatusCode}).", isError: true); SetBusy(false); return; }
        catch (Exception ex) { ShowFeedback($"Unexpected error: {ex.Message}", isError: true); SetBusy(false); return; }
        finally { SetBusy(false); }

        // Close this popup, then show the report popup on top of the current page.
        _ = CloseAsync();

        var reportPopup = new EventReportPopup();
        Shell.Current.CurrentPage.ShowPopup(reportPopup);
        var report = await reportPopup.Result;

        // If the host filled in the report, POST it via api/Report.
        if (report is not null)
        {
            try
            {
                await _apiClient.ReportAsync(new ReportCreateDto
                {
                    EventId = _event.EventId,
                    NumberOfBags = report.NumberOfBags,
                    BagVolume = report.BagVolume
                });

                await AppShell.DisplaySnackbarAsync(
                    $"🌿 Report saved — {report.NumberOfBags} " +
                    $"bag{(report.NumberOfBags == 1 ? "" : "s")} " +
                    $"({report.BagVolume}). Amazing work!");
            }
            catch (ApiException<ProblemDetails> ex)
            {
                await AppShell.DisplaySnackbarAsync(
                    $"Event ended but report failed: {ex.Result?.Detail ?? ex.Response}");
            }
            catch (Exception ex)
            {
                await AppShell.DisplaySnackbarAsync(
                    $"Event ended but report failed: {ex.Message}");
            }
        }
        else
        {
            await AppShell.DisplaySnackbarAsync("Event ended. Great job cleaning up! 🌿");
        }
    }

    private async void OnReportClicked(object? sender, EventArgs e)
    {
        var reportPopup = new EventReportPopup();
        Shell.Current.CurrentPage.ShowPopup(reportPopup);
        var report = await reportPopup.Result;
        await ConfigureActionButtonsAsync();

        if (report is not null)
        {
            try
            {
                await _apiClient.ReportAsync(new ReportCreateDto
                {
                    EventId = _event.EventId,
                    NumberOfBags = report.NumberOfBags,
                    BagVolume = report.BagVolume
                });

                await AppShell.DisplaySnackbarAsync(
                    $"🌿 Report saved — {report.NumberOfBags} " +
                    $"bag{(report.NumberOfBags == 1 ? "" : "s")} " +
                    $"({report.BagVolume}). Amazing work!");
            }
            catch (ApiException<ProblemDetails> ex)
            {
                await AppShell.DisplaySnackbarAsync(
                    $"Event ended but report failed: {ex.Result?.Detail ?? ex.Response}");
            }
            catch (Exception ex)
            {
                await AppShell.DisplaySnackbarAsync(
                    $"Event ended but report failed: {ex.Message}");
            }
        }
        else
        {
            await AppShell.DisplaySnackbarAsync("Event ended. Great job cleaning up! 🌿");
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
        FeedbackLabel.Text = message;
        FeedbackLabel.TextColor = isError
            ? Color.FromArgb("#E53935")
            : Color.FromArgb("#3B6D11");
        FeedbackLabel.IsVisible = true;
    }

    private void SetBusy(bool busy)
    {
        JoinButton.IsEnabled = !busy;
        StartButton.IsEnabled = !busy;
        EndButton.IsEnabled = !busy;
        BusyIndicator.IsRunning = busy;
        BusyIndicator.IsVisible = busy;
    }
}
