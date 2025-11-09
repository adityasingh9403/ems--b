using EMS.Api.Data;
using EMS.Api.DTOs.Onboarding;
using EMS.Api.Hubs;
using EMS.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace EMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OnboardingController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<OnboardingController> _logger;

    public OnboardingController(ApplicationDbContext context, IHubContext<NotificationHub> hubContext, ILogger<OnboardingController> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpGet("{employeeId}")]
    [Authorize]
    public async Task<IActionResult> GetChecklist(int employeeId)
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var requestingUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var requestingUserRole = User.FindFirstValue(ClaimTypes.Role)!;

        // --- NAYA PERMISSION LOGIC ---
        var requestingUser = await _context.Users.FindAsync(requestingUserId);
        if (requestingUser == null) return Unauthorized();

        var targetEmployee = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == employeeId && u.CompanyId == companyId);

        if (targetEmployee == null)
        {
            return NotFound("Employee not found in your company.");
        }

        bool hasPermission = false;

        // 1. Admin/HR kisi ko bhi dekh sakte hain
        if (requestingUserRole == "admin" || requestingUserRole == "hr_manager")
        {
            hasPermission = true;
        }
        // 2. Employee khud ki list dekh sakta hai
        else if (requestingUserId == employeeId)
        {
            hasPermission = true;
        }
        // 3. Dept Manager apni team ke member ki list dekh sakta hai
        else if (requestingUserRole == "department_manager" && targetEmployee.DepartmentId == requestingUser.DepartmentId)
        {
            hasPermission = true;
        }

        if (!hasPermission)
        {
            return Forbid("You do not have permission to view this checklist.");
        }
        // --- END PERMISSION LOGIC ---

        var checklist = await _context.OnboardingChecklists
            .FirstOrDefaultAsync(o => o.EmployeeId == employeeId);

        if (checklist == null)
        {
            return Ok(new List<ChecklistItemDto>());
        }

        var tasks = JsonSerializer.Deserialize<List<ChecklistItemDto>>(checklist.ChecklistJson);
        return Ok(tasks);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> UpdateChecklist([FromBody] OnboardingChecklistDto checklistDto)
    {
        try
        {
            var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
            var requestingUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var requestingUserRole = User.FindFirstValue(ClaimTypes.Role)!;

            // Requesting user ko fetch karein
            var requestingUser = await _context.Users.FindAsync(requestingUserId);
            if (requestingUser == null) return Unauthorized();

            var targetEmployee = await _context.Users.FindAsync(checklistDto.EmployeeId);
            if (targetEmployee == null || targetEmployee.CompanyId != companyId)
            {
                return NotFound("Employee not found in your company.");
            }

            // --- NAYA SECURITY/PERMISSION CHECK ---
            bool hasPermission = false;
            if (requestingUserRole == "admin" || requestingUserRole == "hr_manager")
            {
                hasPermission = true; // Admin/HR kisi ki bhi list edit kar sakte hain
            }
            else if (requestingUserId == checklistDto.EmployeeId)
            {
                hasPermission = true; // User khud ki list edit kar sakta hai
            }
            else if (requestingUserRole == "department_manager" && targetEmployee.DepartmentId == requestingUser.DepartmentId)
            {
                hasPermission = true; // Manager apni team ki list edit kar sakta hai
            }

            if (!hasPermission)
            {
                return Forbid("You do not have permission to update this checklist.");
            }
            // --- END SECURITY CHECK ---

            var existingChecklist = await _context.OnboardingChecklists
                .FirstOrDefaultAsync(o => o.EmployeeId == checklistDto.EmployeeId);

            var tasksAsJson = JsonSerializer.Serialize(checklistDto.Tasks);

            if (existingChecklist != null)
            {
                existingChecklist.ChecklistJson = tasksAsJson;
            }
            else
            {
                var newChecklist = new OnboardingChecklist
                {
                    EmployeeId = checklistDto.EmployeeId,
                    CompanyId = companyId,
                    ChecklistJson = tasksAsJson
                };
                _context.OnboardingChecklists.Add(newChecklist);
            }

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "OnboardingUpdated");

            return Ok(new { message = "Checklist updated successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating checklist for employee {EmployeeId}", checklistDto.EmployeeId);
            return StatusCode(500, "An internal server error occurred.");
        }
    }
}