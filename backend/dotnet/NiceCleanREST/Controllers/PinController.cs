using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NiceCleanLib.Enums;
using NiceCleanLib.Models;
using NiceCleanLib.Services.Interfaces;
using NiceCleanREST.Contracts;

namespace NiceCleanREST.Controllers;

/// <summary>
/// Pin management API endpoints for pollution report creation, updates, and voting.
/// </summary>
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

    /// <summary>
    /// Get all pins (public endpoint).
    /// </summary>
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

    /// <summary>
    /// Get pin by ID (public endpoint).
    /// </summary>
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

    /// <summary>
    /// Check if user has voted on a pin (public endpoint).
    /// </summary>
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

    /// <summary>
    /// Get pin near location (public endpoint).
    /// </summary>
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

    /// <summary>
    /// Check if user is near a location (public endpoint).
    /// </summary>
    [HttpGet("isUserNear")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<bool> IsUserNear([FromQuery] double userLat, [FromQuery] double userLon, [FromQuery] double targetLat, [FromQuery] double targetLon)
    {
        bool isNear = _pinRepo.IsUserNear(userLat, userLon, targetLat, targetLon, Pin.StandardRadiusMeters);

        return Ok(isNear);
    }

    /// <summary>
    /// Create a new pollution pin (requires authentication).
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<Pin> Post([FromBody] PinCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

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
            locationName: dto.LocationName,
            hasEvent: false,
            eventId: 0
        );

        var created = _pinRepo.Add(pin);

        return Created(
            Url.ActionContext.HttpContext.Request.Path + "/" + created.Id,
            created
        );
    }

    /// <summary>
    /// Vote on a pin (requires authentication).
    /// </summary>
    [HttpPost("{id}/vote")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<Pin> Vote(int id, [FromBody] PinVoteDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

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

    /// <summary>
    /// Update a pin (requires authentication).
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<Pin> Put(int id, [FromBody] PinUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

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

    /// <summary>
    /// Delete a pin (requires authentication).
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

