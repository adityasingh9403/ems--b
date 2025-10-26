// DTOs/Performance/GoalDto.cs
using System.ComponentModel.DataAnnotations;

namespace EMS.Api.DTOs.Performance;

public class GoalDto
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public required string GoalDescription { get; set; }

    public DateOnly TargetDate { get; set; }
    
    public string Status { get; set; } = "not_started";
}
