using EMS.Api.Data;
using EMS.Api.DTOs.Payroll;
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
[Authorize(Roles = "admin,hr_manager")]
public class PayrollController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext; // 3. HubContext variable banayein
    private readonly ILogger<PayrollController> _logger; // Logger add karein

    // 4. Constructor mein sabko inject karein
    public PayrollController(ApplicationDbContext context, IHubContext<NotificationHub> hubContext, ILogger<PayrollController> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpGet("structure/{employeeId}")]
    public async Task<IActionResult> GetSalaryStructure(int employeeId)
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var employee = await _context.Users.FirstOrDefaultAsync(u => u.Id == employeeId && u.CompanyId == companyId);
        if (employee == null)
        {
            return NotFound("Employee not found in your company.");
        }

        var structure = await _context.SalaryStructures
            .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.CompanyId == companyId);

        if (structure == null)
        {
            return Ok(new SalaryStructureDto
            {
                EmployeeId = employeeId,
                GrossSalary = employee.Salary ?? 0
            });
        }

        return Ok(SalaryStructureDto.FromModel(structure, employee.Salary ?? 0));
    }

    [HttpPost("structure")]
    public async Task<IActionResult> SaveSalaryStructure([FromBody] SalaryStructureDto salaryDto)
    {
        try
        {
            var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
            var employee = await _context.Users.FirstOrDefaultAsync(u => u.Id == salaryDto.EmployeeId && u.CompanyId == companyId);
            if (employee == null)
            {
                return Forbid("You cannot set salary for an employee outside your company.");
            }

            var existingStructure = await _context.SalaryStructures
                .FirstOrDefaultAsync(s => s.EmployeeId == salaryDto.EmployeeId && s.CompanyId == companyId);

            if (existingStructure != null)
            {
                existingStructure.Basic = salaryDto.Basic;
                existingStructure.Hra = salaryDto.Hra;
                existingStructure.Allowances = salaryDto.Allowances;
                existingStructure.Pf = salaryDto.Pf;
                existingStructure.Tax = salaryDto.Tax;
            }
            else
            {
                var newStructure = new SalaryStructure
                {
                    EmployeeId = salaryDto.EmployeeId,
                    CompanyId = companyId,
                    Basic = salaryDto.Basic,
                    Hra = salaryDto.Hra,
                    Allowances = salaryDto.Allowances,
                    Pf = salaryDto.Pf,
                    Tax = salaryDto.Tax
                };
                _context.SalaryStructures.Add(newStructure);
            }

            employee.Salary = salaryDto.Basic + salaryDto.Hra + salaryDto.Allowances;
            await _context.SaveChangesAsync();

            // 5. Signal bhejein
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "PayrollUpdated");

            return Ok(new { message = "Salary structure saved successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving salary structure for employee {EmployeeId}", salaryDto.EmployeeId);
            return StatusCode(500, "An internal server error occurred.");
        }
    }
}