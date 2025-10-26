using EMS.Api.Data;
using EMS.Api.DTOs.Tasks;
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
public class TasksController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext; // 3. HubContext variable banayein

    // 4. Constructor mein IHubContext ko inject karein
    public TasksController(ApplicationDbContext context, IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    // GET: api/Tasks
    [HttpGet]
    public async Task<IActionResult> GetTasks()
    {
        // ... (Is function mein koi change nahi hai)
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userRole = User.FindFirstValue(ClaimTypes.Role)!;
        
        var query = _context.Tasks.Where(t => t.CompanyId == companyId);

        if (userRole == "employee")
        {
            query = query.Where(t => t.AssignedToId == userId);
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
                query = query.Where(t => teamMemberIds.Contains(t.AssignedToId));
            }
            else
            {
                query = query.Where(t => t.AssignedToId == userId); // No team, just see own tasks
            }
        }

        var tasks = await query.OrderBy(t => t.DueDate).ToListAsync();
        return Ok(tasks);
    }
    
    // POST: api/Tasks
    [HttpPost]
    [Authorize(Roles = "admin,hr_manager,department_manager")]
    public async Task<IActionResult> CreateTask([FromBody] TaskDto taskDto)
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var assignerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        var newTask = new Models.Task
        {
            CompanyId = companyId,
            AssignedById = assignerId,
            Title = taskDto.Title,
            Description = taskDto.Description,
            AssignedToId = taskDto.AssignedToId,
            DueDate = taskDto.DueDate,
            Priority = taskDto.Priority,
            Status = "todo"
        };

        _context.Tasks.Add(newTask);
        await _context.SaveChangesAsync();
        
        // Signal bhejein
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "TaskUpdated");

        return CreatedAtAction(nameof(GetTasks), new { id = newTask.Id }, newTask);
    }

    // PUT: api/Tasks/5
    [HttpPut("{id}")]
    [Authorize(Roles = "admin,hr_manager,department_manager")]
    public async Task<IActionResult> UpdateTask(int id, [FromBody] TaskDto taskDto)
    {
        // ... (Logic to find and check permissions)
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userRole = User.FindFirstValue(ClaimTypes.Role)!;

        var task = await _context.Tasks.FindAsync(id);
        if (task == null) return NotFound("Task not found.");

        if (task.CompanyId != companyId)
        {
            return Forbid();
        }

        if (userRole == "department_manager" && task.AssignedById != userId)
        {
            return Forbid("You can only edit tasks that you have assigned.");
        }
        
        task.Title = taskDto.Title;
        task.Description = taskDto.Description;
        task.AssignedToId = taskDto.AssignedToId;
        task.DueDate = taskDto.DueDate;
        task.Priority = taskDto.Priority;
        
        await _context.SaveChangesAsync();

        // Signal bhejein
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "TaskUpdated");
        
        return Ok(task);
    }
    
    // PATCH: api/Tasks/{id}/status
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] UpdateTaskStatusDto statusDto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var task = await _context.Tasks.FindAsync(id);
        if (task == null) return NotFound("Task not found.");

        if (task.AssignedToId != userId)
        {
             return Forbid("You can only update the status of tasks assigned to you.");
        }

        task.Status = statusDto.Status;
        
        await _context.SaveChangesAsync();

        // 5. YAHAN PAR BHI SIGNAL BHEJEIN
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "TaskUpdated");
        
        return Ok(task);
    }

    // DELETE: api/Tasks/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin,hr_manager,department_manager")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        // ... (Logic to find and check permissions)
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userRole = User.FindFirstValue(ClaimTypes.Role)!;
        
        var task = await _context.Tasks.FindAsync(id);
        if (task == null) return NotFound("Task not found.");

        if (task.CompanyId != companyId)
        {
            return Forbid();
        }

        if (userRole == "department_manager" && task.AssignedById != userId)
        {
            return Forbid("You can only delete tasks that you have assigned.");
        }

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
        
        // Signal bhejein
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "TaskUpdated");

        return Ok(new { message = "Task deleted successfully." });
    }
}