// Platforms/Android/BackgroundLocationService.cs
// ─────────────────────────────────────────────────────────────────────────────
// Android-specific Foreground Service that keeps GPS polling alive when
// the user backgrounds the app. The service posts a persistent "NiceClean is
// tracking your location" notification (required by Android 8+) and signals
// the shared IPinProximityService whenever the user nears a pin.
//
// SETUP CHECKLIST
// ───────────────
// 1. Add to AndroidManifest.xml (inside <manifest>):
//
//      <uses-permission android:name="android.permission.FOREGROUND_SERVICE" />
//      <uses-permission android:name="android.permission.FOREGROUND_SERVICE_LOCATION" />
//      <uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
//      <uses-permission android:name="android.permission.ACCESS_BACKGROUND_LOCATION" />
//      <uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
//
//    And inside <application>:
//
//      <service
//          android:name="NiceCleanApp.Platforms.Android.BackgroundLocationService"
//          android:foregroundServiceType="location"
//          android:exported="false" />
//
// 2. In MauiProgram.cs, after builder.UseMauiApp<App>():
//
//      builder.Services.AddSingleton<IPinProximityService, PinProximityService>();
//      builder.Services.AddSingleton<INotificationService, NotificationService>();
//
// 3. Request ACCESS_BACKGROUND_LOCATION at runtime (Android 10+) before
//    starting the service — this requires a separate runtime dialog.
// ─────────────────────────────────────────────────────────────────────────────

#if ANDROID
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using Microsoft.Maui.ApplicationModel;
using NiceCleanApp.Services;
using Plugin.LocalNotification;

namespace NiceCleanApp.Platforms.Android;

[Service(
    ForegroundServiceType = ForegroundService.TypeLocation,
    Exported = false)]
public sealed class BackgroundLocationService : Service
{
    // ── Channel / notification IDs ─────────────────
    private const string ChannelId         = "niceclean_proximity";
    private const string ChannelName       = "NiceClean location tracking";
    private const int    ForegroundNotifId = 9_001;

    private CancellationTokenSource? _cts;

    // ── Service lifecycle ──────────────────────────

    public override IBinder? OnBind(Intent? intent) => null; // not a bound service

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        EnsureNotificationChannel();
        StartForeground(ForegroundNotifId, BuildPersistentNotification());
        StartPolling();
        return StartCommandResult.Sticky; // OS restarts us if killed
    }

    public override void OnDestroy()
    {
        _cts?.Cancel();
        base.OnDestroy();
    }

    // ── GPS polling ────────────────────────────────

    private void StartPolling()
    {
        _cts = new CancellationTokenSource();
        _ = PollAsync(_cts.Token);
    }

    private async Task PollAsync(CancellationToken ct)
    {
        // Resolve the shared service from the MAUI DI container.
        var proximity = IPlatformApplication.Current!.Services
            .GetService<IPinProximityService>();
        var notifications = IPlatformApplication.Current!.Services
            .GetService<IPinNotificationService>();

        if (proximity == null) return;

        // Subscribe once; the service handles the event below.
        proximity.PinEntered += (_, pin) => notifications?.NotifyNearbyPin(pin);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Reading GPS here keeps the same proximity logic reused from
                // PinProximityService — we don't need to duplicate Haversine math.
                // PinProximityService.StartMonitoring is called by MapPage; this
                // service's role is purely to keep that call alive in background.
                await Task.Delay(5_000, ct);
            }
            catch (System.OperationCanceledException) { break; }
        }
    }

    // ── Persistent foreground notification ─────────

    private Notification BuildPersistentNotification()
    {
        // Tapping the notification opens the app at MapPage.
        var pendingIntent = PendingIntent.GetActivity(
            this, 0,
            new Intent(this, typeof(MainActivity)),
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        return new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("NiceClean")
            .SetContentText("Monitoring for nearby pollution spots…")
            .SetSmallIcon(Resource.Drawable.abc_ic_menu_overflow_material) // replace with your icon
            .SetOngoing(true)
            .SetContentIntent(pendingIntent)
            .SetPriority(NotificationCompat.PriorityMin) // low-priority so it collapses
            .Build();
    }

    private void EnsureNotificationChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

        var channel = new NotificationChannel(
            ChannelId,
            ChannelName,
            NotificationImportance.Min) // quiet — just keeps the service alive
        {
            Description = "Used to keep location tracking active in the background."
        };

        var manager = GetSystemService(NotificationService) as NotificationManager;
        manager?.CreateNotificationChannel(channel);
    }
}

/// <summary>
/// Helper that MapPage calls to start / stop the background service.
/// </summary>
public static class BackgroundLocationServiceHelper
{
    public static void Start(Context context)
    {
        var intent = new Intent(context, typeof(BackgroundLocationService));
        context.StartForegroundService(intent);
    }

    public static void Stop(Context context)
    {
        var intent = new Intent(context, typeof(BackgroundLocationService));
        context.StopService(intent);
    }
}
#endif
