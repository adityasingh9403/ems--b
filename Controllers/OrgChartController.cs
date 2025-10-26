using EMS.Api.Data;
using EMS.Api.DTOs.OrgChart;
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
public class OrgChartController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OrgChartController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrgChart()
    {
        // --- FIX: Yahaan claim ka naya naam use karein ---
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);

        var companyUsers = await _context.Users
            .Where(u => u.CompanyId == companyId && u.EmploymentStatus == "active")
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Role, u.DepartmentId })
            .ToListAsync();

        if (!companyUsers.Any())
        {
            return Ok(null);
        }

        var admin = companyUsers.FirstOrDefault(u => u.Role == "admin");
        if (admin == null)
        {
            return NotFound("No admin found to serve as the root of the organization chart.");
        }

        var rootNode = new OrgChartNodeDto
        {
            Id = admin.Id,
            FirstName = admin.FirstName,
            LastName = admin.LastName,
            Role = admin.Role
        };

        var hr = companyUsers.FirstOrDefault(u => u.Role == "hr_manager");
        if (hr != null)
        {
            rootNode.Children.Add(new OrgChartNodeDto { Id = hr.Id, FirstName = hr.FirstName, LastName = hr.LastName, Role = hr.Role });
        }

        var managers = companyUsers.Where(u => u.Role == "department_manager").ToList();
        var employees = companyUsers.Where(u => u.Role == "employee").ToList();

        foreach (var manager in managers)
        {
            var managerNode = new OrgChartNodeDto
            {
                Id = manager.Id,
                FirstName = manager.FirstName,
                LastName = manager.LastName,
                Role = manager.Role
            };
            
            var teamMembers = employees.Where(e => e.DepartmentId == manager.DepartmentId);
            foreach (var member in teamMembers)
            {
                managerNode.Children.Add(new OrgChartNodeDto { Id = member.Id, FirstName = member.FirstName, LastName = member.LastName, Role = member.Role });
            }
            rootNode.Children.Add(managerNode);
        }
        
        return Ok(rootNode);
    }
}