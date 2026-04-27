using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using NiceCleanApp.Services;

namespace NiceCleanApp.Pages.Controls;

public partial class PinInfoPopup : Popup
{
    private readonly IClient _apiClient;
    private readonly int _currentUserId;
    private readonly List<Pin> _pin;

    public PinInfoPopup(Pin pin, bool isTooFar, bool isWalkable, IClient apiClient, int currentUserId)
    {
        InitializeComponent();
        TooFarBanner.IsVisible = isTooFar;
        _apiClient = apiClient;
        _currentUserId = currentUserId;
        _pin = new List<Pin> { pin };
        WalkButton.IsVisible = isWalkable;
        Populate(pin);
    }

    private void Populate(Pin pin)
    {
        // Header
        TitleLabel.Text = string.IsNullOrWhiteSpace(pin.LocationName)
            ? $"{pin.Latitude:F4}, {pin.Longitude:F4}"
            : pin.LocationName;
        SubtitleLabel.Text = $"{pin.Latitude:F5}, {pin.Longitude:F5}";


        // Status dot colour mirrors MapPage.GetPinMapsuiColor
        (StatusDot.Color, StatusLabel.Text) = pin.Status switch
        {
            PinStatus.Verified   => (Color.FromArgb("#FF3300"), "Verified"),
            PinStatus.Cleaned    => (Color.FromArgb("#00AA44"), "Cleaned"),
            PinStatus.Deleted    => (Color.FromArgb("#888888"), "Deleted"),
            _                    => (Color.FromArgb("#FF9900"), "Unverified")
        };

        // Info rows
        TypeLabel.Text =pin.PollutionType.ToString();
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
    }

    private async void OnWalkClicked(object? sender, EventArgs e)
    {
        var popup = new CreateEventPopup(_pin, _currentUserId, _apiClient);
        Application.Current?.MainPage?.ShowPopup(popup);
    }
    private void OnCloseClicked(object? sender, EventArgs e) => _ = CloseAsync();
}