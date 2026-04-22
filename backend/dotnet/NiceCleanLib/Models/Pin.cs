using NiceCleanLib.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace NiceCleanLib.Models;

public class Pin
{
    public const double StandardRadiusMeters = 100.0;

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

    public Pin(int id, int userId, DateTime creationDate, PollutionSeverity severity, double radius, PinStatus status, PollutionType pollutionType, double latitude, double longitude, string locationName)
    {
        Id = id;
        UserId = userId;
        CreationDate = creationDate;
        Severity = severity;
        Radius = radius;
        Status = status;
        PollutionType = pollutionType;
        Latitude = latitude;
        Longitude = longitude;
        LocationName = locationName;
    }
}
