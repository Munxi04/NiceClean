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
            radius: 50,
            status: PinStatus.Verified,
            pollutionType: PollutionType.Plastic,
            latitude: 43.6950,
            longitude: 7.2586,
            locationName: "Promenade des Anglais"
        ));

        Add(new Pin(
            id: 0,
            userId: 2,
            creationDate: DateTime.UtcNow.AddHours(-5),
            severity: PollutionSeverity.Moderate,
            radius: 100,
            status: PinStatus.Unverified,
            pollutionType: PollutionType.Furniture,
            latitude: 43.7034,
            longitude: 7.2663,
            locationName: "Avenue Jean Médecin Area"
        ));

        Add(new Pin(
            id: 0,
            userId: 1,
            creationDate: DateTime.UtcNow.AddDays(-10),
            severity: PollutionSeverity.Low,
            radius: 20,
            status: PinStatus.Unverified,
            pollutionType: PollutionType.Glass,
            latitude: 43.6967,
            longitude: 7.2755,
            locationName: "Vieux Nice"
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
}
