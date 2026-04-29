using NiceCleanLib.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace NiceCleanLib.Models;

public class Report
{
    public int ReportId { get; set; }
    public int EventId { get; set; }
    public int NumberOfBags { get; set; }
    public BagVolume BagVolume { get; set; }
    public DateTime CreatedAt { get; set; }

    public Report(int reportId, int eventId, int numberOfBags, BagVolume bagVolume, DateTime createdAt)
    {
        ReportId = reportId;
        EventId = eventId;
        NumberOfBags = numberOfBags;
        BagVolume = bagVolume;
        CreatedAt = createdAt;
    }
}
