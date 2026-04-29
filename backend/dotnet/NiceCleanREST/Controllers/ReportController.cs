using Microsoft.AspNetCore.Mvc;
using NiceCleanLib.Models;
using NiceCleanLib.Services.Interfaces;
using NiceCleanREST.Contracts;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace NiceCleanREST.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReportController : ControllerBase
{
    private readonly IReportRepository _reportRepo;
    private readonly IEventRepository _eventRepo;

    public ReportController(IReportRepository reportRepo, IEventRepository eventRepo)
    {
        _reportRepo = reportRepo;
        _eventRepo = eventRepo;
    }

    // GET api/<ReportController>/event/5/hasReport
    [HttpGet("event/{eventId}/hasReport")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<bool> HasReport(int eventId)
    {
        var hasReport = _reportRepo.HasReportForEvent(eventId);
        return Ok(hasReport);
    }

    // POST api/<ReportController>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<Report> Post([FromBody] ReportCreateDto dto)
    {
        if (dto.NumberOfBags <= 0) return BadRequest("Number of bags must be greater than zero.");

        var existingEvent = _eventRepo.GetById(dto.EventId);
        if (existingEvent == null) return BadRequest("Event not found.");

        if (_reportRepo.HasReportForEvent(dto.EventId))
        {
            return BadRequest("A report for this event has already been submitted.");
        }

        var newReport = new Report(
            reportId: 0,
            eventId: dto.EventId,
            numberOfBags: dto.NumberOfBags,
            bagVolume: dto.BagVolume,
            createdAt: DateTime.UtcNow
        );

        var created = _reportRepo.Add(newReport);

        return Ok(created);
    }
}
