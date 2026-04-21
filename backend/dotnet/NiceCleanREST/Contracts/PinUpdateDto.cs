using NiceCleanLib.Enums;

namespace NiceCleanREST.Contracts;

public class PinUpdateDto
{
    public PollutionSeverity Severity { get; set; }
    public double Radius { get; set; }
    public PollutionType PollutionType { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string LocationName { get; set; } = string.Empty;
}
