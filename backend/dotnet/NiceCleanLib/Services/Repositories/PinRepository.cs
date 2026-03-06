using NiceCleanLib.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace NiceCleanLib.Services.Repositories;

public class PinRepository
{
    private readonly List<Pin> _pins = new();
    private int _nextId = 1;

    public List<Pin> GetAll()
    {
        return _pins;
    }

    public Pin? GetById(int id)
    {
        return _pins.FirstOrDefault(s => s.Id == id);
    }

    public Pin Add(Pin sensor)
    {
        sensor.Id = _nextId++;
        _pins.Add(sensor);
        return sensor;
    }

    public Pin? Update(int id, Pin sensor)
    {
        var existing = GetById(id);
        if (existing == null)
        {
            return null;
        }

        var index = _pins.IndexOf(existing);

        sensor.Id = id;

        _pins[index] = sensor;
        return sensor;
    }

    public Pin? Delete(int id)
    {
        Pin? sensor = GetById(id);
        if (sensor != null)
        {
            _pins.Remove(sensor);
        }
        return sensor;
    }
}
