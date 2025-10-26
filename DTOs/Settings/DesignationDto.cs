using System.ComponentModel.DataAnnotations;

namespace EMS.Api.DTOs.Settings;

public class DesignationDto
{
    // --- FIXED: Added the missing Id property ---
    public int Id { get; set; }

    [Required]
    public required string Title { get; set; }

    [Required]
    public required string MapsToRole { get; set; }
}

