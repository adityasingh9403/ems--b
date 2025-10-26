using System.ComponentModel.DataAnnotations;

namespace EMS.Api.DTOs.Tasks;

public class UpdateTaskStatusDto
{
    [Required]
    public required string Status { get; set; }
}