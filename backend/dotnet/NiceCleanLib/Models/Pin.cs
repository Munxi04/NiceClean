using System;
using System.Collections.Generic;
using System.Text;

namespace NiceCleanLib.Models;

public class Pin
{
    public int PinId { get; }
    public DateTime Date { get; set; }
    public string Name { get; set; }
    public string Severity { get; set; }
    public string PollutionType { get; set; }
    public int Radius { get; set; }
    public string Status { get; set; }

    public Pin(int pinId, DateTime date, string name, string pollutionSeverity, string pollutionType, int radius, string pinStatus)
    {
        PinId = pinId;
        Date = date;
        Name = name;
        Severity = pollutionSeverity;
        PollutionType = pollutionType;
        Radius = radius;
        Status = pinStatus;
    }
}
