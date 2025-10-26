using EMS.Api.Data;
using EMS.Api.DTOs.Departments;
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
public class DepartmentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext; // 3. HubContext variable banayein
    private readonly ILogger<DepartmentsController> _logger; // Logger add karein

    // 4. Constructor mein IHubContext aur ILogger ko inject karein
    public DepartmentsController(ApplicationDbContext context, IHubContext<NotificationHub> hubContext, ILogger<DepartmentsController> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetDepartments()
    {
        try
        {
            var companyIdString = User.FindFirstValue("urn:ems:companyid");
            if (string.IsNullOrEmpty(companyIdString))
            {
                return BadRequest("Company ID not found in token.");
            }

            var companyId = int.Parse(companyIdString);

            var departments = await _context.Departments
                .Where(d => d.CompanyId == companyId)
                .ToListAsync();

            return Ok(departments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching departments.");
            return StatusCode(500, "An internal server error occurred.");
        }
    }

    [HttpPost]
    [Authorize(Roles = "admin,hr_manager")]
    public async Task<IActionResult> CreateDepartment([FromBody] DepartmentDto deptDto)
    {
        try
        {
            var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
            var existingDept = await _context.Departments
                .FirstOrDefaultAsync(d => d.CompanyId == companyId && d.Name == deptDto.Name);

            if (existingDept != null)
            {
                return BadRequest(new { message = $"A department named '{deptDto.Name}' already exists." });
            }

            var newDept = new Department
            {
                CompanyId = companyId,
                Name = deptDto.Name,
                Description = deptDto.Description,
                ManagerId = deptDto.ManagerId == 0 ? null : deptDto.ManagerId,
                IsActive = deptDto.IsActive
            };
            _context.Departments.Add(newDept);
            await _context.SaveChangesAsync();

            // 5. Signal bhejein
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "DepartmentUpdated");

            return CreatedAtAction(nameof(GetDepartments), new { id = newDept.Id }, newDept);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating department {DepartmentName}.", deptDto.Name);
            return StatusCode(500, "An internal server error occurred.");
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin,hr_manager")]
    public async Task<IActionResult> UpdateDepartment(int id, [FromBody] DepartmentDto deptDto)
    {
        try
        {
            var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
            var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId);
            if (department == null) return NotFound("Department not found.");

            department.Name = deptDto.Name;
            department.Description = deptDto.Description;
            department.ManagerId = deptDto.ManagerId == 0 ? null : deptDto.ManagerId;
            department.IsActive = deptDto.IsActive;
            await _context.SaveChangesAsync();

            // 6. Signal bhejein
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "DepartmentUpdated");

            return Ok(department);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating department {DepartmentId}.", id);
            return StatusCode(500, "An internal server error occurred.");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin,hr_manager")]
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        try
        {
            var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
            var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId);
            if (department == null) return NotFound("Department not found.");

            var employeesInDept = await _context.Users.Where(u => u.DepartmentId == id).ToListAsync();
            foreach (var emp in employeesInDept)
            {
                emp.DepartmentId = null;
            }

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();

            // 7. Signal bhejein
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "DepartmentUpdated");

            return Ok(new { message = "Department deleted successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting department {DepartmentId}.", id);
            return StatusCode(500, "An internal server error occurred.");
        }
    }
}
