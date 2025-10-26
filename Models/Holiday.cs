using System.ComponentModel.DataAnnotations.Schema;

namespace EMS.Api.Models;

[Table("holidays")]
public class Holiday
{
    [Column("id")]
    public int Id { get; set; }
    
    [Column("company_id")]
    public int CompanyId { get; set; }
    
    [Column("holiday_date")]
    public DateOnly HolidayDate { get; set; }
    
    [Column("description")]
    public required string Description { get; set; }
}
