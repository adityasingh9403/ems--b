using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMS.Api.Models;

[Table("attendance")]
public class Attendance
{
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }
    
    [Column("company_id")] // Assuming you have this column
    public int CompanyId { get; set; }

    [Column("attendance_date")]
    public DateOnly Date { get; set; }

    [Column("clock_in")]
    public DateTime? ClockIn { get; set; }

    [Column("clock_out")]
    public DateTime? ClockOut { get; set; }

    [Column("status")]
    public required string Status { get; set; }

    [Column("clock_in_location")]
    public string? ClockInLocation { get; set; }

    // --- NEW: Added the ClockOutLocation property ---
    [Column("clock_out_location")]
    public string? ClockOutLocation { get; set; }
    
    // Navigation Property
    public User? User { get; set; }
}
