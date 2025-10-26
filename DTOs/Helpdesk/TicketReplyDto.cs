// DTOs/Helpdesk/TicketReplyDto.cs
using System.ComponentModel.DataAnnotations;

namespace EMS.Api.DTOs.Helpdesk;

public class TicketReplyDto
{
    [Required]
    public required string ReplyText { get; set; }
}
