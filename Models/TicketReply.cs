using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMS.Api.Models;

[Table("ticket_replies")]
public class TicketReply
{
    [Column("id")]
    public int Id { get; set; }

    [Column("ticket_id")]
    public int HelpdeskTicketId { get; set; }

    [Column("replied_by_id")]
    public int RepliedById { get; set; }
    
    [Column("replied_by_name")]
    public required string RepliedByName { get; set; }
    
    [Column("reply_text")]
    public required string ReplyText { get; set; }
    
    // --- FIX: Tell Entity Framework the database will generate this value ---
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    // Navigation Property to link back to the main ticket
    public HelpdeskTicket? HelpdeskTicket { get; set; }
}