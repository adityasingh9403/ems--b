using EMS.Api.Data;
using EMS.Api.DTOs.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;

namespace EMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin,hr_manager")]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetReportSummary()
    {
        // --- FIX: Yahaan claim ka naya naam use karein ---
        var companyIdClaim = User.FindFirstValue("urn:ems:companyid");
        if (!int.TryParse(companyIdClaim, out var companyId))
        {
            return Unauthorized("Invalid company information in token.");
        }

        // --- OPTIMIZED QUERY 1: Headcount by Department ---
        var headcountData = await _context.Users
            .Where(u => u.CompanyId == companyId && u.DepartmentId != null)
            .GroupBy(u => new { u.DepartmentId, u.Department!.Name })
            .Select(g => new ChartDataItem { Name = g.Key.Name, Value = g.Count() })
            .ToListAsync();

        // --- OPTIMIZED QUERY 2: Attendance Trends ---
        var sevenDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-6));
        var recentAttendanceGroups = await _context.Attendance
            .Where(a => a.CompanyId == companyId && a.Date >= sevenDaysAgo)
            .GroupBy(a => new { a.Date, a.Status })
            .Select(g => new { g.Key.Date, g.Key.Status, Count = g.Count() })
            .ToListAsync();
        
        var attendanceTrendData = Enumerable.Range(0, 7)
            .Select(offset => DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-offset)))
            .Select(date => new AttendanceTrendItem
            {
                Date = date.ToString("ddd"),
                Present = recentAttendanceGroups.FirstOrDefault(g => g.Date == date && g.Status == "present")?.Count ?? 0,
                Late = recentAttendanceGroups.FirstOrDefault(g => g.Date == date && g.Status == "late")?.Count ?? 0,
                Absent = recentAttendanceGroups.FirstOrDefault(g => g.Date == date && g.Status == "absent")?.Count ?? 0
            }).Reverse().ToList();

        var leaveTypeData = await _context.LeaveRequests
            .Where(l => l.CompanyId == companyId && l.Status == "approved")
            .GroupBy(l => l.LeaveType)
            .Select(g => new ChartDataItem { Name = g.Key, Value = g.Count() })
            .ToListAsync();
            
        // --- OPTIMIZED QUERY 4: Average Salary by Department ---
        var salaryData = await _context.Users
            .Where(u => u.CompanyId == companyId && u.DepartmentId != null && u.Salary.HasValue)
            .GroupBy(u => new { u.DepartmentId, u.Department!.Name })
            .Select(g => new ChartDataItem { 
                Name = g.Key.Name, 
                Value = Math.Round(g.Average(u => u.Salary) ?? 0, 2) 
            })
            .ToListAsync();
        
        var summary = new ReportSummaryDto
        {
            HeadcountData = headcountData,
            AttendanceTrendData = attendanceTrendData,
            LeaveTypeData = leaveTypeData,
            SalaryData = salaryData
        };

        return Ok(summary);
    }
}