using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AndroidOption;

namespace NiceCleanApp.Services;

// ══════════════════════════════════════════════════════════════════════════════
// INTERFACE
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Sends a local device notification when the user is near a pollution pin
/// and the app is in the background.
/// </summary>
public interface IPinNotificationService
{
    /// <summary>Request OS permission to show notifications (call once on startup).</summary>
    Task RequestPermissionAsync();

    /// <summary>
    /// Post a "nearby pin" notification that opens the app when tapped.
    /// Only call this when the app is backgrounded; when foregrounded use the popup instead.
    /// </summary>
    void NotifyNearbyPin(Pin pin);
}

/// <summary>
/// Legacy/DI-friendly notification interface used by platform code.
/// Some platform files request `INotificationService`; provide a matching
/// interface so the existing `PinNotificationService` can be registered
/// for both interfaces without changing other files.
/// </summary>
public interface INotificationService
{
    Task RequestPermissionAsync();
    void NotifyNearbyPin(Pin pin);
}

// ══════════════════════════════════════════════════════════════════════════════
// IMPLEMENTATION  (requires NuGet: Plugin.LocalNotification 11.x)
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Cross-platform implementation backed by Plugin.LocalNotification.
///
/// Setup required:
///   Android – add to AndroidManifest.xml:
///     &lt;uses-permission android:name="android.permission.POST_NOTIFICATIONS" /&gt;
///     &lt;uses-permission android:name="android.permission.RECEIVE_BOOT_COMPLETED" /&gt;
///
///   iOS – add to Info.plist:
///     &lt;key&gt;UIBackgroundModes&lt;/key&gt;
///     &lt;array&gt;&lt;string&gt;fetch&lt;/string&gt;&lt;/array&gt;
///
///   MauiProgram.cs – call builder.UseLocalNotification() before Build().
/// </summary>
public sealed class PinNotificationService : IPinNotificationService, INotificationService
{
    private const int NearbyPinNotificationId = 1_000;

    // ── Permission ──────────────────────────────────

    public async Task RequestPermissionAsync()
    {
        var granted = await LocalNotificationCenter.Current.RequestNotificationPermission();
        if (!granted)
            System.Diagnostics.Debug.WriteLine("[NotificationService] Notification permission denied.");
    }

    // ── Nearby-pin notification ─────────────────────

    public void NotifyNearbyPin(Pin pin)
    {
        var locationText = string.IsNullOrWhiteSpace(pin.LocationName)
            ? $"{pin.Latitude:F4}, {pin.Longitude:F4}"
            : pin.LocationName;

        var request = new NotificationRequest
        {
            NotificationId = NearbyPinNotificationId + pin.Id, // unique per pin
            Title = "🌿 Pollution spot nearby",
            Description = $"{pin.PollutionType} ({pin.Severity}) reported at {locationText}. "
                          + "Tap to open the app and validate.",
            BadgeNumber = 1,
            Android = new AndroidOptions
            {
                ChannelId = "niceclean_proximity",
                Priority = AndroidPriority.High
            }
        };

        LocalNotificationCenter.Current.Show(request);
    }
}
