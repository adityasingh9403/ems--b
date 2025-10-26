using System.ComponentModel.DataAnnotations.Schema;

namespace EMS.Api.Models;

[Table("onboarding_checklists")]
public class OnboardingChecklist
{
    [Column("id")]
    public int Id { get; set; }

    [Column("employee_id")]
    public int EmployeeId { get; set; }

    [Column("company_id")]
    public int CompanyId { get; set; }

    // Inside OnboardingChecklist.cs
    [Column("checklist_json", TypeName = "json")]
    public required string ChecklistJson { get; set; }
}

