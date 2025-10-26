using EMS.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompanyController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CompanyController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("details")]
    public async Task<IActionResult> GetCompanyDetails()
    {
        var companyId = int.Parse(User.FindFirstValue("urn:ems:companyid")!);
        var company = await _context.Companies.FindAsync(companyId);

        if (company == null)
        {
            return NotFound("Company details not found.");
        }

        return Ok(new { name = company.Name });
    }
}