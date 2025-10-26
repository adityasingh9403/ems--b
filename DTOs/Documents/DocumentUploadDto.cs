using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace EMS.Api.DTOs.Documents;

public class DocumentUploadDto
{
    [Required]
    public required string DocumentType { get; set; }

    [Required]
    public required IFormFile File { get; set; }
    
    // Optional: Admin/HR can specify an employee ID
    // If not provided, it's a company-wide document
    public int? EmployeeId { get; set; } 
}