using EMS.Api.Data;
using EMS.Api.DTOs.Leaves;
using EMS.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using EMS.Api.Hubs;

namespace EMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;

    public LeaveController(ApplicationDbContext context, IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    // GET: api/Leave (Ismein koi change nahi)
    [HttpGet]
    public async Task<IActionResult> GetLeaveRequests()
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userRole = User.FindFirstValue(ClaimTypes.Role)!;

        var query = _context.LeaveRequests.Where(l => l.CompanyId == companyId);

        if (userRole == "employee")
        {
            query = query.Where(l => l.RequestorId == userId);
        }
        else if (userRole == "department_manager")
        {
            var manager = await _context.Users.FindAsync(userId);
            if (manager?.DepartmentId != null)
            {
                var teamMemberIds = await _context.Users
                    .Where(u => u.DepartmentId == manager.DepartmentId)
                    .Select(u => u.Id)
                    .ToListAsync();
                query = query.Where(l => teamMemberIds.Contains(l.RequestorId));
            }
            else
            {
                query = query.Where(l => l.RequestorId == userId);
            }
        }

        var requests = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();
        return Ok(requests);
    }

    // POST: api/Leave (YEH UPDATE HUA HAI)
    [HttpPost]
    public async Task<IActionResult> ApplyForLeave([FromBody] LeaveRequestDto leaveDto)
    {
        if (leaveDto.StartDate > leaveDto.EndDate)
        {
            return BadRequest(new { message = "Start date cannot be after end date." });
        }

        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentUser = await _context.Users.FindAsync(userId);
        if (currentUser == null) return Unauthorized();

        var overlappingRequest = await _context.LeaveRequests.FirstOrDefaultAsync(l =>
            l.RequestorId == userId &&
            l.Status != "rejected" &&
            l.StartDate <= leaveDto.EndDate &&
            l.EndDate >= leaveDto.StartDate
        );

        if (overlappingRequest != null)
        {
            return BadRequest(new { message = "You already have a pending or approved leave request for these dates." });
        }

        var newRequest = new LeaveRequest
        {
            CompanyId = companyId,
            RequestorId = userId,
            RequestorName = $"{currentUser.FirstName} {currentUser.LastName}",
            LeaveType = leaveDto.LeaveType,
            StartDate = leaveDto.StartDate,
            EndDate = leaveDto.EndDate,
            Reason = leaveDto.Reason,
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };
        _context.LeaveRequests.Add(newRequest);

        // --- NAYA NOTIFICATION CODE ---
        var newNotification = new Notification
        {
            CompanyId = companyId,
            // Yeh message bell icon mein dikhega
            Message = $"{currentUser.FirstName} {currentUser.LastName} applied for {leaveDto.LeaveType}."
        };
        _context.Notifications.Add(newNotification);
        // --- END NAYA CODE ---

        await _context.SaveChangesAsync(); // Dono cheezein (leave + notification) ek saath save hongi

        // Signal bhejein (taaki Header component refresh ho)
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "LeaveRequestUpdated");

        return CreatedAtAction(nameof(GetLeaveRequests), new { id = newRequest.Id }, newRequest);
    }

    // PUT: api/Leave/{id}/status (YEH BHI UPDATE HUA HAI)
    [HttpPut("{requestId}/status")]
    [Authorize(Roles = "admin,hr_manager,department_manager")]
    public async Task<IActionResult> UpdateStatus(int requestId, [FromBody] UpdateLeaveStatusDto statusDto)
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var actingUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var actingUser = await _context.Users.FindAsync(actingUserId);
        if (actingUser == null) return Unauthorized();

        var request = await _context.LeaveRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.CompanyId == companyId);
        if (request == null) return NotFound("Leave request not found.");

        if (actingUser.Role == "department_manager")
        {
            var requestor = await _context.Users.FindAsync(request.RequestorId);
            if (requestor?.DepartmentId != actingUser.DepartmentId)
            {
                return Forbid("You can only manage leave requests for your own department.");
            }
        }

        request.Status = statusDto.Status;
        request.ActionById = actingUser.Id;
        request.ActionByName = $"{actingUser.FirstName} {actingUser.LastName}";
        request.ActionTimestamp = DateTime.UtcNow;

        // --- NAYA NOTIFICATION CODE ---
        // Requestor ko batayein ki uski leave ka status update hua
        var newNotification = new Notification
        {
            CompanyId = companyId,
            Message = $"Your {request.LeaveType} request from {request.StartDate} was {statusDto.Status} by {actingUser.FirstName}."
            // Hum is notification ko specific user ID se bhi link kar sakte hain (future update)
        };
        _context.Notifications.Add(newNotification);
        // --- END NAYA CODE ---

        await _context.SaveChangesAsync();

        // Signal bhejein
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "LeaveRequestUpdated");

        return Ok(request);
    }
}