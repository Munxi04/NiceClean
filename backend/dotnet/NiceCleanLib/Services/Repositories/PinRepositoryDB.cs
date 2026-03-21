using NiceCleanLib.Data;
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

}
