using System;
using System.Collections.Generic;
using System.Text;
using NiceCleanLib.Enums;

namespace NiceCleanLib.Models
{
    public class Event
    {
        public int EventId { get; set; }
        public DateTime Date { get; set; }
        public EventStatus EventStatus { get; set; }
        public int HostUserId { get; set; }
        public int PinId { get; set; }

        public Event(int eventId, DateTime date, EventStatus eventStatus, int hostUserId, int pinId)
        {
            EventId = eventId;
            Date = date;
            EventStatus = eventStatus;
            HostUserId = hostUserId;
            PinId = pinId;
        }

    }
}
