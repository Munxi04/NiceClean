using NiceCleanLib.Models;
using NiceCleanLib.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NiceCleanLib.Services.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly List<Report> _reports = new();
    private int _nextReportId = 1;

    public Report Add(Report report)
    {
        report.ReportId = _nextReportId++;
        _reports.Add(report);
        return report;
    }

    public bool HasReportForEvent(int eventId)
    {
        return _reports.Any(r => r.EventId == eventId);
    }

    public Report? GetReportByEventId(int eventId)
    {
        return _reports.FirstOrDefault(r => r.EventId == eventId);
    }
}
