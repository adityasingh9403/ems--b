using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMS.Api.Models;

[Table("documents")]
public class Document
{
    [Column("id")]
    public int Id { get; set; }
    
    [Column("company_id")]
    public int CompanyId { get; set; }
    
    // FIX: Changed from EmployeeId to match DB schema 'uploaded_for_id'
    [Column("uploaded_for_id")]
    public int? EmployeeId { get; set; } 
    
    // FIX: Added missing property from DB schema
    [Column("uploaded_by_id")]
    public int UploadedById { get; set; }

    // FIX: Changed from DocumentName to match DB schema 'name'
    [Column("name")]
    public required string DocumentName { get; set; }
    
    // FIX: Changed from DocumentType to match DB schema 'type'
    [Column("type")]
    public required string DocumentType { get; set; }
    
    // FIX: Changed from FileUrl to match DB schema 'file_path'
    [Column("file_path")]
    public required string FileUrl { get; set; }
    
    // FIX: Changed from UploadedAt to match DB schema 'created_at'
    [Column("created_at")]
    public DateTime UploadedAt { get; set; }
}