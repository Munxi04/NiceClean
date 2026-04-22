using Microsoft.AspNetCore.Mvc;
using NiceCleanLib.Enums;
using NiceCleanLib.Models;
using NiceCleanLib.Services.Interfaces;
using NiceCleanREST.Contracts;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace NiceCleanREST.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PinController : ControllerBase
{
    private const int WalksThresholdForVerify = 10;
    
    private readonly IPinRepository _pinRepo;
    private readonly IUserRepository _userRepo;
    private readonly IPinVoteRepository _voteRepo;

    public PinController(IPinRepository pinRepo, IUserRepository userRepo, IPinVoteRepository voteRepo)
    {
        _pinRepo = pinRepo;
        _userRepo = userRepo;
        _voteRepo = voteRepo;
    }

    // GET: api/<PinController>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult<IEnumerable<Pin>> Get()
    {
        var result = _pinRepo.GetAll();

        if (result.Count == 0)
        {
            return NoContent();
        }

        return Ok(result);
    }

    // GET api/<PinController>/5
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Pin> GetById(int id)
    {
        var pin = _pinRepo.GetById(id);

        if (pin == null)
        {
            return NotFound();
        }

        return Ok(pin);
    }

    // GET api/<PinController>/5/hasVoted/2
    [HttpGet("{pinId}/hasVoted/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<bool> HasUserVoted(int pinId, int userId)
    {
        var pin = _pinRepo.GetById(pinId);
        if (pin == null)
        {
            return NotFound("Pin not found.");
        }

        bool cannotVote = pin.UserId == userId || _voteRepo.HasUserVoted(pinId, userId);

        return Ok(cannotVote);
    }

    // GET api/<PinController>/atLocation
    [HttpGet("atLocation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult<Pin> GetPinAtLocation([FromQuery] double latitude, [FromQuery] double longitude)
    {
        var pin = _pinRepo.GetPinAtLocation(latitude, longitude);

        if (pin == null)
        {
            return NoContent();
        }
        return Ok(pin);
    }

    // GET api/<PinController>/isUserNear?userLat=55.6&userLon=12.5&targetLat=55.602&targetLon=12.502
    [HttpGet("isUserNear")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<bool> IsUserNear([FromQuery] double userLat, [FromQuery] double userLon, [FromQuery] double targetLat, [FromQuery] double targetLon)
    {
        bool isNear = _pinRepo.IsUserNear(userLat, userLon, targetLat, targetLon, Pin.StandardRadiusMeters);

        return Ok(isNear);
    }

    // POST api/<PinController>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<Pin> Post([FromBody] PinCreateDto dto)
    {
        var user = _userRepo.GetById(dto.UserId);

        if (user == null)
        {
            return BadRequest("User does not exist.");
        }

        if (_pinRepo.GetPinAtLocation(dto.Latitude, dto.Longitude) != null)
        {
            return BadRequest("A pin already exists within 100 meters of this location.");
        }

        var isTrusted = (user?.NumberOfWalks ?? 0) >= WalksThresholdForVerify || (user?.IsVerified ?? false);

        var initialStatus = isTrusted ? PinStatus.Verified : PinStatus.Unverified;

        var pin = new Pin(
            id: 0,
            userId: dto.UserId,
            creationDate: DateTime.UtcNow,
            severity: dto.Severity,
            radius: Pin.StandardRadiusMeters,
            status: initialStatus,
            pollutionType: dto.PollutionType,
            latitude: dto.Latitude,
            longitude: dto.Longitude,
            locationName: dto.LocationName
        );

        var created = _pinRepo.Add(pin);

        return Created(
            Url.ActionContext.HttpContext.Request.Path + "/" + created.Id,
            created
        );
    }

    // POST api/<PinController>/5/vote
    [HttpPost("{id}/vote")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Pin> Vote(int id, [FromBody] PinVoteDto dto)
    {
        var pin = _pinRepo.GetById(id);

        if (pin == null)
        {
            return NotFound("Pin not found.");
        }

        if (pin.UserId == dto.UserId)
        {
            return BadRequest("You cannot vote on a pin that you created.");
        }

        if (_voteRepo.HasUserVoted(id, dto.UserId))
        {
            return BadRequest("User has already voted on this pin.");
        }

        var newVote = new PinVote(
            id: 0,
            pinId: id,
            userId: dto.UserId,
            voteType: dto.VoteType,
            createdAt: DateTime.UtcNow
        );

        var createdVote = _voteRepo.AddVote(newVote);

        if (createdVote == null)
        {
            return BadRequest("Failed to register vote.");
        }

        int netScore = _voteRepo.GetVoteCount(id);

        if (netScore >= 3 && pin.Status == PinStatus.Unverified)
        {
            pin.Status = PinStatus.Verified;
            _pinRepo.Update(id, pin);
        }
        else if (netScore <= -3 && pin.Status != PinStatus.Deleted)
        {
            pin.Status = PinStatus.Deleted;
            _pinRepo.Update(id, pin);
        }

        return Ok(pin);
    }

    // PUT api/<PinController>/5
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Pin> Put(int id, [FromBody] PinUpdateDto dto)
    {
        var existingPin = _pinRepo.GetById(id);

        if (existingPin == null)
        {
            return NotFound();
        }

        existingPin.Severity = dto.Severity;
        existingPin.PollutionType = dto.PollutionType;
        existingPin.Latitude = dto.Latitude;
        existingPin.Longitude = dto.Longitude;
        existingPin.LocationName = dto.LocationName;

        var updated = _pinRepo.Update(id, existingPin);

        return Ok(updated);
    }

    // DELETE api/<PinController>/5
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Pin> Delete(int id)
    {
        var deleted = _pinRepo.Delete(id);

        if (deleted == null)
        {
            return NotFound();
        }

        return Ok(deleted);
    }
}
