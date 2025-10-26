using System.ComponentModel.DataAnnotations;
using System;

namespace EMS.Api.DTOs.Performance;

public class PerformanceReviewDto
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public required string ReviewPeriod { get; set; } // e.g., "Q4 2025"

    [Range(1, 5)]
    public int Rating { get; set; }
    
    public string? Comments { get; set; }
}