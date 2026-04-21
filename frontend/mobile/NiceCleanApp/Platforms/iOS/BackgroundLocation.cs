// Platforms/iOS/BackgroundLocation.cs
// ─────────────────────────────────────────────────────────────────────────────
// iOS keeps our GPS polling alive through "Background Fetch" and the
// "Location updates" background mode. This file wires up the CLLocationManager
// "significant-change" service so iOS can wake the app after it is suspended.
//
// SETUP CHECKLIST
// ───────────────
// 1. In Info.plist add:
//
//      <key>NSLocationAlwaysAndWhenInUseUsageDescription</key>
//      <string>NiceClean monitors nearby pollution spots even when you're not using the app.</string>
//      <key>NSLocationWhenInUseUsageDescription</key>
//      <string>NiceClean shows pollution spots near your location.</string>
//      <key>UIBackgroundModes</key>
//      <array>
//          <string>location</string>
//          <string>fetch</string>
//      </array>
//
// 2. Request Permissions.LocationAlways (not just LocationWhenInUse) when the
//    user first opens MapPage so the background mode is available.
//
// 3. Call BackgroundLocationHelper.StartSignificantChangeMonitoring() from
//    AppDelegate.FinishedLaunching after DI is set up.
// ─────────────────────────────────────────────────────────────────────────────

#if IOS
using CoreLocation;
using NiceCleanApp.Services;

namespace NiceCleanApp.Platforms.iOS;

/// <summary>
/// Uses CLLocationManager's "significant location change" service
/// (low battery, ~500 m accuracy) to wake the app in background.
/// When the app wakes, PinProximityService resumes its high-accuracy
/// polling via the normal MAUI Geolocation API.
/// </summary>
public static class BackgroundLocationHelper
{
    private static CLLocationManager? _manager;
    private static ProximityLocationDelegate? _delegate;

    public static void StartSignificantChangeMonitoring()
    {
        _manager  = new CLLocationManager();
        _delegate = new ProximityLocationDelegate();
        _manager.Delegate = _delegate;

        // "Significant change" wakes the app every ~500 m — enough to know
        // that the user has moved into a new area. PinProximityService then
        // does the precise 10 m check.
        _manager.StartMonitoringSignificantLocationChanges();
    }

    public static void StopSignificantChangeMonitoring()
        => _manager?.StopMonitoringSignificantLocationChanges();
}

/// <summary>
/// CLLocationManager delegate that re-triggers proximity checking after
/// the OS wakes the app for a significant location change.
/// </summary>
internal sealed class ProximityLocationDelegate : CLLocationManagerDelegate
{
    public override void LocationsUpdated(CLLocationManager manager, CLLocation[] locations)
    {
        // When the OS wakes us, PinProximityService is still registered
        // in MAUI's DI container. Signal it to do a fresh check.
        var proximity = IPlatformApplication.Current?.Services
            .GetService<IPinProximityService>();
        
        var notifications = IPlatformApplication.Current?.Services
            .GetService<IPinNotificationService>();

        if (proximity == null || notifications == null) return;

        // Subscribe for this wake cycle. The service de-dupes via _handled.
        void OnPinEntered(object? _, Pin pin) => notifications.NotifyNearbyPin(pin);
        proximity.PinEntered += OnPinEntered;

        // Give the high-accuracy poller a single cycle to fire events.
        Task.Delay(8_000).ContinueWith(_ =>
            proximity.PinEntered -= OnPinEntered);
    }
}
#endif
