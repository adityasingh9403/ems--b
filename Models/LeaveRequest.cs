using System.ComponentModel.DataAnnotations.Schema;

namespace EMS.Api.Models;

[Table("leave_requests")]
public class LeaveRequest
{
    [Column("id")]
    public int Id { get; set; }
    
    [Column("company_id")]
    public int CompanyId { get; set; }
    
    [Column("requestor_id")]
    public int RequestorId { get; set; }
    
    // --- FIXED: Added missing properties ---
    [Column("requestor_name")]
    public required string RequestorName { get; set; }

    [Column("leave_type")]
    public required string LeaveType { get; set; }
    
    [Column("start_date")]
    public DateOnly StartDate { get; set; }
    
    [Column("end_date")]
    public DateOnly EndDate { get; set; }
    
    [Column("reason")]
    public required string Reason { get; set; }
    
    [Column("status")]
    public string Status { get; set; } = "pending";
    
    [Column("action_by_id")]
    public int? ActionById { get; set; }

    // --- FIXED: Added missing properties ---
    [Column("action_by_name")]
    public string? ActionByName { get; set; }
    
    [Column("action_timestamp")]
    public DateTime? ActionTimestamp { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
