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
using Microsoft.AspNetCore.SignalR; // 1. Naya import
using EMS.Api.Hubs;                 // 2. Naya import

namespace EMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext; // 3. Hub ko inject karein

    public LeaveController(ApplicationDbContext context, IHubContext<NotificationHub> hubContext) // 4. Constructor update karein
    {
        _context = context;
        _hubContext = hubContext;
    }

    // GET: api/Leave - Returns leave requests based on user's role
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

    // POST: api/Leave
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
        await _context.SaveChangesAsync();

        // 5. Signal bhejein
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "LeaveRequestUpdated");

        return CreatedAtAction(nameof(GetLeaveRequests), new { id = newRequest.Id }, newRequest);
    }

    // PUT: api/Leave/{id}/status
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

        await _context.SaveChangesAsync();

        // 6. Signal bhejein
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "LeaveRequestUpdated");

        return Ok(request);
    }
}