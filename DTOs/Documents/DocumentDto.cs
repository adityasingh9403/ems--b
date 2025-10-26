// DTOs/Documents/DocumentDto.cs
using System.ComponentModel.DataAnnotations;

namespace EMS.Api.DTOs.Documents;

public class DocumentDto
{
    [Required]
    public int EmployeeId { get; set; }
    
    [Required]
    public required string DocumentType { get; set; }
    
    [Required]
    public required string DocumentName { get; set; }
    
    [Required]
    public required string FileUrl { get; set; } // In a real app, you'd handle file upload and generate this URL
}
