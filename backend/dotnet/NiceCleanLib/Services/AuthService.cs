using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NiceCleanLib.Services;

public interface IAuthService
{
    /// <summary>
    /// Generates JWT token for authenticated user
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <param name="email">The user's email address</param>
    /// <param name="role">The user's role (default: User)</param>
    /// <returns>JWT token string</returns>
    string GenerateJwt(int userId, string email, string role);

    /// <summary>
    /// Verifies a plaintext password against a BCrypt hash.
    /// </summary>
    /// <param name="password">The plaintext password to verify</param>
    /// <param name="hash">The BCrypt hash to verify against</param>
    /// <returns>True if password matches hash; false otherwise</returns>
    bool VerifyPassword(string password, string hash);

    /// <summary>
    /// Hashes a plaintext password using BCrypt.
    /// </summary>
    /// <param name="password">The plaintext password to hash</param>
    /// <returns>BCrypt hash of the password</returns>
    string HashPassword(string password);
}

public class AuthService : IAuthService
{
    private readonly IConfiguration _config;

    public AuthService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateJwt(int userId, string email, string role)
    {
        var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured in appsettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured"),
            audience: _config["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured"),
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool VerifyPassword(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);

    public string HashPassword(string password)
        => BCrypt.Net.BCrypt.HashPassword(password);
}
