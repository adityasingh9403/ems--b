using EMS.Api.Data;
using EMS.Api.DTOs.Announcements;
using EMS.Api.Hubs;
using EMS.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnnouncementsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<AnnouncementsController> _logger; // Logger add karein

    public AnnouncementsController(ApplicationDbContext context, IHubContext<NotificationHub> hubContext, ILogger<AnnouncementsController> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAnnouncements()
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);

        var announcements = await _context.Announcements
            .Where(a => a.CompanyId == companyId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(20)
            .ToListAsync();
        return Ok(announcements);
    }

    [HttpPost]
    [Authorize(Roles = "admin,hr_manager")]
    public async Task<IActionResult> AddAnnouncement(AnnouncementDto announcementDto)
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentUser = await _context.Users.FindAsync(userId);

        if (currentUser == null) return Unauthorized();

        var newAnnouncement = new Announcement
        {
            CompanyId = companyId,
            Content = announcementDto.Content,
            AuthorName = $"{currentUser.FirstName} {currentUser.LastName}",
            CreatedAt = DateTime.UtcNow
        };
        _context.Announcements.Add(newAnnouncement);

        // --- NAYA NOTIFICATION CODE ---
        var newNotification = new Notification
        {
            CompanyId = companyId,
            Message = $"{currentUser.FirstName} posted a new announcement."
        };
        _context.Notifications.Add(newNotification);
        // --- END NAYA CODE ---

        await _context.SaveChangesAsync(); // Dono (announcement aur notification) ek saath save honge

        // Signal bhejein
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "AnnouncementUpdated");

        return CreatedAtAction(nameof(GetAnnouncements), new { id = newAnnouncement.Id }, newAnnouncement);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin,hr_manager")]
    public async Task<IActionResult> DeleteAnnouncement(int id)
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);

        var announcement = await _context.Announcements
            .FirstOrDefaultAsync(a => a.Id == id && a.CompanyId == companyId);

        if (announcement == null) return NotFound();

        _context.Announcements.Remove(announcement);

        // --- NAYA NOTIFICATION CODE ---
        var newNotification = new Notification
        {
            CompanyId = companyId,
            Message = $"An announcement from {announcement.AuthorName} was deleted."
        };
        _context.Notifications.Add(newNotification);
        // --- END NAYA CODE ---

        await _context.SaveChangesAsync(); // Dono changes ek saath save honge

        // Signal bhejein
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "AnnouncementUpdated");

        return Ok(new { message = "Announcement deleted." });
    }
}