using EMS.Api.Data;
using EMS.Api.DTOs.Attendance;
using EMS.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using EMS.Api.Hubs;

namespace EMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;

    public AttendanceController(ApplicationDbContext context, IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    [HttpGet]
    [Authorize(Roles = "admin,hr_manager,department_manager")]
    public async Task<IActionResult> GetAttendance()
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userRole = User.FindFirstValue(ClaimTypes.Role)!;

        IQueryable<Attendance> query = _context.Attendance
            .Where(a => a.CompanyId == companyId && a.UserId != 0);

        if (userRole == "department_manager")
        {
            var manager = await _context.Users.FindAsync(userId);
            if (manager?.DepartmentId == null)
            {
                var userRecords = await query.Where(a => a.UserId == userId).ToListAsync();
                var userResult = userRecords.Select(r => new
                {
                    r.Id,
                    r.UserId,
                    r.CompanyId,
                    r.Date,
                    ClockIn = r.ClockIn.HasValue ? DateTime.SpecifyKind(r.ClockIn.Value, DateTimeKind.Utc).ToString("o") : null,
                    ClockOut = r.ClockOut.HasValue ? DateTime.SpecifyKind(r.ClockOut.Value, DateTimeKind.Utc).ToString("o") : null,
                    r.Status,
                    r.ClockInLocation,
                    r.ClockOutLocation
                }).ToList();
                return Ok(userResult);
            }

            var teamMemberIds = await _context.Users
                .Where(u => u.DepartmentId == manager.DepartmentId)
                .Select(u => u.Id)
                .ToListAsync();

            if (!teamMemberIds.Contains(userId))
            {
                teamMemberIds.Add(userId);
            }

            query = query.Where(a => teamMemberIds.Contains(a.UserId));
        }

        var records = await query
            .OrderByDescending(a => a.Date)
            .ThenBy(a => a.UserId)
            .ToListAsync();
        
        var result = records.Select(r => new
        {
            r.Id,
            r.UserId,
            r.CompanyId,
            r.Date,
            ClockIn = r.ClockIn.HasValue ? DateTime.SpecifyKind(r.ClockIn.Value, DateTimeKind.Utc).ToString("o") : null,
            ClockOut = r.ClockOut.HasValue ? DateTime.SpecifyKind(r.ClockOut.Value, DateTimeKind.Utc).ToString("o") : null,
            r.Status,
            r.ClockInLocation,
            r.ClockOutLocation
        }).ToList();

        return Ok(result);
    }
    
    [HttpGet("my-records")]
    public async Task<IActionResult> GetMyAttendance()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var records = await _context.Attendance
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Date)
            .ToListAsync();
        
        var result = records.Select(r => new
        {
            r.Id,
            r.UserId,
            r.CompanyId,
            r.Date,
            ClockIn = r.ClockIn.HasValue ? DateTime.SpecifyKind(r.ClockIn.Value, DateTimeKind.Utc).ToString("o") : null,
            ClockOut = r.ClockOut.HasValue ? DateTime.SpecifyKind(r.ClockOut.Value, DateTimeKind.Utc).ToString("o") : null,
            r.Status,
            r.ClockInLocation,
            r.ClockOutLocation
        }).ToList();

        return Ok(result);
    }

    [HttpPost("mark")]
    public async Task<IActionResult> MarkAttendance([FromBody] MarkAttendanceDto attendanceDto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var isHoliday = await _context.Holidays.AnyAsync(h => h.CompanyId == companyId && h.HolidayDate == today);
        if (isHoliday)
        {
            return BadRequest(new { message = "Cannot mark attendance. Today is a holiday." });
        }

        var isOnLeave = await _context.LeaveRequests.AnyAsync(l =>
            l.RequestorId == userId &&
            l.Status == "approved" &&
            l.StartDate <= today &&
            l.EndDate >= today
        );
        if (isOnLeave)
        {
            return BadRequest(new { message = "Cannot mark attendance. You are on an approved leave." });
        }

        var existingRecord = await _context.Attendance
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Date == today);

        if (existingRecord == null) // Clocking In
        {
            var officeStartTimeSetting = await _context.CompanySettings
                .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.Key == "OfficeStartTime");
            var officeStartTime = TimeOnly.Parse(officeStartTimeSetting?.Value ?? "09:30");

            var timezoneSetting = await _context.CompanySettings
                .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.Key == "Timezone");
            var timezoneId = timezoneSetting?.Value ?? "India Standard Time"; 
            
            TimeOnly currentTime;
            try
            {
                var companyZone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
                var companyLocalTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, companyZone);
                currentTime = TimeOnly.FromDateTime(companyLocalTime);
            }
            catch (TimeZoneNotFoundException)
            {
                currentTime = TimeOnly.FromDateTime(DateTime.UtcNow);
            }
            
            var status = currentTime > officeStartTime.AddMinutes(15) ? "late" : "present";

            var newRecord = new Attendance
            {
                UserId = userId,
                CompanyId = companyId,
                Date = today,
                ClockIn = DateTime.UtcNow,
                Status = status,
                ClockInLocation = attendanceDto.ClockInLocation
            };
            _context.Attendance.Add(newRecord);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "AttendanceUpdated");

            return Ok(new { message = $"Clocked in successfully as {status}." });
        }
        else // Clocking Out
        {
            if (existingRecord.ClockOut != null)
            {
                return BadRequest(new { message = "You have already clocked out for today." });
            }
            existingRecord.ClockOut = DateTime.UtcNow;
            existingRecord.ClockOutLocation = attendanceDto.ClockOutLocation;
            await _context.SaveChangesAsync();
            
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "AttendanceUpdated");

            return Ok(new { message = "Clocked out successfully." });
        }
    }
}

