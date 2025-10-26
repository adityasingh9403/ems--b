// DTOs/Helpdesk/CreateTicketDto.cs
using System.ComponentModel.DataAnnotations;

namespace EMS.Api.DTOs.Helpdesk;

public class CreateTicketDto
{
    [Required]
    public required string Subject { get; set; }
    [Required]
    public required string Category { get; set; }
    [Required]
    public required string Description { get; set; }
}
