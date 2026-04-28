namespace NiceCleanREST.Contracts;

public class EventCreateDto
{
    public int PinId { get; set; }
    public int HostUserId { get; set; }
    public string HostNickname { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
}
