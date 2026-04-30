using System.ComponentModel.DataAnnotations;

namespace NiceCleanREST.Contracts;

/// <summary>
/// Login request contract with validated email and password fields.
/// </summary>
public class LoginDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(255, MinimumLength = 5, ErrorMessage = "Email must be between 5 and 255 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(255, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 255 characters")]
    public string Password { get; set; } = string.Empty;
}
