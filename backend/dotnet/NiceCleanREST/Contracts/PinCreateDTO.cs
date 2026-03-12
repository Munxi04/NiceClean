using NiceCleanLib.Enums;

namespace NiceCleanREST.Contracts;

public class PinCreateDTO
{
    public string Name { get; set; } = string.Empty;
    public PollutionSeverity Severity { get; set; }
    public PollutionType PollutionType { get; set; }
    public double Latatitude { get; set; }
    public double Longitude { get; set; }
}
