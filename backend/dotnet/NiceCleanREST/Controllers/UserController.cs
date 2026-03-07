using Microsoft.AspNetCore.Mvc;
using NiceCleanLib.Models;
using NiceCleanLib.Services.Interfaces;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace NiceCleanREST.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IUserRepository _repo;

    public UserController(IUserRepository repo)
    {
        _repo = repo;
    }

    // GET: api/<UserController>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult<IEnumerable<User>> Get()
    {
        var result = _repo.GetAll();

        if (result.Count == 0)
        {
            return NoContent();
        }

        return Ok(result);
    }

    // GET api/<UserController>/5
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<User> GetById(int id)
    {
        var user = _repo.GetById(id);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    // POST api/<UserController>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<User> Post(User user)
    {
        var created = _repo.Add(user);

        return Created(
            Url.ActionContext.HttpContext.Request.Path + "/" + created.Id,
            created
        );
    }

    // PUT api/<UserController>/5
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<User> Put(int id, User userData)
    {
        var updated = _repo.Update(id, userData);

        if (updated == null)
        {
            return NotFound();
        }

        return Ok(updated);
    }

    // DELETE api/<UserController>/5
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<User> Delete(int id)
    {
        var deleted = _repo.Delete(id);

        if (deleted == null)
        {
            return NotFound();
        }

        return Ok(deleted);
    }
}
