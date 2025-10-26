using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace EMS.Api.Models;

[Table("users")]
public class User
{
    [Column("id")]
    public int Id { get; set; }
    
    [Column("company_id")]
    public int CompanyId { get; set; }
    
    [Column("department_id")]
    public int? DepartmentId { get; set; }

    [Column("first_name")]
    public required string FirstName { get; set; }
    
    [Column("last_name")]
    public required string LastName { get; set; }
    
    [Column("email")]
    public required string Email { get; set; }
    
    [Column("password_hash")]
    public required string PasswordHash { get; set; }
    
    [Column("role")]
    public required string Role { get; set; }
    
    [Column("designation")]
    public string? Designation { get; set; }

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("dob")]
    public DateOnly? Dob { get; set; }

    [Column("gender")]
    public string? Gender { get; set; }

    [Column("marital_status")]
    public string? MaritalStatus { get; set; }

    [Column("current_address")]
    public string? CurrentAddress { get; set; }

    [Column("permanent_address")]
    public string? PermanentAddress { get; set; }

    [Column("emergency_contact_name")]
    public string? EmergencyContactName { get; set; }

    [Column("emergency_contact_relation")]
    public string? EmergencyContactRelation { get; set; }

    [Column("salary")]
    public decimal? Salary { get; set; }

    // --- FIXED: Made JoinDate nullable to prevent crash on old data ---
    [Column("join_date")]
    public DateOnly? JoinDate { get; set; }

    [Column("pan_number")]
    public string? PanNumber { get; set; }

    [Column("bank_account_number")]
    public string? BankAccountNumber { get; set; }

    [Column("bank_name")]
    public string? BankName { get; set; }

    [Column("ifsc_code")]
    public string? IfscCode { get; set; }

    [Column("employment_status")]
    public string EmploymentStatus { get; set; } = "active";

    [Column("last_working_day")]
    public DateOnly? LastWorkingDay { get; set; }

    [Column("exit_reason")]
    public string? ExitReason { get; set; }

    [Column("face_descriptor", TypeName = "json")]
    public string? FaceDescriptor { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public Company? Company { get; set; }
    public Department? Department { get; set; }
}

