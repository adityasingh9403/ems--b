using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EMS.Api.DTOs.Onboarding;

// Represents a single item in the checklist
public class ChecklistItemDto
{
    [Required]
    public required string Text { get; set; }
    public bool Completed { get; set; }
}

public class OnboardingChecklistDto
{
    [Required]
    public int EmployeeId { get; set; }

    // --- FIXED: Renamed 'Checklist' to 'Tasks' for consistency with the controller ---
    [Required]
    public required List<ChecklistItemDto> Tasks { get; set; }
}

