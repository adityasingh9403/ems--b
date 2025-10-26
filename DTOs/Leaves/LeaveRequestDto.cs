// DTOs/Leaves/LeaveRequestDto.cs
using System.ComponentModel.DataAnnotations;

namespace EMS.Api.DTOs.Leaves;

public class LeaveRequestDto
{
    [Required]
    public required string LeaveType { get; set; }
    [Required]
    public DateOnly StartDate { get; set; }
    [Required]
    public DateOnly EndDate { get; set; }
    [Required]
    public required string Reason { get; set; }
}
