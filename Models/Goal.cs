using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMS.Api.Models;

[Table("goals")]
public class Goal
{
    [Column("id")]
    public int Id { get; set; }
    
    // FIX: Changed this to match the database column name
    [Column("user_id")] 
    public int EmployeeId { get; set; }
    
    [Column("set_by_id")]
    public int SetById { get; set; }
    
    [Column("goal_description")]
    public required string GoalDescription { get; set; }
    
    [Column("target_date")]
    public DateOnly TargetDate { get; set; }
    
    [Column("status")]
    public string Status { get; set; } = "not_started";
}