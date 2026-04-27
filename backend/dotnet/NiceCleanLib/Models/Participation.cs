using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

namespace NiceCleanLib.Models
{
    public class Participation
    {
        public int ParticipationId { get; set; }
        public bool IsParticipating { get; set; }
        public int EventId { get; set; }
        public int UserId { get; set; }


        public Participation(int participationId, int userId, int eventId, bool isParticipating)
        {
            ParticipationId = participationId;
            UserId = userId;
            EventId = eventId;
            IsParticipating = isParticipating;
        }

    }
}
