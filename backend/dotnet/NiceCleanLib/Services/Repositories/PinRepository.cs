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
