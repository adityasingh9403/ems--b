using System.ComponentModel.DataAnnotations;

namespace EMS.Api.DTOs.Chat;

public class ChatMessageDto
{
    [Required]
    public required string Message { get; set; }
}

