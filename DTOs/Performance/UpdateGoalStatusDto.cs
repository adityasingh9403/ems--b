using System.ComponentModel.DataAnnotations;

namespace EMS.Api.DTOs.Performance;

public class UpdateGoalStatusDto
{
    [Required]
    public required string Status { get; set; } // e.g., "in_progress", "completed"
}