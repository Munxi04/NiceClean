using NiceCleanLib.Data;
using NiceCleanLib.Enums;
using NiceCleanLib.Models;
using NiceCleanLib.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NiceCleanLib.Services.Repositories;

public class EventRepositoryDB : IEventRepository
{
    private readonly NiceCleanDbContext _context;

    public EventRepositoryDB(NiceCleanDbContext context)
    {
        _context = context;
    }

    public List<Event> GetAll()
    {
        return _context.Events.ToList();
    }

    public Event? GetById(int id)
    {
        return _context.Events.Find(id);
    }

    public Event Add(Event newEvent)
    {
        _context.Events.Add(newEvent);
        _context.SaveChanges();
        return newEvent;
    }

    public Event? UpdateStatus(int eventId, EventStatus newStatus)
    {
        var existingEvent = _context.Events.Find(eventId);
        if (existingEvent != null)
        {
            existingEvent.EventStatus = newStatus;
            _context.SaveChanges();
        }
        return existingEvent;
    }

    public Event? Delete(int id)
    {
        var ev = GetById(id);
        if (ev != null)
        {
            _context.Events.Remove(ev);
            _context.SaveChanges();
        }
        return ev;
    }

    public Participation? AddParticipant(int eventId, int userId)
    {
        if (HasUserJoined(eventId, userId)) return null;

        var participation = new Participation(
            participationId: 0,
            userId: userId,
            eventId: eventId,
            isParticipating: true,
            joinDate: DateTime.UtcNow
        );

        _context.Participations.Add(participation);

        var existingEvent = GetById(eventId);
        if (existingEvent != null)
        {
            existingEvent.ParticipationCount += 1;
        }

        _context.SaveChanges();

        return participation;
    }

    public List<Participation> GetParticipantsForEvent(int eventId)
    {
        return _context.Participations.Where(p => p.EventId == eventId).ToList();
    }

    public List<string> GetEventNicknames(int eventId)
    {
        // Fetches participations for a specific event and joins them with the Users table to retrieve their nicknames.
        return _context.Participations
            .Where(p => p.EventId == eventId)
            .Join(_context.Users,
                  participation => participation.UserId,
                  user => user.Id,
                  (participation, user) => user.Nickname ?? "Unknown User")
            .ToList();
    }

    public bool HasUserJoined(int eventId, int userId)
    {
        return _context.Participations.Any(p => p.EventId == eventId && p.UserId == userId);
    }

    public bool RemoveParticipant(int eventId, int userId)
    {
        var participation = _context.Participations
            .FirstOrDefault(p => p.EventId == eventId && p.UserId == userId);

        if (participation == null) return false;

        _context.Participations.Remove(participation);

        var existingEvent = GetById(eventId);
        if (existingEvent != null)
        {
            existingEvent.ParticipationCount -= 1;
        }

        _context.SaveChanges();
        return true;
    }

    public Event? RescheduleEvent(int eventId, DateTime newDate)
    {
        var existingEvent = GetById(eventId);
        if (existingEvent == null) return null;

        existingEvent.Date = newDate;
        _context.SaveChanges();

        return existingEvent;
    }
}