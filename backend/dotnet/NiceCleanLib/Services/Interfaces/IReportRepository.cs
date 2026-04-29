using NiceCleanLib.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace NiceCleanLib.Services.Interfaces;

public interface IReportRepository
{
    Report Add(Report report);
    bool HasReportForEvent(int eventId);
    Report? GetReportByEventId(int eventId);
}
