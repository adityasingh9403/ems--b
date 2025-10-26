namespace EMS.Api.DTOs.Employees;

public class EmployeeListDto
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Role { get; set; }
    public string? Designation { get; set; }
    public int? DepartmentId { get; set; }
    public required string EmploymentStatus { get; set; }
    public bool FaceRegistered { get; set; }
    
    // --- YEH LINE ADD KAREIN ---
    public decimal? Salary { get; set; }
}