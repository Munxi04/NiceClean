namespace NiceCleanREST.Contracts;

public class EventCreateDto
{
    public int PinId { get; set; }
    public int HostUserId { get; set; }
    public DateTime StartTime { get; set; }
}
