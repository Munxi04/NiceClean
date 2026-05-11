using CommunityToolkit.Maui.Views;
using NiceCleanApp.Services;

namespace NiceCleanApp.Pages.Controls;

/// <summary>
/// Shown immediately after the host ends an event.
/// Collects number of bags and bag volume, then surfaces the result via
/// <see cref="Result"/> so the caller can POST to <c>api/Report</c>.
/// </summary>
public partial class EventReportPopup : Popup
{
    // ── Result type ─────────────────────────────────────────────────────────
    /// <summary>
    /// The values the host entered, ready to be passed directly to
    /// <see cref="ReportCreateDto"/>.
    /// </summary>
    public sealed record ReportResult(int NumberOfBags, BagVolume BagVolume);

    private readonly TaskCompletionSource<ReportResult?> _tcs = new();

    /// <summary>
    /// Awaitable result; <c>null</c> when the user skips or dismisses.
    /// </summary>
    public Task<ReportResult?> Result => _tcs.Task;

    // ── Litres lookup — mirrors BagVolume enum order ─────────────────────────
    private static readonly Dictionary<BagVolume, int> VolumeLitres = new()
    {
        [BagVolume.Small] = 10,
        [BagVolume.Medium] = 30,
        [BagVolume.Large] = 60,
        [BagVolume.ExtraLarge] = 100,
    };

    // ── Card map — keyed by the CommandParameter string in XAML ─────────────
    private Dictionary<string, (Border Card, Label MainLabel, Label SubLabel)> _cards = [];

    private int _bagCount = 1;
    private BagVolume _selectedVolume = BagVolume.Small;

    public EventReportPopup()
    {
        InitializeComponent();

        // Build the card map after InitializeComponent so x:Name fields exist.
        _cards = new()
        {
            ["Small"] = (SmallCard, SmallLabel, SmallSubLabel),
            ["Medium"] = (MediumCard, MediumLabel, MediumSubLabel),
            ["Large"] = (LargeCard, LargeLabel, LargeSubLabel),
            ["ExtraLarge"] = (XLargeCard, XLargeLabel, XLargeSubLabel),
        };

        DecrementButton.IsEnabled = false; // starts at 1
        RefreshSummary();
    }

    // ── Bag-count stepper ───────────────────────────────────────────────────

    private void OnIncrementClicked(object? sender, EventArgs e)
    {
        _bagCount++;
        BagCountLabel.Text = _bagCount.ToString();
        DecrementButton.IsEnabled = true;
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
        if (e.Parameter is not string key) return;
        if (!Enum.TryParse<BagVolume>(key, out var volume)) return;

        _selectedVolume = volume;
        HighlightVolumeCard(key);
        RefreshSummary();
    }

    private void HighlightVolumeCard(string selectedKey)
    {
        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark
                   || Application.Current?.UserAppTheme == AppTheme.Dark;

        var activeColor = Color.FromArgb("#3B6D11");
        var inactiveStroke = isDark ? Color.FromArgb("#444444") : Color.FromArgb("#E0E0E0");
        var inactiveBg = isDark ? Color.FromArgb("#2C2C2C") : Color.FromArgb("#F9F9F9");
        var inactiveText = isDark ? Color.FromArgb("#EEEEEE") : Color.FromArgb("#333333");
        var inactiveSubText = isDark ? Color.FromArgb("#AAAAAA") : Color.FromArgb("#888888");
        var activeSubText = Color.FromArgb("#C5E49A");

        foreach (var (key, (card, mainLbl, subLbl)) in _cards)
        {
            bool isActive = key == selectedKey;
            card.Stroke = isActive ? activeColor : inactiveStroke;
            card.BackgroundColor = isActive ? activeColor : inactiveBg;
            mainLbl.TextColor = isActive ? Colors.White : inactiveText;
            subLbl.TextColor = isActive ? activeSubText : inactiveSubText;
        }
    }

    // ── Summary ─────────────────────────────────────────────────────────────

    private void RefreshSummary()
    {
        int litresEach = VolumeLitres[_selectedVolume];
        int litresTotal = _bagCount * litresEach;
        string volName = _selectedVolume == BagVolume.ExtraLarge ? "Extra Large" : _selectedVolume.ToString();

        SummaryLabel.Text =
            $"{_bagCount} × {volName} bag{(_bagCount == 1 ? "" : "s")} " +
            $"≈ {litresTotal} L of waste collected 🌿";
    }

    // ── Actions ─────────────────────────────────────────────────────────────

    private void OnSubmitClicked(object? sender, EventArgs e)
    {
        _tcs.TrySetResult(new ReportResult(_bagCount, _selectedVolume));
        _ = CloseAsync();
    }

    private void OnSkipClicked(object? sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        _ = CloseAsync();
    }
}