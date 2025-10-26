using EMS.Api.Data;
using EMS.Api.DTOs.Calendar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CalendarController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CalendarController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents()
    {
        // --- FIX: Yahaan claim ka naya naam use karein ---
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var allEvents = new List<CalendarEventDto>();

        // 1. Get all holidays for the company
        var holidays = await _context.Holidays
            .Where(h => h.CompanyId == companyId)
            .Select(h => new CalendarEventDto
            {
                Date = h.HolidayDate.ToString("yyyy-MM-dd"),
                Description = h.Description,
                Type = "holiday"
            })
            .ToListAsync();
        allEvents.AddRange(holidays);

        // 2. Get all employee birthdays for the company
        var birthdays = await _context.Users
            .Where(u => u.CompanyId == companyId && u.Dob.HasValue)
            .Select(u => new CalendarEventDto
            {
                Date = u.Dob!.Value.ToString("MM-dd"),
                Description = $"{u.FirstName}'s B'day",
                Type = "birthday"
            })
            .ToListAsync();
        allEvents.AddRange(birthdays);
        
        return Ok(allEvents);
    }
}