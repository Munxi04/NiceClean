using System;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using NiceCleanApp.Services;

namespace NiceCleanApp.Pages.Controls;

public partial class PinInfoPopup : Popup
{
    private readonly IClient _apiClient;
    private readonly int _currentUserId;
    private readonly List<Pin> _pinList;
    private readonly bool _isTooFar;
    private readonly Pin _pin;

    public PinInfoPopup(Pin pin, bool isTooFar, IClient apiClient, int currentUserId)
    {
        InitializeComponent();
        _apiClient = apiClient;
        _currentUserId = currentUserId;
        _pinList = [pin];
        _isTooFar = isTooFar;
        _pin = pin;
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

        ConfigureActionButtons();
    }

    private async void ConfigureActionButtons()
    {
        bool isHost = false;
        bool isParticipant = false;
        if (_pin.HasEvent)
        {
            EventResponseDto @event = await _apiClient.EventGETAsync(_pin.EventId);
            isHost = @event.HostUserId == _currentUserId;
            isParticipant = await _apiClient.HasJoinedAsync(@event.EventId, _currentUserId);
        }

        WalkButton.IsVisible = _pin.Status == PinStatus.Verified && !_pin.HasEvent;
        TooFarBanner.IsVisible = _isTooFar;
        JoinButton.IsVisible = _pin.HasEvent && !isHost && !isParticipant;
        LeaveButton.IsVisible = _pin.HasEvent && !isHost && isParticipant;
    }

    private async void OnWalkClicked(object? sender, EventArgs e)
    {
        var popup = new CreateEventPopup(_pinList, _currentUserId, _apiClient);
        _ = CloseAsync();
        Shell.Current.CurrentPage.ShowPopup(popup);
    }

    private async void OnJoinClicked(object? sender, EventArgs e)
    {
        var popup = new EventDetailPopup(await _apiClient.EventGETAsync(_pin.EventId), _apiClient, _currentUserId);
        _ = CloseAsync();
        Shell.Current.CurrentPage.ShowPopup(popup);
    }

    private async void OnLeaveClicked(object? sender, EventArgs e)
    {
        var popup = new EventDetailPopup(await _apiClient.EventGETAsync(_pin.EventId), _apiClient, _currentUserId);
        _ = CloseAsync();
        Shell.Current.CurrentPage.ShowPopup(popup);
    }
    private void OnCloseClicked(object? sender, EventArgs e) => _ = CloseAsync();
}