using NiceCleanLib.Enums;
using NiceCleanLib.Models;
using NiceCleanLib.Services.Interfaces;

namespace NiceCleanLib.Services.Repositories;

/// <summary>
/// In-memory pin repository for development and testing.
/// For production, use PinRepositoryDB which connects to the actual database.
/// </summary>
public class PinRepository : IPinRepository
{
    private readonly List<Pin> _pins = new();
    private int _nextId = 1;

    /// <summary>
    /// Initialize empty repository. Dummy data removed for production-ready deployment.
    /// </summary>
    public PinRepository()
    {
        // Development: Start with empty collection
        // Dummy data removed to ensure production-ready state
    }

    public List<Pin> GetAll()
    {
        return _pins;
    }

    public Pin? GetById(int id)
    {
        return _pins.FirstOrDefault(p => p.Id == id);
    }

    public Pin Add(Pin pin)
    {
        pin.Id = _nextId++;
        _pins.Add(pin);
        return pin;
    }

    public Pin? Update(int id, Pin pin)
    {
        var existing = GetById(id);
        if (existing == null)
        {
            return null;
        }

        var index = _pins.IndexOf(existing);
        pin.Id = id;
        _pins[index] = pin;
        return pin;
    }

    public Pin? Delete(int id)
    {
        Pin? pin = GetById(id);
        if (pin != null)
        {
            _pins.Remove(pin);
        }
        return pin;
    }

    public Pin? GetPinAtLocation(double latitude, double longitude)
    {
        var activePins = _pins.Where(p => p.Status != PinStatus.Deleted && p.Status != PinStatus.Cleaned);

        foreach (var pin in activePins)
        {
            if (HaversineMeters(latitude, longitude, pin.Latitude, pin.Longitude) <= Pin.StandardRadiusMeters)
            {
                return pin;
            }
        }
        return null;
    }

    public bool IsUserNear(double userLat, double userLon, double targetLat, double targetLon, double thresholdMeters)
    {
        double distance = HaversineMeters(userLat, userLon, targetLat, targetLon);
        return distance <= thresholdMeters;
    }

    /// <summary>
    /// Calculates distance between two coordinates using Haversine formula (in meters).
    /// Used in GetPinAtLocation to check proximity of pins.
    /// Note: In production, this should be replaced with database spatial queries for performance.
    /// </summary>
    private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double angle) => angle * Math.PI / 180.0;
}
