//using NiceCleanLib.Data;
//using NiceCleanLib.Enums;
//using NiceCleanLib.Models;
//using NiceCleanLib.Services.Interfaces;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace NiceCleanLib.Services.Repositories;

//public class EventRepositoryDB : IEventRepository
//{
//    private readonly NiceCleanDbContext _context;

//    public EventRepositoryDB(NiceCleanDbContext context)
//    {
//        _context = context;
//    }

//    public List<Event> GetAll()
//    {
//        return _context.Events.ToList();
//    }

//    public Event? GetById(int id)
//    {
//        return _context.Events.Find(id);
//    }

//    public Event Add(Event newEvent)
//    {
//        _context.Events.Add(newEvent);
//        _context.SaveChanges();
//        return newEvent;
//    }

//    public Event? UpdateStatus(int eventId, EventStatus newStatus)
//    {
//        var existingEvent = _context.Events.Find(eventId);
//        if (existingEvent != null)
//        {
//            // Corrected from 'Status' to 'EventStatus'
//            existingEvent.EventStatus = newStatus;
//            _context.SaveChanges();
//        }
//        return existingEvent;
//    }

//    public Event? Delete(int id)
//    {
//        var ev = GetById(id);
//        if (ev != null)
//        {
//            _context.Events.Remove(ev);
//            // Warning: Usually dependent Participations will be cascade-deleted by EF Core 
//            // if configured correctly in OnModelCreating, or you could manually delete them.
//            _context.SaveChanges();
//        }
//        return ev;
//    }

//    public Participation? AddParticipant(int eventId, int userId)
//    {
//        if (HasUserJoined(eventId, userId)) return null;

//        // Corrected to use the parameterised constructor matching your Participation model
//        var participation = new Participation(
//            participationId: 0, // Entity Framework will automatically assign the ID
//            userId: userId,
//            eventId: eventId,
//            isParticipating: true,
//            joinDate: DateTime.UtcNow
//        );

//        _context.Participations.Add(participation);
//        _context.SaveChanges();

//        return participation;
//    }

//    public List<Participation> GetParticipantsForEvent(int eventId)
//    {
//        return _context.Participations.Where(p => p.EventId == eventId).ToList();
//    }

//    public bool HasUserJoined(int eventId, int userId)
//    {
//        return _context.Participations.Any(p => p.EventId == eventId && p.UserId == userId);
//    }
//}