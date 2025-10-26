using System;

namespace EMS.Api.DTOs.SuperAdmin;

public class CompanyStatDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CompanyCode { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int UserCount { get; set; }
}