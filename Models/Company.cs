using System.ComponentModel.DataAnnotations.Schema; // YEH LINE ADD KAREIN

namespace EMS.Api.Models;

public class Company
{
    [Column("id")] // MySQL column name
    public int Id { get; set; }
    
    [Column("company_code")] // MySQL column name
    public required string CompanyCode { get; set; }
    
    [Column("name")] // MySQL column name
    public required string Name { get; set; }
    
    [Column("owner_email")] // MySQL column name
    public required string OwnerEmail { get; set; }
    
    [Column("created_at")] // MySQL column name
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
