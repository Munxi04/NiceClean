using NiceCleanLib.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace NiceCleanLib.Models;

public class Pin
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Name { get; set; }
    public PollutionSeverity Severity { get; set; }
    public PollutionType PollutionType { get; set; }
    public int Radius { get; set; }
    public PinStatus Status { get; set; }

    public Pin(int id, DateTime date, string name, PollutionSeverity severity, PollutionType pollutionType, int radius, PinStatus status)
    {
        Id = id;
        Date = date;
        Name = name;
        Severity = severity;
        PollutionType = pollutionType;
        Radius = radius;
        Status = status;
    }
}
