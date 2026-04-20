using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Platform;
using CommunityToolkit.Maui.Views;
using NiceCleanApp.Services;

namespace NiceCleanApp.Pages.Controls;

public partial class PinInfoPopup : Popup
{
    public PinInfoPopup(Pin pin)
    {
        InitializeComponent();
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
            PollutionSeverity.Extreme  => "💀 Extreme",
            _                          => pin.Severity.ToString()
        };

        RadiusLabel.Text = $"{pin.Radius:F0} m";
        DateLabel.Text   = pin.CreationDate.LocalDateTime.ToString("dd MMM yyyy, HH:mm");
    }

    private void OnCloseClicked(object? sender, EventArgs e) => _ = CloseAsync();
}