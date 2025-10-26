using EMS.Api.Data;
using EMS.Api.DTOs.Chat;
using EMS.Api.Hubs; // <-- YEH ADD KAREIN
using EMS.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR; // <-- YEH ADD KAREIN
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<ChatHub> _hubContext; // <-- YEH ADD KAREIN

    // Constructor ko update karein
    public ChatController(ApplicationDbContext context, IHubContext<ChatHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext; // <-- YEH ADD KAREIN
    }

    [HttpGet]
    public async Task<IActionResult> GetMessages()
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var messages = await _context.ChatMessages
            .Where(m => m.CompanyId == companyId)
            .OrderBy(m => m.CreatedAt)
            .Take(100)
            .ToListAsync();
        return Ok(messages);
    }

    [HttpPost]
    public async Task<IActionResult> PostMessage(ChatMessageDto chatDto)
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        
        if (!int.TryParse(userIdString, out var userId))
        {
            return Unauthorized("Invalid user ID.");
        }

        var currentUser = await _context.Users.FindAsync(userId);
        if (currentUser == null) return Unauthorized();

        var newMessage = new ChatMessage
        {
            CompanyId = companyId,
            UserId = userId,
            UserName = $"{currentUser.FirstName} {currentUser.LastName}",
            Message = chatDto.Message,
            CreatedAt = DateTime.UtcNow
        };

        _context.ChatMessages.Add(newMessage);
        await _context.SaveChangesAsync();
        
        // --- NEW REAL-TIME LOGIC ---
        // Message save hone ke baad, use SignalR Hub se sabhi clients ko broadcast karein
        await _hubContext.Clients.All.SendAsync("ReceiveMessage", 
            newMessage.Id,
            newMessage.UserId, 
            newMessage.UserName, 
            newMessage.Message,
            newMessage.CreatedAt
        );
        
        return CreatedAtAction(nameof(GetMessages), new { id = newMessage.Id }, newMessage);
    }
}