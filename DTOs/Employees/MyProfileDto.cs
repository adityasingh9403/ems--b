namespace EMS.Api.DTOs.Employees;

public class MyProfileDto
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Phone { get; set; }
    public string? Bio { get; set; }
    public string? CurrentAddress { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactRelation { get; set; }
}

