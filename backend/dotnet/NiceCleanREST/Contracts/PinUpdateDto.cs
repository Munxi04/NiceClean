using System.ComponentModel.DataAnnotations;
using NiceCleanLib.Enums;

namespace NiceCleanREST.Contracts;

/// <summary>
/// Pin update request contract with same geographic and severity validation as creation.
/// </summary>
public class PinUpdateDto
{
    [Required(ErrorMessage = "Latitude is required")]
    [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90 degrees")]
    public double Latitude { get; set; }

    [Required(ErrorMessage = "Longitude is required")]
    [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180 degrees")]
    public double Longitude { get; set; }

    [StringLength(255, ErrorMessage = "Location name cannot exceed 255 characters")]
    public string LocationName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Severity is required")]
    public PollutionSeverity Severity { get; set; }

    [Required(ErrorMessage = "Pollution type is required")]
    public PollutionType PollutionType { get; set; }
}
