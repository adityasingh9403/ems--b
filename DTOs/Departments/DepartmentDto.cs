using System.ComponentModel.DataAnnotations;

namespace EMS.Api.DTOs.Departments;

public class DepartmentDto
{
    // Id sirf update karte waqt zaroori hai, create karte waqt ye 0 hoga
    public int Id { get; set; } 

    [Required(ErrorMessage = "Department name is required.")]
    [StringLength(100, ErrorMessage = "Department name cannot be longer than 100 characters.")]
    public required string Name { get; set; }

    public string? Description { get; set; }

    public int? ManagerId { get; set; }

    public bool IsActive { get; set; } = true;
}

