using EMS.Api.Data;
using EMS.Api.DTOs.Onboarding;
using EMS.Api.Hubs; // 1. Hub ko import karein
using EMS.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR; // 2. SignalR ko import karein
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
    private readonly IHubContext<NotificationHub> _hubContext; // 3. HubContext variable banayein
    private readonly ILogger<OnboardingController> _logger; // Logger add karein

    // 4. Constructor mein sabko inject karein
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

        if (requestingUserRole != "admin" && requestingUserRole != "hr_manager" && requestingUserId != employeeId)
        {
            return Forbid("You can only view your own onboarding checklist.");
        }

        var isEmployeeInCompany = await _context.Users.AnyAsync(u => u.Id == employeeId && u.CompanyId == companyId);
        if (!isEmployeeInCompany)
        {
            return NotFound("Employee not found in your company.");
        }

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
            var employee = await _context.Users.FindAsync(checklistDto.EmployeeId);
            if (employee == null || employee.CompanyId != companyId)
            {
                return NotFound("Employee not found in your company.");
            }

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
            
            // 5. Signal bhejein
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