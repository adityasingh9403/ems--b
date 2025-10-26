using EMS.Api.Data;
using EMS.Api.DTOs.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace EMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        // --- FIX: Yahaan claim ka naya naam use karein ---
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userRole = User.FindFirstValue(ClaimTypes.Role)!;
        var today = DateOnly.FromDateTime(System.DateTime.UtcNow);

        var stats = new DashboardStatsDto();

        if (userRole == "admin" || userRole == "hr_manager")
        {
            stats.TotalEmployees = await _context.Users.CountAsync(u => u.CompanyId == companyId && u.Role != "admin");
            stats.TotalDepartments = await _context.Departments.CountAsync(d => d.CompanyId == companyId);
            stats.PendingLeaves = await _context.LeaveRequests.CountAsync(l => l.CompanyId == companyId && l.Status == "pending");
            stats.PresentToday = await _context.Attendance.CountAsync(a => a.CompanyId == companyId && a.Date == today && (a.Status == "present" || a.Status == "late"));
        }
        else if (userRole == "department_manager")
        {
            var manager = await _context.Users.FindAsync(userId);
            if (manager?.DepartmentId == null)
            {
                return Ok(new DashboardStatsDto { TeamCount = 0, TeamPendingLeaves = 0, TeamPresentToday = 0 });
            }

            var teamMemberIds = await _context.Users
                .Where(u => u.DepartmentId == manager.DepartmentId && u.Role == "employee")
                .Select(u => u.Id)
                .ToListAsync();
            
            stats.TeamCount = teamMemberIds.Count;
            
            if (teamMemberIds.Any())
            {
                stats.TeamPendingLeaves = await _context.LeaveRequests.CountAsync(l => teamMemberIds.Contains(l.RequestorId) && l.Status == "pending");
                stats.TeamPresentToday = await _context.Attendance.CountAsync(a => teamMemberIds.Contains(a.UserId) && a.Date == today && (a.Status == "present" || a.Status == "late"));
            }
            else
            {
                stats.TeamPendingLeaves = 0;
                stats.TeamPresentToday = 0;
            }
        }
        else if (userRole == "employee")
        {
            stats.MyPendingLeaves = await _context.LeaveRequests.CountAsync(l => l.RequestorId == userId && l.Status == "pending");
            stats.MyTasksPending = await _context.Tasks.CountAsync(t => t.AssignedToId == userId && t.Status != "completed");
        }

        return Ok(stats);
    }
}