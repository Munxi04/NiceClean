using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using NiceCleanApp.Services;

namespace NiceCleanApp.Pages.Controls;

/// <summary>
/// Shows full details for a single event.
/// Participants can join/leave; the host can advance status (Pending → Ongoing → Ended).
/// </summary>
public partial class EventDetailPopup : Popup
{
    private readonly EventResponseDto _event;
    private readonly IClient _apiClient;
    private readonly int _currentUserId;

    private readonly TaskCompletionSource<bool> _tcs = new();

    /// <summary>Awaitable result — <c>true</c> when a change was made (join / leave / status update).</summary>
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
            _ => (Color.FromArgb("#888888"), "Ended", Color.FromArgb("#666666"))
        };
        HeaderGrid.BackgroundColor = headerColor;

        DateLabel.Text = _event.Date.LocalDateTime.ToString("dddd d MMMM yyyy · HH:mm");
        HostLabel.Text = _event.HostNickname;
        ParticipantsLabel.Text = _event.ParticipantCount.ToString();

        // Fetch pin details
        try
        {
            var pin = await _apiClient.PinGETAsync(_event.PinId);

            var locationName = string.IsNullOrWhiteSpace(pin.LocationName)
                ? $"{pin.Latitude:F5}, {pin.Longitude:F5}"
                : pin.LocationName;

            SubtitleLabel.Text = locationName;
            LocationLabel.Text = locationName;
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

    // ──────────────────────────────────────────────
    // Button visibility
    // ──────────────────────────────────────────────

    private async Task ConfigureActionButtonsAsync()
    {
        bool isHost = _currentUserId == _event.HostUserId;
        bool isEnded = _event.EventStatus == EventStatus.Ended;
        bool isParticipant = await _apiClient.HasJoinedAsync(_event.EventId, _currentUserId);
        bool isReported = false; //TODO: await _apiClient.HasReportAsync(_event.EventId);

        JoinButton.IsVisible = !isHost && !isEnded && !isParticipant;
        LeaveButton.IsVisible = !isHost && !isEnded && isParticipant;

        HostActions.IsVisible = isHost && !isReported;
        StartButton.IsVisible = isHost && _event.EventStatus == EventStatus.Pending;
        EndButton.IsVisible = isHost && _event.EventStatus == EventStatus.Ongoing;
        ReportButton.IsVisible = isHost && isEnded && !isReported;
    }

    // ──────────────────────────────────────────────
    // Participant: join / leave
    // ──────────────────────────────────────────────

    private async void OnJoinClicked(object? sender, EventArgs e)
    {
        SetBusy(true);
        try
        {
            await _apiClient.JoinAsync(_event.EventId, new ParticipationDto { UserId = _currentUserId });
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
    // Host: start / end / report
    // ──────────────────────────────────────────────

    private async void OnStartClicked(object? sender, EventArgs e)
        => await UpdateStatusAsync(EventStatus.Ongoing);

    private async void OnEndClicked(object? sender, EventArgs e)
    {
        SetBusy(true);
        try
        {
            await _apiClient.StatusAsync(_event.EventId, EventStatus.Ended, _currentUserId);

            ShowFeedback("Event ended. Thanks for cleaning up! 🌱", isError: false);
            StartButton.IsVisible = false;
            EndButton.IsVisible = false;
            StatusDot.Color = Color.FromArgb("#888888");
            StatusLabel.Text = "Ended";

            _tcs.TrySetResult(true);
        }
        catch (ApiException<ProblemDetails> ex) { ShowFeedback(ex.Result?.Detail ?? "Could not end event.", isError: true); SetBusy(false); return; }
        catch (ApiException ex) { ShowFeedback($"Server error ({ex.StatusCode}).", isError: true); SetBusy(false); return; }
        catch (Exception ex) { ShowFeedback($"Unexpected error: {ex.Message}", isError: true); SetBusy(false); return; }
        finally { SetBusy(false); }

        _ = CloseAsync();

        var reportPopup = new EventReportPopup();
        Shell.Current.CurrentPage.ShowPopup(reportPopup);
        var report = await reportPopup.Result;

        if (report is not null)
        {
            // TODO: POST report to API when endpoint is available.
            await AppShell.DisplaySnackbarAsync(
                $"🌿 Report saved — {report.BagCount} bag{(report.BagCount == 1 ? "" : "s")}, " +
                $"~{report.LitresTotal} L collected. Amazing work!");
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
            // TODO: POST report to API when endpoint is available.
            await AppShell.DisplaySnackbarAsync(
                $"🌿 Report saved — {report.BagCount} bag{(report.BagCount == 1 ? "" : "s")}, " +
                $"~{report.LitresTotal} L collected. Amazing work!");
        }
        else
        {
            await AppShell.DisplaySnackbarAsync("Event ended. Great job cleaning up! 🌿");
        }
    }

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

            StartButton.IsVisible = false;
            EndButton.IsVisible = newStatus == EventStatus.Ongoing;
            StatusDot.Color = newStatus == EventStatus.Ongoing
                ? Color.FromArgb("#3B6D11")
                : Color.FromArgb("#888888");
            StatusLabel.Text = newStatus.ToString();

            _tcs.TrySetResult(true);
        }
        catch (ApiException<ProblemDetails> ex) { ShowFeedback(ex.Result?.Detail ?? "Could not update status.", isError: true); }
        catch (ApiException ex) { ShowFeedback($"Server error ({ex.StatusCode}).", isError: true); }
        catch (Exception ex) { ShowFeedback($"Unexpected error: {ex.Message}", isError: true); }
        finally { SetBusy(false); }
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
        FeedbackLabel.TextColor = isError ? Color.FromArgb("#E53935") : Color.FromArgb("#3B6D11");
        FeedbackLabel.IsVisible = true;
    }

    private void SetBusy(bool busy)
    {
        JoinButton.IsEnabled = !busy;
        LeaveButton.IsEnabled = !busy;
        StartButton.IsEnabled = !busy;
        EndButton.IsEnabled = !busy;
        BusyIndicator.IsRunning = busy;
        BusyIndicator.IsVisible = busy;
    }
}
