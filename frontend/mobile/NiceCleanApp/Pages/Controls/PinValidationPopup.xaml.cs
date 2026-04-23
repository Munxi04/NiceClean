using CommunityToolkit.Maui.Views;
using NiceCleanApp.Services;

namespace NiceCleanApp.Pages.Controls;

/// <summary>
/// Popup shown when the current user is within 10 m of a pin they did not create.
/// Lets them either confirm the pollution is still present (→ Verified)
/// or mark the area as cleaned (→ Cleaned).
/// </summary>
public partial class PinValidationPopup : Popup
{
    private readonly Pin _pin;
    private readonly IClient _apiClient;
    private readonly int _currentUserId;

    // Callers await this to know which status the user chose (null = dismissed).
    private readonly TaskCompletionSource<PinStatus?> _tcs = new();
    public Task<PinStatus?> Result => _tcs.Task;

    public PinValidationPopup(Pin pin, IClient apiClient, int currentUserId)
    {
        InitializeComponent();
        _pin = pin;
        _apiClient = apiClient;
        _currentUserId = currentUserId;
        Populate(pin);
    }

    // ──────────────────────────────────────────────
    // UI population
    // ──────────────────────────────────────────────

    private void Populate(Pin pin)
    {
        // Header
        TitleLabel.Text = string.IsNullOrWhiteSpace(pin.LocationName)
            ? $"{pin.Latitude:F4}, {pin.Longitude:F4}"
            : pin.LocationName;
        SubtitleLabel.Text = $"{pin.Latitude:F5}, {pin.Longitude:F5}";

        // Status dot + label
        (StatusDot.Color, StatusLabel.Text) = pin.Status switch
        {
            PinStatus.Verified => (Color.FromArgb("#FF3300"), "Verified"),
            PinStatus.Cleaned  => (Color.FromArgb("#00AA44"), "Cleaned"),
            PinStatus.Deleted  => (Color.FromArgb("#888888"), "Deleted"),
            _                  => (Color.FromArgb("#FF9900"), "Unverified")
        };

        // Info rows
        TypeLabel.Text = pin.PollutionType.ToString();
        SeverityLabel.Text = pin.Severity switch
        {
            PollutionSeverity.Low      => "🟢 Low",
            PollutionSeverity.Moderate => "🟡 Moderate",
            PollutionSeverity.High     => "🟠 High",
            PollutionSeverity.VeryHigh => "🔴 Very High",
            PollutionSeverity.Extreme  => "⚠️ Extreme",
            _                          => pin.Severity.ToString()
        };

        DateLabel.Text   = pin.CreationDate.LocalDateTime.ToString("dd MMM yyyy, HH:mm");

        // "Confirm still here" is additionally gated on: not already Verified
        // and the user hasn't already voted.
        bool alreadyVerified = pin.Status == PinStatus.Verified;
        VerifyButton.IsVisible = !alreadyVerified
            && _apiClient.HasVotedAsync(pin.Id, _currentUserId).Result == false;
    }

    // ──────────────────────────────────────────────
    // Button handlers
    // ──────────────────────────────────────────────

    private async void OnVerifyClicked(object? sender, EventArgs e)
        => await SubmitVoteAsync();

    // For the moment this simply closes the popup and leaves the pin as Unverified.
    private async void OnMarkCleanedClicked(object? sender, EventArgs e)
        => _ = CloseAsync();

    /// <summary>
    /// Confirms pollution is still present via POST /api/Pin/{id}/vote.
    /// The server transitions the pin to Verified.
    /// </summary>
    private async Task SubmitVoteAsync()
    {
        SetBusy(true);
        try
        {
            await _apiClient.VoteAsync(_pin.Id, new PinVoteDto
            {
                UserId = _currentUserId,
                VoteType = VoteType.Confirmed
            });
            _tcs.SetResult(PinStatus.Verified);
        }
        catch (ApiException<ProblemDetails> ex)
        {
            await ShowErrorAsync($"Could not cast vote: {ex.Result?.Detail ?? ex.Response}");
            _tcs.TrySetResult(null);
        }
        catch (ApiException ex)
        {
            await ShowErrorAsync($"Could not cast vote: {ex.Response}");
            _tcs.TrySetResult(null);
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Unexpected error: {ex.Message}");
            _tcs.TrySetResult(null);
        }
        finally
        {
            SetBusy(false);
            _ = CloseAsync();
        }
    }

    private void OnCloseClicked(object? sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        _ = CloseAsync();
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private void SetBusy(bool busy)
    {
        VerifyButton.IsEnabled  = !busy;
        CleanedButton.IsEnabled = !busy;
        BusyIndicator.IsVisible = busy;
        BusyIndicator.IsRunning = busy;
    }

    private static Task ShowErrorAsync(string message)
        => Shell.Current.CurrentPage.DisplayAlertAsync("Error", message, "OK");
}
