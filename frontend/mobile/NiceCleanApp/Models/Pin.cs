namespace NiceCleanApp.Models;

public class Pin
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime CreationDate { get; set; }
    public PollutionSeverity Severity { get; set; }
    public double Radius { get; set; }
    public PinStatus Status { get; set; }
    public PollutionType PollutionType { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string LocationName { get; set; }
}
