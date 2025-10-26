using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace EMS.Api.Models;

[Table("chat_messages")]
public class ChatMessage
{
    [Column("id")]
    public int Id { get; set; }

    [Column("company_id")]
    public int CompanyId { get; set; }

    // --- CHANGE: string se int kiya ---
    [Column("user_id")]
    public int UserId { get; set; } 

    [Column("user_name")]
    public required string UserName { get; set; }

    [Column("message_text")]
    public required string Message { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}