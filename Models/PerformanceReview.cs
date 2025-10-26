using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMS.Api.Models;

[Table("performance_reviews")]
public class PerformanceReview
{
    [Column("id")]
    public int Id { get; set; }
    
    [Column("user_id")]
    public int EmployeeId { get; set; }
    
    [Column("reviewer_id")]
    public int ReviewerId { get; set; }
    
    // FIX: Removed the 'required' keyword
    [Column("review_period")]
    public string ReviewPeriod { get; set; } = string.Empty;
    
    [Column("rating")]
    public int Rating { get; set; }
    
    [Column("comments")]
    public string? Comments { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}