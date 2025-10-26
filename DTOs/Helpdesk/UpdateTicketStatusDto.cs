using System.ComponentModel.DataAnnotations;

namespace EMS.Api.DTOs.Helpdesk;

public class UpdateTicketStatusDto
{
    [Required]
    public required string Status { get; set; }
}