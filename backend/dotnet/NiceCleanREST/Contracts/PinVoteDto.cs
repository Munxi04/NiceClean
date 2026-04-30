using System.ComponentModel.DataAnnotations;
using NiceCleanLib.Enums;

namespace NiceCleanREST.Contracts;

/// <summary>
/// Pin vote request contract with required fields validated.
/// </summary>
public class PinVoteDto
{
    [Required(ErrorMessage = "User ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "User ID must be greater than 0")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "Vote type is required")]
    public VoteType VoteType { get; set; }
}
