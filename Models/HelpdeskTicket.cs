using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace EMS.Api.Models;

[Table("helpdesk_tickets")]
public class HelpdeskTicket
{
    [Column("id")]
    public int Id { get; set; }
    
    [Column("company_id")]
    public int CompanyId { get; set; }
    
    [Column("raised_by_id")]
    public int RaisedById { get; set; }

    // --- FIXED: Added missing property ---
    [Column("raised_by_name")]
    public required string RaisedByName { get; set; }
    
    [Column("subject")]
    public required string Subject { get; set; }
    
    [Column("description")]
    public required string Description { get; set; }
    
    [Column("category")]
    public required string Category { get; set; }
    
    [Column("status")]
    public string Status { get; set; } = "open";
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // --- FIXED: Added relationship to TicketReply ---
    public List<TicketReply> Replies { get; set; } = new List<TicketReply>();
}

