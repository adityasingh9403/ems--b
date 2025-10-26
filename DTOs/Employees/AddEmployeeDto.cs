using System;
using System.ComponentModel.DataAnnotations;

namespace EMS.Api.DTOs.Employees;

public class AddEmployeeDto
{
    // Personal Details
    [Required]
    public required string FirstName { get; set; }
    [Required]
    public required string LastName { get; set; }
    [Required, EmailAddress]
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public DateOnly? Dob { get; set; }
    public string? Gender { get; set; }
    public string? MaritalStatus { get; set; }
    public string? CurrentAddress { get; set; }
    public string? PermanentAddress { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactRelation { get; set; }

    // Professional Details
    [Required]
    public required string Password { get; set; }
    [Required]
    public required string Designation { get; set; }
    public int? DepartmentId { get; set; }
    public string? Position { get; set; }
    public decimal? Salary { get; set; }
    public DateOnly JoinDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    // Financial Details
    public string? PanNumber { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankName { get; set; }
    public string? IfscCode { get; set; }
}
