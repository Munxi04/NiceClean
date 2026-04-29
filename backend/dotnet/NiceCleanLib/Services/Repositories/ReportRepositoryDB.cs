using NiceCleanLib.Data;
using NiceCleanLib.Models;
using NiceCleanLib.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NiceCleanLib.Services.Repositories;

public class ReportRepositoryDB : IReportRepository
{
    private readonly NiceCleanDbContext _context;

    public ReportRepositoryDB(NiceCleanDbContext context)
    {
        _context = context;
    }

    public Report Add(Report report)
    {
        _context.Reports.Add(report);
        _context.SaveChanges();
        return report;
    }

    public bool HasReportForEvent(int eventId)
    {
        return _context.Reports.Any(r => r.EventId == eventId);
    }

    public Report? GetReportByEventId(int eventId)
    {
        return _context.Reports.FirstOrDefault(r => r.EventId == eventId);
    }
}
