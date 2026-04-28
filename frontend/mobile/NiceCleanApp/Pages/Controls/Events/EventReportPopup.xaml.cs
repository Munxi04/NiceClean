using CommunityToolkit.Maui.Views;

namespace NiceCleanApp.Pages.Controls;

/// <summary>
/// Shown immediately after the host ends an event.
/// Collects the number of bags used and their volume category,
/// then exposes the result via <see cref="Result"/>.
/// </summary>
public partial class EventReportPopup : Popup
{
    // ── Public result type ──────────────────────────────────────────────────
    public sealed record ReportResult(int BagCount, string BagVolume, int LitresTotal);

    private readonly TaskCompletionSource<ReportResult?> _tcs = new();

    /// <summary>
    /// Awaitable result. <c>null</c> when the user skips or dismisses.
    /// </summary>
    public Task<ReportResult?> Result => _tcs.Task;

    // ── Internal state ──────────────────────────────────────────────────────
    private static readonly Dictionary<string, int> VolumeLitres = new()
    {
        ["Small"]  = 10,
        ["Medium"] = 30,
        ["Large"]  = 60,
        ["XLarge"] = 100,
    };

    private int    _bagCount      = 1;
    private string _selectedVolume = "Small"; // default matches pre-highlighted card

    public EventReportPopup()
    {
        InitializeComponent();
        RefreshSummary();
    }

    // ── Bag-count controls ──────────────────────────────────────────────────

    private void OnIncrementClicked(object? sender, EventArgs e)
    {
        _bagCount++;
        BagCountLabel.Text = _bagCount.ToString();
        DecrementButton.IsEnabled = _bagCount > 1;
        RefreshSummary();
    }

    private void OnDecrementClicked(object? sender, EventArgs e)
    {
        if (_bagCount <= 1) return;
        _bagCount--;
        BagCountLabel.Text = _bagCount.ToString();
        DecrementButton.IsEnabled = _bagCount > 1;
        RefreshSummary();
    }

    // ── Volume card selection ───────────────────────────────────────────────

    private void OnVolumeSelected(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not string volume) return;
        _selectedVolume = volume;
        HighlightVolumeCard(volume);
        RefreshSummary();
    }

    private void HighlightVolumeCard(string selected)
    {
        var active   = Color.FromArgb("#3B6D11");
        var inactiveStrokeLight = Color.FromArgb("#E0E0E0");
        var inactiveStrokeDark  = Color.FromArgb("#444444");
        var inactiveBgLight     = Color.FromArgb("#F9F9F9");
        var inactiveBgDark      = Color.FromArgb("#2C2C2C");

        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark
                   || Application.Current?.UserAppTheme   == AppTheme.Dark;

        var inactiveStroke = isDark ? inactiveStrokeDark : inactiveStrokeLight;
        var inactiveBg     = isDark ? inactiveBgDark     : inactiveBgLight;

        foreach (var (card, key) in new[]
        {
            (SmallCard,  "Small"),
            (MediumCard, "Medium"),
            (LargeCard,  "Large"),
            (XLargeCard, "XLarge"),
        })
        {
            bool isActive = key == selected;
            card.Stroke          = isActive ? active        : inactiveStroke;
            card.BackgroundColor = isActive ? active        : inactiveBg;

            // Re-tint child text labels
            foreach (var lbl in card.GetVisualTreeDescendants().OfType<Label>())
            {
                lbl.TextColor = isActive ? Colors.White
                    : (isDark ? Color.FromArgb("#EEEEEE") : Color.FromArgb("#333333"));
            }
        }
    }

    // ── Summary ─────────────────────────────────────────────────────────────

    private void RefreshSummary()
    {
        int litresEach  = VolumeLitres.TryGetValue(_selectedVolume, out var l) ? l : 0;
        int litresTotal = _bagCount * litresEach;

        SummaryLabel.Text =
            $"{_bagCount} × {_selectedVolume} bag{(_bagCount == 1 ? "" : "s")} " +
            $"≈ {litresTotal} L of waste collected 🌿";
    }

    // ── Actions ─────────────────────────────────────────────────────────────

    private void OnSubmitClicked(object? sender, EventArgs e)
    {
        int litresEach  = VolumeLitres.TryGetValue(_selectedVolume, out var l) ? l : 0;
        int litresTotal = _bagCount * litresEach;

        _tcs.TrySetResult(new ReportResult(_bagCount, _selectedVolume, litresTotal));
        _ = CloseAsync();
    }

    private void OnSkipClicked(object? sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        _ = CloseAsync();
    }
}
