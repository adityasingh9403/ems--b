using System.ComponentModel.DataAnnotations.Schema;

namespace EMS.Api.Models;

[Table("designations")]
public class Designation
{
    [Column("id")]
    public int Id { get; set; }
    
    [Column("company_id")]
    public int CompanyId { get; set; }
    
    [Column("title")]
    public required string Title { get; set; }
    
    [Column("maps_to_role")]
    public required string MapsToRole { get; set; }
}