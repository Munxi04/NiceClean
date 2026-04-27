using NiceCleanLib.Enums;
using NiceCleanLib.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace NiceCleanLib.Services.Interfaces;

public interface IEventRepository
{
    List<Event> GetAll();
    Event? GetById(int id);
    Event Add(Event newEvent);
    Event? UpdateStatus(int eventId, EventStatus newStatus);
    Event? Delete(int id);

    Participation? AddParticipant(int eventId, int userId);
    List<Participation> GetParticipantsForEvent(int eventId);
    bool HasUserJoined(int eventId, int userId);
}
