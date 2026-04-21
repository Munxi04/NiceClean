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

    // POST api/<PinController>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<Pin> Post([FromBody] PinCreateDto dto)
    {
        var user = _userRepo.GetById(dto.UserId);

        var isTrusted = (user?.NumberOfWalks ?? 0) >= WalksThresholdForVerify || (user?.IsVerified ?? false);

        var initialStatus = isTrusted ? PinStatus.Verified : PinStatus.Unverified;

        var pin = new Pin(
            id: 0,
            userId: dto.UserId,
            creationDate: DateTime.UtcNow,
            severity: dto.Severity,
            radius: dto.Radius,
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

        if (dto.VoteType == VoteType.Confirmed)
        {
            int confirmedCount = _voteRepo.GetVoteCount(id, VoteType.Confirmed);

            if (confirmedCount >= 3 && pin.Status == PinStatus.Unverified)
            {
                pin.Status = PinStatus.Verified;
                _pinRepo.Update(id, pin);
            }
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
        existingPin.Radius = dto.Radius;
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
