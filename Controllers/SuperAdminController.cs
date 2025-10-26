using EMS.Api.Data;
using EMS.Api.DTOs.SuperAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "super_admin")]
public class SuperAdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SuperAdminController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var totalCompanies = await _context.Companies.CountAsync();
        var totalUsers = await _context.Users.CountAsync();
        
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var newCompanies = await _context.Companies
            .CountAsync(c => c.CreatedAt >= thirtyDaysAgo);
            
        var companies = await _context.Companies
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CompanyStatDto
            {
                Id = c.Id,
                Name = c.Name,
                CompanyCode = c.CompanyCode,
                OwnerEmail = c.OwnerEmail,
                CreatedAt = c.CreatedAt,
                UserCount = _context.Users.Count(u => u.CompanyId == c.Id)
            })
            .ToListAsync();

        var dashboardData = new SuperAdminDashboardDto
        {
            TotalCompanies = totalCompanies,
            TotalUsers = totalUsers,
            NewCompaniesLast30Days = newCompanies,
            Companies = companies
        };

        return Ok(dashboardData);
    }
}