using EMS.Api.Data;
using EMS.Api.DTOs.Helpdesk;
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
public class HelpdeskController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext; // 3. HubContext variable banayein

    // 4. Constructor mein IHubContext ko inject karein
    public HelpdeskController(ApplicationDbContext context, IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    // GET: api/helpdesk/tickets
    [HttpGet("tickets")]
    public async Task<IActionResult> GetTickets()
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userRole = User.FindFirstValue(ClaimTypes.Role)!;

        var query = _context.HelpdeskTickets.Where(t => t.CompanyId == companyId);

        if (userRole == "employee")
        {
            query = query.Where(t => t.RaisedById == userId);
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
                query = query.Where(t => teamMemberIds.Contains(t.RaisedById));
            }
            else
            {
                query = query.Where(t => t.RaisedById == userId);
            }
        }
        
        var tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        return Ok(tickets);
    }

    // GET: api/helpdesk/tickets/5
    [HttpGet("tickets/{ticketId}")]
    public async Task<IActionResult> GetTicketById(int ticketId)
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userRole = User.FindFirstValue(ClaimTypes.Role)!;

        var ticket = await _context.HelpdeskTickets
            .Include(t => t.Replies)
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.CompanyId == companyId);

        if (ticket == null) return NotFound();

        if (userRole == "employee" && ticket.RaisedById != userId)
        {
            return Forbid("You can only view your own tickets.");
        }

        return Ok(ticket);
    }

    // POST: api/helpdesk/tickets
    [HttpPost("tickets")]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketDto ticketDto)
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentUser = await _context.Users.FindAsync(userId);

        if (currentUser == null) return Unauthorized();

        var newTicket = new HelpdeskTicket
        {
            CompanyId = companyId,
            RaisedById = userId,
            RaisedByName = $"{currentUser.FirstName} {currentUser.LastName}",
            Subject = ticketDto.Subject,
            Category = ticketDto.Category,
            Description = ticketDto.Description,
            Status = "open",
            CreatedAt = DateTime.UtcNow
        };

        _context.HelpdeskTickets.Add(newTicket);
        await _context.SaveChangesAsync();

        // 5. Signal bhejein
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "HelpdeskUpdated");
        
        return CreatedAtAction(nameof(GetTicketById), new { ticketId = newTicket.Id }, newTicket);
    }

    // PUT: api/helpdesk/tickets/{ticketId}/status
    [HttpPut("tickets/{ticketId}/status")]
    [Authorize(Roles = "admin,hr_manager")]
    public async Task<IActionResult> UpdateTicketStatus(int ticketId, [FromBody] UpdateTicketStatusDto statusDto)
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var ticket = await _context.HelpdeskTickets.FirstOrDefaultAsync(t => t.Id == ticketId && t.CompanyId == companyId);

        if (ticket == null) return NotFound("Ticket not found in your company.");

        ticket.Status = statusDto.Status;
        await _context.SaveChangesAsync();
        
        // 6. Signal bhejein
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "HelpdeskUpdated");
        
        return Ok(ticket);
    }

    // POST: api/helpdesk/tickets/{ticketId}/replies
    [HttpPost("tickets/{ticketId}/replies")]
    public async Task<IActionResult> AddReply(int ticketId, [FromBody] TicketReplyDto replyDto)
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentUser = await _context.Users.FindAsync(userId);
        if (currentUser == null) return Unauthorized();

        var ticket = await _context.HelpdeskTickets.FirstOrDefaultAsync(t => t.Id == ticketId && t.CompanyId == companyId);
        if (ticket == null) return NotFound("Ticket not found in your company.");

        bool canReply = currentUser.Role == "admin" || currentUser.Role == "hr_manager" || ticket.RaisedById == userId;
        if (!canReply)
        {
            return Forbid("You do not have permission to reply to this ticket.");
        }

        var newReply = new TicketReply
        {
            HelpdeskTicketId = ticketId,
            RepliedById = userId,
            RepliedByName = $"{currentUser.FirstName} {currentUser.LastName}",
            ReplyText = replyDto.ReplyText
        };

        _context.TicketReplies.Add(newReply);
        await _context.SaveChangesAsync();
        
        // 7. Signal bhejein
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "HelpdeskUpdated");

        return CreatedAtAction(nameof(GetTicketById), new { ticketId = ticket.Id }, newReply);
    }
}
