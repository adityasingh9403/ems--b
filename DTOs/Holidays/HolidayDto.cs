// DTOs/Holidays/HolidayDto.cs
using System.ComponentModel.DataAnnotations;

namespace EMS.Api.DTOs.Holidays;

public class HolidayDto
{
    [Required]
    public DateOnly HolidayDate { get; set; }

    [Required]
    public required string Description { get; set; }
}
