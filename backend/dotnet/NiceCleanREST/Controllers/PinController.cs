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

    public PinController(IPinRepository pinRepo, IUserRepository userRepo)
    {
        _pinRepo = pinRepo;
        _userRepo = userRepo;
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
            date: DateTime.UtcNow,
            name: dto.Name,
            severity: dto.Severity,
            pollutionType: dto.PollutionType,
            radius: 100,
            status: initialStatus,
            latitude: dto.Latitude,
            longitude: dto.Longitude
        );

        var created = _pinRepo.Add(pin);

        return Created(
            Url.ActionContext.HttpContext.Request.Path + "/" + created.Id,
            created
        );
    }

    // PUT api/<PinController>/5
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Pin> Put(int id, Pin pinData)
    {
        var updated = _pinRepo.Update(id, pinData);

        if (updated == null)
        {
            return NotFound();
        }

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
