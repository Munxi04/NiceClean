using Microsoft.AspNetCore.Mvc;
using NiceCleanLib.Enums;
using NiceCleanLib.Models;
using NiceCleanLib.Services.Interfaces;
using NiceCleanREST.Contracts;

namespace NiceCleanREST.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EventController : ControllerBase
{
    private readonly IEventRepository _eventRepo;
    private readonly IPinRepository _pinRepo;
    private readonly IUserRepository _userRepo;

    public EventController(IEventRepository eventRepo, IPinRepository pinRepo, IUserRepository userRepo)
    {
        _eventRepo = eventRepo;
        _pinRepo = pinRepo;
        _userRepo = userRepo;
    }

    // GET: api/<EventController>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult<IEnumerable<EventResponseDto>> Get()
    {
        var events = _eventRepo.GetAll();
        if (!events.Any()) return NoContent();

        var responseList = new List<EventResponseDto>();

        foreach (var ev in events)
        {
            var hostUser = _userRepo.GetById(ev.HostUserId);

            responseList.Add(new EventResponseDto
            {
                EventId = ev.EventId,
                Date = ev.Date,
                EventStatus = ev.EventStatus,
                PinId = ev.PinId,
                HostUserId = ev.HostUserId,
                HostNickname = hostUser?.Nickname ?? "Unknown Host"
            });
        }

        return Ok(responseList);
    }

    // GET api/<EventController>/5
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<EventResponseDto> GetById(int id)
    {
        var ev = _eventRepo.GetById(id);
        if (ev == null) return NotFound();

        var hostUser = _userRepo.GetById(ev.HostUserId);

        var participantCount = _eventRepo.GetParticipantsForEvent(ev.EventId).Count;

        var responseDto = new EventResponseDto
        {
            EventId = ev.EventId,
            Date = ev.Date,
            EventStatus = ev.EventStatus,
            PinId = ev.PinId,
            HostUserId = ev.HostUserId,
            HostNickname = hostUser?.Nickname ?? "Unknown Host",
            ParticipantCount = participantCount
        };

        return Ok(responseDto);
    }

    // GET api/<EventController>/5/hasJoined/2
    [HttpGet("{eventId}/hasJoined/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<bool> HasUserJoined(int eventId, int userId)
    {
        var ev = _eventRepo.GetById(eventId);
        if (ev == null)
        {
            return NotFound("Event not found.");
        }

        bool hasJoined = _eventRepo.HasUserJoined(eventId, userId);

        return Ok(hasJoined);
    }

    // POST api/<EventController>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<Event> Post([FromBody] EventCreateDto dto)
    {
        var hostUser = _userRepo.GetById(dto.HostUserId);
        if (hostUser == null) return BadRequest("Host user does not exist.");

        var pin = _pinRepo.GetById(dto.PinId);
        if (pin == null) return BadRequest("The specified pin does not exist.");
        if (pin.HasEvent) return BadRequest("This pin already has an active event.");

        var newEvent = new Event(
            eventId: 0,
            date: dto.StartTime,
            eventStatus: EventStatus.Pending,
            hostUserId: dto.HostUserId,
            nickname: dto.HostNickname,
            pinId: dto.PinId,
            participationCount: 0
        );

        var created = _eventRepo.Add(newEvent);

        pin.HasEvent = true;
        pin.EventId = created.EventId;
        _pinRepo.Update(pin.Id, pin);

        // Automatically add the host as a participant
        _eventRepo.AddParticipant(created.EventId, hostUser.Id);

        return CreatedAtAction(nameof(GetById), new { id = created.EventId }, created);
    }

    // POST api/<EventController>/5/join
    [HttpPost("{id}/join")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Participation> JoinEvent(int id, [FromBody] ParticipationDto dto)
    {
        var ev = _eventRepo.GetById(id);
        if (ev == null) return NotFound("Event not found.");

        if (ev.EventStatus == EventStatus.Ended) return BadRequest("Cannot join an event that has already ended.");

        var user = _userRepo.GetById(dto.UserId);
        if (user == null) return BadRequest("User does not exist.");

        if (_eventRepo.HasUserJoined(id, dto.UserId)) return BadRequest("User has already joined this event.");

        var participation = _eventRepo.AddParticipant(id, dto.UserId);
        if (participation == null) return BadRequest("Failed to join event.");

        return Ok(participation);
    }

    // PUT api/<EventController>/5/status
    [HttpPut("{id}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Event> UpdateStatus(int id, [FromQuery] EventStatus status, [FromQuery] int hostUserId)
    {
        var ev = _eventRepo.GetById(id);
        if (ev == null) return NotFound("Event not found.");

        if (ev.HostUserId != hostUserId) return BadRequest("Only the host can update the event status.");

        var updatedEv = _eventRepo.UpdateStatus(id, status);

        return Ok(updatedEv);
    }

    // PUT api/<EventController>/5/reschedule
    [HttpPut("{id}/reschedule")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Event> RescheduleEvent(int id, [FromQuery] DateTime newDate, [FromQuery] int hostUserId)
    {
        var ev = _eventRepo.GetById(id);
        if (ev == null) return NotFound("Event not found.");

        if (ev.HostUserId != hostUserId)
            return BadRequest("Only the host can reschedule the event.");

        var updatedEvent = _eventRepo.RescheduleEvent(id, newDate);
        if (updatedEvent == null)
            return BadRequest("Failed to reschedule the event.");

        return Ok(updatedEvent);
    }

    // DELETE api/<EventController>/5/remove/2
    [HttpDelete("{eventId}/remove/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult RemoveParticipant(int eventId, int userId)
    {
        var ev = _eventRepo.GetById(eventId);
        if (ev == null) return NotFound("Event not found.");

        if (!_eventRepo.HasUserJoined(eventId, userId))
            return NotFound("User is not a participant of this event.");

        var success = _eventRepo.RemoveParticipant(eventId, userId);
        if (!success) return BadRequest("Failed to remove participant.");

        return Ok("Participant removed successfully.");
    }
}