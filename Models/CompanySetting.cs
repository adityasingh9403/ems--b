using System.ComponentModel.DataAnnotations.Schema;

namespace EMS.Api.Models;

[Table("company_settings")]
public class CompanySetting
{
    [Column("id")]
    public int Id { get; set; }

    [Column("company_id")]
    public int CompanyId { get; set; }

    [Column("setting_key")]
    public required string Key { get; set; } // e.g., "OfficeStartTime"

    [Column("setting_value")]
    public required string Value { get; set; } // e.g., "09:30"
}