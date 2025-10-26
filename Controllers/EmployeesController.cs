using EMS.Api.Data;
using EMS.Api.DTOs.Employees;
using EMS.Api.Hubs; // 1. Naya import
using EMS.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR; // 2. Naya import
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace EMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext; // 3. Hub ko add karein

    public EmployeesController(ApplicationDbContext context, IHubContext<NotificationHub> hubContext) // 4. Constructor mein inject karein
    {
        _context = context;
        _hubContext = hubContext;
    }

    [HttpGet("my-profile")]
    [Authorize]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpGet]
    [Authorize(Roles = "admin,hr_manager,department_manager")]
    public async Task<IActionResult> GetEmployees()
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var employees = await _context.Users
            .Where(u => u.CompanyId == companyId && u.Role != "admin")
            .Select(u => new EmployeeListDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Role = u.Role,
                Designation = u.Designation,
                DepartmentId = u.DepartmentId,
                EmploymentStatus = u.EmploymentStatus,
                FaceRegistered = u.FaceDescriptor != null
            })
            .ToListAsync();

        return Ok(employees);
    }

    [HttpPost]
    [Authorize(Roles = "admin,hr_manager")]
    public async Task<IActionResult> AddEmployee([FromBody] AddEmployeeDto empDto)
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);

        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == empDto.Email.ToLower() && u.CompanyId == companyId))
        {
            return BadRequest("An employee with this email already exists in the company.");
        }

        var designations = await _context.Designations.Where(d => d.CompanyId == companyId).ToListAsync();
        var designationMap = designations.FirstOrDefault(d => d.Title.Equals(empDto.Designation, StringComparison.OrdinalIgnoreCase));

        var newUser = new User
        {
            CompanyId = companyId,
            FirstName = empDto.FirstName,
            LastName = empDto.LastName,
            Email = empDto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(empDto.Password),
            Role = designationMap?.MapsToRole ?? "employee",
            Designation = empDto.Designation,
            Phone = empDto.Phone,
            Dob = empDto.Dob,
            Gender = empDto.Gender,
            MaritalStatus = empDto.MaritalStatus,
            CurrentAddress = empDto.CurrentAddress,
            PermanentAddress = empDto.PermanentAddress,
            EmergencyContactName = empDto.EmergencyContactName,
            EmergencyContactRelation = empDto.EmergencyContactRelation,
            DepartmentId = empDto.DepartmentId,
            Salary = empDto.Salary,
            JoinDate = empDto.JoinDate,
            PanNumber = empDto.PanNumber,
            BankAccountNumber = empDto.BankAccountNumber,
            BankName = empDto.BankName,
            IfscCode = empDto.IfscCode,
            EmploymentStatus = "active",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        // Signal bhejein
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "EmployeeUpdated");

        return CreatedAtAction(nameof(GetEmployees), new { id = newUser.Id }, newUser);
    }

    [HttpPost("bulk")]
    [Authorize(Roles = "admin,hr_manager")]
    public async Task<IActionResult> BulkImportEmployees([FromBody] List<AddEmployeeDto> employeesDto)
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);

        var existingEmails = await _context.Users
            .Where(u => u.CompanyId == companyId)
            .Select(u => u.Email.ToLower())
            .ToListAsync();

        var designations = await _context.Designations
            .Where(d => d.CompanyId == companyId)
            .ToListAsync();

        var newUsers = new List<User>();
        var skippedCount = 0;

        foreach (var empDto in employeesDto)
        {
            if (string.IsNullOrEmpty(empDto.Email) || existingEmails.Contains(empDto.Email.ToLower()))
            {
                skippedCount++;
                continue;
            }

            var designationMap = designations.FirstOrDefault(d => d.Title.Equals(empDto.Designation, StringComparison.OrdinalIgnoreCase));

            var newUser = new User
            {
                CompanyId = companyId,
                FirstName = empDto.FirstName,
                LastName = empDto.LastName,
                Email = empDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(empDto.Password ?? "password123"),
                Role = designationMap?.MapsToRole ?? "employee",
                Designation = empDto.Designation,
                // ... (baaki ki properties waisi hi rahengi)
                EmploymentStatus = "active",
                CreatedAt = DateTime.UtcNow
            };

            newUsers.Add(newUser);
            existingEmails.Add(newUser.Email.ToLower());
        }

        if (newUsers.Any())
        {
            await _context.Users.AddRangeAsync(newUsers);
            await _context.SaveChangesAsync();
            // Signal bhejein
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "EmployeeUpdated");
        }

        return Ok(new
        {
            Message = $"{newUsers.Count} employees imported successfully.",
            Skipped = skippedCount
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin,hr_manager")]
    public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeDto employeeDto)
    {
        var employee = await _context.Users.FindAsync(id);
        if (employee == null) return NotFound("Employee not found.");

        employee.FirstName = employeeDto.FirstName;
        employee.LastName = employeeDto.LastName;
        // ... (baaki ki properties waisi hi rahengi)

        await _context.SaveChangesAsync();

        // Signal bhejein
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "EmployeeUpdated");

        return Ok(new { message = "Employee updated successfully." });
    }

    [HttpPut("my-profile")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] MyProfileDto profileDto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.FirstName = profileDto.FirstName;
        user.LastName = profileDto.LastName;
        user.Phone = profileDto.Phone;
        user.CurrentAddress = profileDto.CurrentAddress;
        user.EmergencyContactName = profileDto.EmergencyContactName;
        user.EmergencyContactRelation = profileDto.EmergencyContactRelation;

        await _context.SaveChangesAsync();
        return Ok(user);
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "admin,hr_manager")]
    public async Task<IActionResult> UpdateEmployeeStatus(int id, [FromBody] UpdateStatusDto statusDto)
    {
        var employee = await _context.Users.FindAsync(id);
        if (employee == null) return NotFound("Employee not found.");

        employee.EmploymentStatus = statusDto.Status;
        employee.LastWorkingDay = statusDto.LastDay;
        employee.ExitReason = statusDto.Reason;

        await _context.SaveChangesAsync();

        // Signal bhejein
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "EmployeeUpdated");

        return Ok(new { message = "Employee status updated." });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin,hr_manager")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var employee = await _context.Users.FindAsync(id);
        if (employee == null) return NotFound("Employee not found.");

        if (employee.EmploymentStatus == "active")
        {
            return BadRequest("Cannot delete an active employee. Please change their status to inactive first.");
        }

        _context.Users.Remove(employee);
        await _context.SaveChangesAsync();

        // Signal bhejein
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "EmployeeUpdated");

        return Ok(new { message = "Employee permanently deleted." });
    }

    [HttpPost("register-face")]
    public async Task<IActionResult> RegisterFace([FromBody] JsonElement faceDescriptor)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.FaceDescriptor = JsonSerializer.Serialize(faceDescriptor);
        await _context.SaveChangesAsync();

        // --- YEH HAI ASLI FIX ---
        // 'Ok(user)' bhej ne ke bajaye, hum waisa hi saaf object bhejenge jaisa Login method bhejta hai.
        // Isse JSON serialization ki problem nahi hogi.
        var updatedUserData = new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Role,
            user.CompanyId,
            user.Designation,
            user.FaceDescriptor // <-- Sabse zaroori, naya data saath bhejna
        };

        return Ok(updatedUserData);
    }

    [HttpPost("{id}/reset-face")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ResetFace(int id)
    {
        var employee = await _context.Users.FindAsync(id);
        if (employee == null) return NotFound("Employee not found.");

        employee.FaceDescriptor = null;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Face registration has been reset." });
    }

    [HttpPut("{id}/role")]
    [Authorize(Roles = "admin,hr_manager")]
    public async Task<IActionResult> UpdateEmployeeRole(int id, [FromBody] UpdateRoleDto roleDto)
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var actingUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var employeeToUpdate = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.CompanyId == companyId);

        if (employeeToUpdate == null) return NotFound(new { message = "Employee not found." });
        if (employeeToUpdate.Role == "admin") return BadRequest(new { message = "Cannot change the role of an Administrator." });
        if (employeeToUpdate.Id == actingUserId) return BadRequest(new { message = "You cannot change your own role." });

        employeeToUpdate.Role = roleDto.Role;
        await _context.SaveChangesAsync();

        // Signal bhejein
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", "EmployeeUpdated");

        return Ok(new { message = "Employee role updated successfully." });
    }
}