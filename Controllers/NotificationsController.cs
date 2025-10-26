using EMS.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public NotificationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);

        var notifications = await _context.Notifications
            .Where(n => n.CompanyId == companyId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(10) // Get the latest 10 notifications
            .ToListAsync();
            
        return Ok(notifications);
    }
}