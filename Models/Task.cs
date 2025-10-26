using System.ComponentModel.DataAnnotations.Schema;

namespace EMS.Api.Models;

[Table("tasks")]
public class Task
{
    [Column("id")]
    public int Id { get; set; }
    
    [Column("company_id")]
    public int CompanyId { get; set; }
    
    [Column("title")]
    public required string Title { get; set; }
    
    [Column("description")]
    public string? Description { get; set; }
    
    [Column("assigned_to_id")]
    public int AssignedToId { get; set; }
    
    [Column("assigned_by_id")]
    public int AssignedById { get; set; }
    
    [Column("due_date")]
    public DateOnly DueDate { get; set; }
    
    [Column("priority")]
    public string Priority { get; set; } = "medium";
    
    [Column("status")]
    public string Status { get; set; } = "todo";
}