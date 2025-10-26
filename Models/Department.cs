using System.ComponentModel.DataAnnotations.Schema;

namespace EMS.Api.Models;

[Table("departments")]
public class Department
{
    [Column("id")]
    public int Id { get; set; }

    [Column("company_id")]
    public int CompanyId { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("manager_id")]
    public int? ManagerId { get; set; }
    
    // --- FIX: Add the missing Column attribute ---
    [Column("is_active")]
    public bool IsActive { get; set; }
}