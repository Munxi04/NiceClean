using NiceCleanLib.Enums;
using NiceCleanLib.Models;
using NiceCleanLib.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace NiceCleanLib.Services.Repositories
{
    public class EventRepository : IEventRepository
    {
        private readonly List<Event> _events = new();
        private readonly List<Participation> _participations = new();
        private readonly IUserRepository _userRepository;
        private int _nextEventId = 1;
        private int _nextParticipationId = 1;

        public List<Event> GetAll() => _events;

        public Event? GetById(int id) => _events.FirstOrDefault(e => e.EventId == id);

        public Event Add(Event newEvent)
        {
            newEvent.EventId = _nextEventId++;
            _events.Add(newEvent);
            return newEvent;
        }

        public Event? UpdateStatus(int eventId, EventStatus newStatus)
        {
            var existingEvent = GetById(eventId);
            if (existingEvent != null)
            {
                existingEvent.EventStatus = newStatus;
            }
            return existingEvent;
        }

        public Event? Delete(int id)
        {
            var ev = GetById(id);
            if (ev != null)
            {
                _events.Remove(ev);
                _participations.RemoveAll(p => p.EventId == id); // Cascade delete participations
            }
            return ev;
        }

        public Participation? AddParticipant(int eventId, int userId)
        {
            if (HasUserJoined(eventId, userId)) return null;

            var participation = new Participation
            {
                ParticipationId = _nextParticipationId++,
                EventId = eventId,
                UserId = userId,
                IsParticipating = true,
                JoinDate = DateTime.UtcNow
            };
            _participations.Add(participation);
            GetById(eventId)!.ParticipationCount += 1;
            return participation;
        }

        public List<Participation> GetParticipantsForEvent(int eventId)
        {
            return _participations.Where(p => p.EventId == eventId).ToList();
        }

        public List<string> GetEventNicknames(int eventId)
        {
            var participations = GetParticipantsForEvent(eventId);
            return participations.Select(p => _userRepository.GetById(p.UserId)?.Nickname ?? "Unknown User").ToList();
        }

        public bool HasUserJoined(int eventId, int userId)
        {
            return _participations.Any(p => p.EventId == eventId && p.UserId == userId);
        }

        public bool RemoveParticipant(int eventId, int userId)
        {
            var participation = _participations
                .FirstOrDefault(p => p.EventId == eventId && p.UserId == userId);

            if (participation == null) return false;

            _participations.Remove(participation);
            GetById(eventId)!.ParticipationCount -= 1;
            return true;
        }

        public Event? RescheduleEvent(int eventId, DateTime newDate)
        {
            var existingEvent = GetById(eventId);
            if (existingEvent == null) return null;

            existingEvent.Date = newDate;
            return existingEvent;
        }
    }
}

