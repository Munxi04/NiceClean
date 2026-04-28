using NiceCleanLib.Enums;

namespace NiceCleanREST.Contracts;

public class EventResponseDto
{
    public int EventId { get; set; }
    public DateTime Date { get; set; }
    public EventStatus EventStatus { get; set; }
    public int PinId { get; set; }

    // The new Host properties for the frontend
    public int HostUserId { get; set; }
    public string HostNickname { get; set; } = string.Empty;
    public int ParticipantCount { get; set; }
}
