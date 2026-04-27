using NiceCleanLib.Enums;
using NiceCleanLib.Models;
using NiceCleanLib.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NiceCleanLib.Services.Repositories;

public class PinRepository : IPinRepository
{
    private readonly List<Pin> _pins = new();
    private int _nextId = 1;

    // Dummy data for testing purposes (3 pins with different severities, types, and statuses)
    public PinRepository()
    {
        Add(new Pin(
            id: 0,
            userId: 1,
            creationDate: DateTime.UtcNow.AddDays(-2),
            severity: PollutionSeverity.High,
            radius: Pin.StandardRadiusMeters,
            status: PinStatus.Verified,
            pollutionType: PollutionType.Plastic,
            latitude: 43.6950,
            longitude: 7.2586,
            locationName: "Promenade des Anglais",
            hasEvent: false
        ));

        Add(new Pin(
            id: 0,
            userId: 2,
            creationDate: DateTime.UtcNow.AddHours(-5),
            severity: PollutionSeverity.Moderate,
            radius: Pin.StandardRadiusMeters,
            status: PinStatus.Unverified,
            pollutionType: PollutionType.Furniture,
            latitude: 43.7034,
            longitude: 7.2663,
            locationName: "Avenue Jean Médecin Area",
            hasEvent: false
        ));

        Add(new Pin(
            id: 0,
            userId: 1,
            creationDate: DateTime.UtcNow.AddDays(-10),
            severity: PollutionSeverity.Low,
            radius: Pin.StandardRadiusMeters,
            status: PinStatus.Unverified,
            pollutionType: PollutionType.Glass,
            latitude: 43.6967,
            longitude: 7.2755,
            locationName: "Vieux Nice",
            hasEvent: false
        ));
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

    // Helper method for distance calculation using Haversine formula. Used in GetPinAtLocation to check proximity of pins.
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
