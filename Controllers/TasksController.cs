using EMS.Api.Data;
using EMS.Api.DTOs.Tasks;
using EMS.Api.Hubs;
using EMS.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks; // Task (System.Threading.Tasks) ko import karein

namespace EMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<TasksController> _logger; 

    public TasksController(ApplicationDbContext context, IHubContext<NotificationHub> hubContext, ILogger<TasksController> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    // GET: api/Tasks (Ismein koi change nahi)
    [HttpGet]
    public async Task<IActionResult> GetTasks()
    {
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
                
                query = query.Where(t => teamMemberIds.Contains(t.AssignedToId) || t.AssignedToId == userId);
            }
            else
            {
                query = query.Where(t => t.AssignedToId == userId); 
            }
        }

        var tasks = await query.OrderBy(t => t.DueDate).ToListAsync();
        return Ok(tasks);
    }
    
    // POST: api/Tasks (YEH UPDATE HUA HAI)
    [HttpPost]
    [Authorize(Roles = "admin,hr_manager,department_manager")]
    public async Task<IActionResult> CreateTask([FromBody] TaskDto taskDto)
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var assignerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        var assignerUser = await _context.Users.FindAsync(assignerId);
        var assignedToUser = await _context.Users.FindAsync(taskDto.AssignedToId);
        
        // --- YEH RAHA FIX 1 ---
        if (assignerUser == null)
            return Unauthorized("Assigner user (manager) not found.");
        // --- END FIX 1 ---

        if (assignedToUser == null)
            return NotFound(new { message = "Employee to assign task to was not found." });

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
        
        var newNotification = new Notification
        {
            CompanyId = companyId,
            Message = $"{assignerUser.FirstName} assigned a new task to {assignedToUser.FirstName}: '{taskDto.Title}'."
        };
        _context.Notifications.Add(newNotification);

        await _context.SaveChangesAsync();
        
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "TaskUpdated");

        return CreatedAtAction(nameof(GetTasks), new { id = newTask.Id }, newTask);
    }

    // PUT: api/Tasks/5 (Ismein change ki zaroorat nahi)
    [HttpPut("{id}")]
    [Authorize(Roles = "admin,hr_manager,department_manager")]
    public async Task<IActionResult> UpdateTask(int id, [FromBody] TaskDto taskDto)
    {
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

        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "TaskUpdated");
        
        return Ok(task);
    }
    
    // PATCH: api/Tasks/{id}/status (YEH UPDATE HUA HAI)
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] UpdateTaskStatusDto statusDto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentUser = await _context.Users.FindAsync(userId);
        var task = await _context.Tasks.FindAsync(id);
        
        if (task == null) return NotFound("Task not found.");

        // --- YEH RAHA FIX 2 ---
        if (currentUser == null)
            return Unauthorized("Current user not found.");
        // --- END FIX 2 ---

        if (task.AssignedToId != userId)
        {
             return Forbid("You can only update the status of tasks assigned to you.");
        }

        task.Status = statusDto.Status;

        if (statusDto.Status == "completed")
        {
            var assignerUser = await _context.Users.FindAsync(task.AssignedById);
            var newNotification = new Notification
            {
                CompanyId = task.CompanyId,
                Message = $"{currentUser.FirstName} {currentUser.LastName} completed the task: '{task.Title}'."
            };
            _context.Notifications.Add(newNotification);
        }
        
        await _context.SaveChangesAsync();

        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "TaskUpdated");
        
        return Ok(task);
    }

    // DELETE: api/Tasks/5 (Ismein change ki zaroorat nahi)
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin,hr_manager,department_manager")]
    public async Task<IActionResult> DeleteTask(int id)
    {
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
        
        var newNotification = new Notification
        {
            CompanyId = task.CompanyId,
            Message = $"A task '{task.Title}' was deleted."
        };
        _context.Notifications.Add(newNotification);

        await _context.SaveChangesAsync();
        
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "TaskUpdated");

        return Ok(new { message = "Task deleted successfully." });
    }
}