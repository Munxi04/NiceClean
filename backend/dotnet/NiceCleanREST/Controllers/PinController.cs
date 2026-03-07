using Microsoft.AspNetCore.Mvc;
using NiceCleanLib.Models;
using NiceCleanLib.Services.Interfaces;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace NiceCleanREST.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PinController : ControllerBase
{
    private readonly IPinRepository _repo;

    public PinController(IPinRepository repo)
    {
        _repo = repo;
    }

    // GET: api/<PinController>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult<IEnumerable<Pin>> Get()
    {
        var result = _repo.GetAll();

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
        var pin = _repo.GetById(id);

        if (pin == null)
        {
            return NotFound();
        }

        return Ok(pin);
    }

    // POST api/<PinController>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<Pin> Post(Pin pin)
    {
        var created = _repo.Add(pin);

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
        var updated = _repo.Update(id, pinData);

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
        var deleted = _repo.Delete(id);

        if (deleted == null)
        {
            return NotFound();
        }

        return Ok(deleted);
    }
}
