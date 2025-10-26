using System;
using System.Collections.Generic;

namespace EMS.Api.DTOs.SuperAdmin;

public class SuperAdminDashboardDto
{
    public int TotalCompanies { get; set; }
    public int TotalUsers { get; set; }
    public int NewCompaniesLast30Days { get; set; }
    public List<CompanyStatDto> Companies { get; set; } = new();
}