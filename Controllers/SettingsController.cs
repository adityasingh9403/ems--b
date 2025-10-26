using EMS.Api.Data;
using EMS.Api.DTOs.Holidays;
using EMS.Api.DTOs.Settings;
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
public class SettingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext; // 3. HubContext variable banayein

    // 4. Constructor mein IHubContext ko inject karein
    public SettingsController(ApplicationDbContext context, IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    // --- DESIGNATIONS ---
    [HttpGet("designations")]
    [Authorize(Roles = "admin,hr_manager")]
    public async Task<IActionResult> GetDesignations()
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var designations = await _context.Designations
            .Where(d => d.CompanyId == companyId)
            .OrderBy(d => d.Title)
            .ToListAsync();
        return Ok(designations);
    }

    [HttpPost("designations")]
    [Authorize(Roles = "admin,hr_manager")]
    public async Task<IActionResult> AddDesignation(DesignationDto designationDto)
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);

        var newDesignation = new Designation
        {
            CompanyId = companyId,
            Title = designationDto.Title,
            MapsToRole = designationDto.MapsToRole
        };

        _context.Designations.Add(newDesignation);
        await _context.SaveChangesAsync();

        // 5. Signal bhejein
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "SettingsUpdated");

        return CreatedAtAction(nameof(GetDesignations), new { id = newDesignation.Id }, newDesignation);
    }

    [HttpDelete("designations/{id}")]
    [Authorize(Roles = "admin,hr_manager")]
    public async Task<IActionResult> DeleteDesignation(int id)
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var designation = await _context.Designations.FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId);
        if (designation == null) return NotFound();

        bool isAssigned = await _context.Users.AnyAsync(u => u.Designation == designation.Title && u.CompanyId == companyId);
        if (isAssigned)
        {
            return BadRequest("Cannot delete this designation as it is currently assigned to one or more employees.");
        }

        _context.Designations.Remove(designation);
        await _context.SaveChangesAsync();

        // 6. Signal bhejein
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "SettingsUpdated");

        return Ok(new { message = "Designation deleted successfully." });
    }

    // --- HOLIDAYS ---
    [HttpGet("holidays")]
    public async Task<IActionResult> GetHolidays()
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var holidays = await _context.Holidays.Where(h => h.CompanyId == companyId).OrderBy(h => h.HolidayDate).ToListAsync();
        return Ok(holidays);
    }

    [HttpPost("holidays")]
    [Authorize(Roles = "admin,hr_manager")]
    public async Task<IActionResult> AddHoliday(HolidayDto holidayDto)
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var existingHoliday = await _context.Holidays.AnyAsync(h => h.CompanyId == companyId && h.HolidayDate == holidayDto.HolidayDate);
        if (existingHoliday)
        {
            return BadRequest("A holiday for this date already exists.");
        }

        var newHoliday = new Holiday
        {
            CompanyId = companyId,
            HolidayDate = holidayDto.HolidayDate,
            Description = holidayDto.Description
        };
        _context.Holidays.Add(newHoliday);
        await _context.SaveChangesAsync();

        // 7. Signal bhejein
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "SettingsUpdated");

        return CreatedAtAction(nameof(GetHolidays), new { id = newHoliday.Id }, newHoliday);
    }

    [HttpDelete("holidays/{id}")]
    [Authorize(Roles = "admin,hr_manager")]
    public async Task<IActionResult> DeleteHoliday(int id)
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var holiday = await _context.Holidays.FirstOrDefaultAsync(h => h.Id == id && h.CompanyId == companyId);
        if (holiday == null) return NotFound();

        _context.Holidays.Remove(holiday);
        await _context.SaveChangesAsync();

        // 8. Signal bhejein
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "SettingsUpdated");

        return Ok(new { message = "Holiday deleted successfully." });
    }

    // --- OFFICE TIMINGS --- (Inmein signal ki zaroorat nahi)
    [HttpGet("office-timings")]
    public async Task<IActionResult> GetOfficeTimings()
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var startTime = await _context.CompanySettings.FirstOrDefaultAsync(s => s.CompanyId == companyId && s.Key == "OfficeStartTime");
        var endTime = await _context.CompanySettings.FirstOrDefaultAsync(s => s.CompanyId == companyId && s.Key == "OfficeEndTime");

        return Ok(new OfficeTimingsDto
        {
            StartTime = startTime?.Value ?? "09:30",
            EndTime = endTime?.Value ?? "18:30"
        });
    }

    [HttpPost("office-timings")]
    [Authorize(Roles = "admin,hr_manager")]
    public async Task<IActionResult> SaveOfficeTimings(OfficeTimingsDto timingsDto)
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);

        var startTimeSetting = await _context.CompanySettings.FirstOrDefaultAsync(s => s.CompanyId == companyId && s.Key == "OfficeStartTime");
        if (startTimeSetting == null)
        {
            _context.CompanySettings.Add(new CompanySetting { CompanyId = companyId, Key = "OfficeStartTime", Value = timingsDto.StartTime ?? "09:30" });
        }
        else
        {
            startTimeSetting.Value = timingsDto.StartTime ?? "09:30";
        }

        var endTimeSetting = await _context.CompanySettings.FirstOrDefaultAsync(s => s.CompanyId == companyId && s.Key == "OfficeEndTime");
        if (endTimeSetting == null)
        {
            _context.CompanySettings.Add(new CompanySetting { CompanyId = companyId, Key = "OfficeEndTime", Value = timingsDto.EndTime ?? "18:30" });
        }
        else
        {
            endTimeSetting.Value = timingsDto.EndTime ?? "18:30";
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Office timings saved successfully." });
    }
}
