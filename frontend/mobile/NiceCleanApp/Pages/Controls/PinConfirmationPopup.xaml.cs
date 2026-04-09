using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Maps; // for Location

namespace NiceCleanApp.Pages.Controls;

public partial class PinConfirmationPopup : Popup
{

    public PinConfirmationPopup()
    {
        InitializeComponent();
    }

    private void OnConfirmClicked(object? sender, EventArgs e)
    {
        Close(true);
    }

    private void OnDenyClicked(object? sender, EventArgs e)
    {
        Close(false);
    }

    private void OnBackgroundTapped(object? sender, TappedEventArgs e)
    {
        // Dismiss when tapping outside (but not on buttons)
        if (e.Handled) return;
        Close(false);
    }
}