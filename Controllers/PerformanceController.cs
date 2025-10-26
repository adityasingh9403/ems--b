using EMS.Api.Data;
using EMS.Api.DTOs.Performance;
using EMS.Api.Hubs; // 1. Hub ko import karein
using EMS.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR; // 2. SignalR ko import karein
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PerformanceController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext; // 3. HubContext variable banayein
    private readonly ILogger<PerformanceController> _logger; // Logger add karein

    // 4. Constructor mein IHubContext aur ILogger ko inject karein
    public PerformanceController(ApplicationDbContext context, IHubContext<NotificationHub> hubContext, ILogger<PerformanceController> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    // ========== GOALS ==========

    [HttpGet("goals/{employeeId}")]
    public async Task<IActionResult> GetGoals(int employeeId)
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var isEmployeeInCompany = await _context.Users.AnyAsync(u => u.Id == employeeId && u.CompanyId == companyId);
        if (!isEmployeeInCompany) return NotFound("Employee not found in your company.");

        var goals = await _context.Goals.Where(g => g.EmployeeId == employeeId).ToListAsync();
        return Ok(goals);
    }

    [HttpPost("goals")]
    [Authorize(Roles = "admin,hr_manager,department_manager")]
    public async Task<IActionResult> SetGoal([FromBody] GoalDto goalDto)
    {
        try
        {
            var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
            var setterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var setterRole = User.FindFirstValue(ClaimTypes.Role)!;

            var employee = await _context.Users.FindAsync(goalDto.EmployeeId);
            if (employee == null || employee.CompanyId != companyId)
            {
                return NotFound("Employee not found in your company.");
            }

            if (setterRole == "department_manager")
            {
                var setter = await _context.Users.FindAsync(setterId);
                if (setter?.DepartmentId != employee.DepartmentId)
                {
                    return Forbid("You can only set goals for employees in your own department.");
                }
            }

            var newGoal = new Goal
            {
                EmployeeId = goalDto.EmployeeId,
                SetById = setterId,
                GoalDescription = goalDto.GoalDescription,
                TargetDate = goalDto.TargetDate,
                Status = "not_started"
            };
            _context.Goals.Add(newGoal);
            await _context.SaveChangesAsync();
            
            // 5. Signal bhejein
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "PerformanceUpdated");

            return CreatedAtAction(nameof(GetGoals), new { employeeId = newGoal.EmployeeId }, newGoal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting goal for employee {EmployeeId}", goalDto.EmployeeId);
            return StatusCode(500, "An internal server error occurred.");
        }
    }

    [HttpPatch("goals/{goalId}/status")]
    public async Task<IActionResult> UpdateGoalStatus(int goalId, [FromBody] UpdateGoalStatusDto statusDto)
    {
        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var goal = await _context.Goals.FindAsync(goalId);

            if (goal == null) return NotFound();

            if (goal.EmployeeId != userId)
            {
                return Forbid("You can only update the status of your own goals.");
            }

            goal.Status = statusDto.Status;
            await _context.SaveChangesAsync();
            
            // 6. Signal bhejein
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "PerformanceUpdated");

            return Ok(goal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for goal {GoalId}", goalId);
            return StatusCode(500, "An internal server error occurred.");
        }
    }

    [HttpDelete("goals/{goalId}")]
    [Authorize(Roles = "admin,hr_manager,department_manager")]
    public async Task<IActionResult> DeleteGoal(int goalId)
    {
        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var goal = await _context.Goals.FindAsync(goalId);

            if (goal == null) return NotFound();

            if (goal.SetById != userId)
            {
                return Forbid("You can only delete goals that you have set.");
            }

            _context.Goals.Remove(goal);
            await _context.SaveChangesAsync();
            
            // 7. Signal bhejein
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "PerformanceUpdated");

            return Ok(new { message = "Goal deleted successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting goal {GoalId}", goalId);
            return StatusCode(500, "An internal server error occurred.");
        }
    }

    // ========== PERFORMANCE REVIEWS ==========

    [HttpGet("reviews/{employeeId}")]
    [Authorize(Roles = "admin,hr_manager,department_manager")]
    public async Task<IActionResult> GetReviews(int employeeId)
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var isEmployeeInCompany = await _context.Users.AnyAsync(u => u.Id == employeeId && u.CompanyId == companyId);
        if (!isEmployeeInCompany) return NotFound("Employee not found in your company.");

        var reviews = await _context.PerformanceReviews
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return Ok(reviews);
    }

    [HttpPost("reviews")]
    [Authorize(Roles = "admin,hr_manager,department_manager")]
    public async Task<IActionResult> AddReview([FromBody] PerformanceReviewDto reviewDto)
    {
        try
        {
            var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
            var reviewerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var employee = await _context.Users.FindAsync(reviewDto.EmployeeId);
            if (employee == null || employee.CompanyId != companyId)
            {
                return NotFound("Employee not found in your company.");
            }

            var newReview = new PerformanceReview
            {
                EmployeeId = reviewDto.EmployeeId,
                ReviewerId = reviewerId,
                ReviewPeriod = reviewDto.ReviewPeriod,
                Rating = reviewDto.Rating,
                Comments = reviewDto.Comments,
                CreatedAt = DateTime.UtcNow
            };

            _context.PerformanceReviews.Add(newReview);
            await _context.SaveChangesAsync();
            
            // 8. Signal bhejein
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "PerformanceUpdated");

            return CreatedAtAction(nameof(GetReviews), new { employeeId = newReview.EmployeeId }, newReview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding review for employee {EmployeeId}", reviewDto.EmployeeId);
            return StatusCode(500, "An internal server error occurred.");
        }
    }

    // --- EMPLOYEE RANKING --- (Ise update karne ki zaroorat nahi)
    [HttpGet("ranking")]
    [Authorize]
    public async Task<IActionResult> GetRanking([FromQuery] string period = "monthly")
    {
        // ... (Poora ranking logic waisa hi rahega)
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);

        var employees = await _context.Users
            .Where(u => u.CompanyId == companyId && u.Role != "admin")
            .ToListAsync();

        var startDate = period == "monthly"
            ? new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1)
            : DateOnly.MinValue;

        var attendance = await _context.Attendance
            .Where(a => a.CompanyId == companyId && a.Date >= startDate)
            .ToListAsync();

        var tasks = await _context.Tasks
            .Where(t => t.CompanyId == companyId && t.DueDate >= startDate)
            .ToListAsync();

        var leaves = await _context.LeaveRequests
            .Where(l => l.CompanyId == companyId && l.Status == "approved" && l.EndDate >= startDate)
            .ToListAsync();

        var rankingList = new List<EmployeeRankingDto>();

        foreach (var emp in employees)
        {
            const int presentScore = 2;
            const int lateScore = -1;
            const int absentScore = -3;
            const int taskCompletedScore = 3;
            const int leaveDayScore = -1;

            var presentDays = attendance.Count(a => a.UserId == emp.Id && a.Status == "present");
            var lateDays = attendance.Count(a => a.UserId == emp.Id && a.Status == "late");
            var absentDays = attendance.Count(a => a.UserId == emp.Id && a.Status == "absent");
            var tasksCompleted = tasks.Count(t => t.AssignedToId == emp.Id && t.Status == "completed");

            int leaveDays = 0;
            foreach (var leave in leaves.Where(l => l.RequestorId == emp.Id))
            {
                for (var date = leave.StartDate; date <= leave.EndDate; date = date.AddDays(1))
                {
                    if (date >= startDate) leaveDays++;
                }
            }

            var totalScore = (presentDays * presentScore) +
                             (lateDays * lateScore) +
                             (absentDays * absentScore) +
                             (tasksCompleted * taskCompletedScore) +
                             (leaveDays * leaveDayScore);

            rankingList.Add(new EmployeeRankingDto
            {
                EmployeeId = emp.Id,
                FullName = $"{emp.FirstName} {emp.LastName}",
                Designation = emp.Designation ?? emp.Role,
                Score = totalScore,
                PresentDays = presentDays,
                LateDays = lateDays,
                AbsentDays = absentDays,
                TasksCompleted = tasksCompleted,
                LeaveDays = leaveDays
            });
        }

        return Ok(rankingList.OrderByDescending(r => r.Score));
    }
}