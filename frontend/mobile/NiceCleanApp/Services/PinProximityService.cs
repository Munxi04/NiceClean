using NiceCleanApp.Services;

namespace NiceCleanApp.Services;

/// <summary>
/// Contract for the proximity monitor.
/// MapPage subscribes to <see cref="PinEntered"/> and shows the validation popup.
/// </summary>
public interface IPinProximityService
{
    /// <summary>Fired on the UI thread when the user steps within 10 meters of a qualifying pin.</summary>
    event EventHandler<Pin> PinEntered;

    /// <summary>Begin GPS polling against the supplied pin list.</summary>
    void StartMonitoring(IEnumerable<Pin> pins, int currentUserId);

    /// <summary>Stop GPS polling (call on page disappear or sign-out).</summary>
    void StopMonitoring();

    /// <summary>Call after a pin has been validated so it is never re-triggered.</summary>
    void MarkHandled(int pinId);
}

/// <summary>
/// Polls the device GPS every <see cref="PollIntervalMs"/> milliseconds and fires
/// <see cref="PinEntered"/> whenever the user walks within <see cref="TriggerRadiusMeters"/>
/// of a pin they did not create and that is not yet Cleaned/Deleted.
/// </summary>
public sealed class PinProximityService : IPinProximityService
{
    // ── Configuration ──────────────────────────────
    /// <summary>Distance in metres that triggers the validation prompt.</summary>
    public const double TriggerRadiusMeters = 10.0;

    /// <summary>How often to read GPS (milliseconds). 5 s is a good balance of responsiveness vs battery.</summary>
    private const int PollIntervalMs = 5_000;

    // ── State ──────────────────────────────────────
    private CancellationTokenSource? _cts;
    private List<Pin> _pins = [];

    /// <summary>
    /// Pin IDs that have already triggered a popup in this session so we don't
    /// spam the user if they linger near the same spot.
    /// </summary>
    private readonly HashSet<int> _handled = [];

    // ── Public API ─────────────────────────────────

    public event EventHandler<Pin>? PinEntered;

    public void StartMonitoring(IEnumerable<Pin> pins, int currentUserId)
    {
        // Only watch pins that:
        //   • belong to a different user (creator cannot validate their own)
        //   • are still actionable (not already Cleaned or Deleted)
        _pins = pins
            .Where(p => p.UserId != currentUserId
                     && p.Status != PinStatus.Cleaned
                     && p.Status != PinStatus.Deleted)
            .ToList();

        // Cancel any existing polling loop before starting a fresh one.
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        _ = PollLoopAsync(_cts.Token);
    }

    public void StopMonitoring()
    {
        _cts?.Cancel();
        _cts = null;
    }

    public void MarkHandled(int pinId) => _handled.Add(pinId);

    // ── Internal polling loop ──────────────────────

    private async Task PollLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var location = await Geolocation.GetLocationAsync(
                    new GeolocationRequest
                    {
                        DesiredAccuracy = GeolocationAccuracy.High,
                        Timeout         = TimeSpan.FromSeconds(4)
                    }, ct);

                if (location != null)
                    CheckProximity(location);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (FeatureNotSupportedException)
            {
                // Device has no GPS — stop silently.
                break;
            }
            catch
            {
                // Transient GPS failure — just try again next cycle.
            }

            try { await Task.Delay(PollIntervalMs, ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    private void CheckProximity(Location userLocation)
    {
        foreach (var pin in _pins)
        {
            if (_handled.Contains(pin.Id)) continue;

            double distanceM = HaversineMeters(
                userLocation.Latitude, userLocation.Longitude,
                pin.Latitude,          pin.Longitude);

            if (distanceM <= TriggerRadiusMeters)
            {
                // Mark immediately so rapid location updates don't fire it twice.
                _handled.Add(pin.Id);

                // Marshal onto the UI thread so callers can safely show popups.
                MainThread.BeginInvokeOnMainThread(
                    () => PinEntered?.Invoke(this, pin));
            }
        }
    }

    // ── Haversine formula ──────────────────────────

    private static double HaversineMeters(
        double lat1, double lon1,
        double lat2, double lon2)
    {
        const double R = 6_371_000; // Earth radius in metres
        double φ1 = ToRad(lat1), φ2 = ToRad(lat2);
        double Δφ = ToRad(lat2 - lat1);
        double Δλ = ToRad(lon2 - lon1);

        double a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2)
                 + Math.Cos(φ1) * Math.Cos(φ2)
                 * Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);

        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double degrees) => degrees * Math.PI / 180.0;
}
