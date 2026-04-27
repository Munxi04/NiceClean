using NiceCleanLib.Data;
using NiceCleanLib.Enums;
using NiceCleanLib.Models;
using NiceCleanLib.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NiceCleanLib.Services.Repositories;

public class PinRepositoryDB : IPinRepository
{
    private readonly NiceCleanDbContext _context;

    public PinRepositoryDB(NiceCleanDbContext context)
    {
        _context = context;
    }

    public List<Pin> GetAll()
    {
        return _context.Pins.ToList();
    }

    public Pin? GetById(int id)
    {
        return _context.Pins.Find(id);
    }

    public Pin Add(Pin pin)
    {
        _context.Pins.Add(pin);
        _context.SaveChanges();
        return pin;
    }

    public Pin? Update(int id, Pin pin)
    {
        var existing = _context.Pins.Find(id);

        if (existing == null)
        {
            return null;
        }

        existing.Severity = pin.Severity;
        existing.Radius = pin.Radius;
        existing.Status = pin.Status;
        existing.PollutionType = pin.PollutionType;
        existing.Latitude = pin.Latitude;
        existing.Longitude = pin.Longitude;
        existing.LocationName = pin.LocationName;
        existing.HasEvent = pin.HasEvent;

        _context.SaveChanges();
        return existing;
    }

    public Pin? Delete(int id)
    {
        var pin = GetById(id);
        if (pin != null)
        {
            _context.Pins.Remove(pin);
            _context.SaveChanges();
        }
        return pin;
    }

    public Pin? GetPinAtLocation(double latitude, double longitude)
    {
        var activePins = _context.Pins
            .Where(p => p.Status != PinStatus.Deleted && p.Status != PinStatus.Cleaned)
            .ToList();

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

    // Helper method for distance calculation using Haversine formula. Used in IsLocationOccupied to check proximity of pins.
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
