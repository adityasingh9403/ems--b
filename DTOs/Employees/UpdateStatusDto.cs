using System;
using System.ComponentModel.DataAnnotations;

namespace EMS.Api.DTOs.Employees;

public class UpdateStatusDto
{
    [Required]
    public required string Status { get; set; }
    public DateOnly? LastDay { get; set; }
    public string? Reason { get; set; }
}
