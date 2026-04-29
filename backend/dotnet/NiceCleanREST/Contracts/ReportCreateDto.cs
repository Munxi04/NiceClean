using NiceCleanLib.Enums;

namespace NiceCleanREST.Contracts;

public class ReportCreateDto
{
    public int EventId { get; set; }
    public int NumberOfBags { get; set; }
    public BagVolume BagVolume { get; set; }
}
