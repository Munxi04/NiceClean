using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NiceCleanLib.Models;
using NiceCleanLib.Services.Interfaces;
using NiceCleanREST.Contracts;
using NiceCleanREST.Services;

namespace NiceCleanREST.Controllers;

/// <summary>
/// User management API endpoints for authentication and user profile operations.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IUserRepository _repo;
    private readonly IAuthService _authService;

    public UserController(IUserRepository repo, IAuthService authService)
    {
        _repo = repo;
        _authService = authService;
    }

    /// <summary>
    /// Get all users (requires authentication).
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<IEnumerable<User>> Get()
    {
        var result = _repo.GetAll();

        if (result.Count == 0)
        {
            return NoContent();
        }

        return Ok(result);
    }

    /// <summary>
    /// Get user by ID (requires authentication).
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<User> GetById(int id)
    {
        var user = _repo.GetById(id);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    /// <summary>
    /// Register a new user account (public endpoint).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<User> Post([FromBody] LoginDto loginDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if user already exists
        var existingUser = _repo.GetByEmail(loginDto.Email);
        if (existingUser != null)
        {
            return BadRequest("Email already registered.");
        }

        // Create new user with hashed password
        var user = new User(
            id: 0,
            email: loginDto.Email,
            password: _authService.HashPassword(loginDto.Password),
            age: DateTime.Now,
            nickname: loginDto.Email.Split('@')[0], // Default nickname from email
            numberOfWalks: 0,
            isVerified: false
        );

        var created = _repo.Add(user);

        return Created(
            Url.ActionContext.HttpContext.Request.Path + "/" + created.Id,
            created
        );
    }

    /// <summary>
    /// Authenticate user and return JWT token (public endpoint).
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<object> Login([FromBody] LoginDto loginDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = _repo.GetByEmail(loginDto.Email);

        if (user == null || !_authService.VerifyPassword(loginDto.Password, user.Password))
        {
            return Unauthorized("Invalid email or password.");
        }

        // Generate JWT token for authenticated user
        var token = _authService.GenerateJwtToken(user);

        return Ok(new
        {
            success = true,
            token,
            user = new
            {
                user.Id,
                user.Email,
                user.Nickname,
                user.IsVerified
            }
        });
    }

    /// <summary>
    /// Update user profile (requires authentication).
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<User> Put(int id, [FromBody] User userData)
    {
        if (userData == null)
        {
            return BadRequest("User data is required.");
        }

        var updated = _repo.Update(id, userData);

        if (updated == null)
        {
            return NotFound();
        }

        return Ok(updated);
    }

    /// <summary>
    /// Delete user account (requires authentication).
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
