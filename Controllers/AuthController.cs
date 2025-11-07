using EMS.Api.Data;
using EMS.Api.DTOs.Auth;
using EMS.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging; // 1. Logger namespace ko import karein

namespace EMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger; // 2. Logger ke liye private field banayein

    // 3. Constructor ko update karke ILogger ko inject karein
    public AuthController(ApplicationDbContext context, IConfiguration configuration, ILogger<AuthController> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        // 4. Poore method ko try-catch block mein daalein
        try
        {
            var passwordRegex = new Regex("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[\\W_]).{8,}$");
            if (!passwordRegex.IsMatch(registerDto.Password))
            {
                _logger.LogWarning("Registration failed for company {CompanyName} due to weak password.", registerDto.CompanyName);
                return BadRequest("Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one number, and one special character.");
            }

            var companyCode = registerDto.CompanyName.ToLower().Replace(" ", "-").Replace("'", "");

            if (await _context.Companies.AnyAsync(c => c.CompanyCode.ToLower() == companyCode))
                return BadRequest("Company with this name already exists.");

            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == registerDto.Email.ToLower()))
                return BadRequest("This email is already registered.");

            var company = new Company
            {
                Name = registerDto.CompanyName,
                CompanyCode = companyCode,
                OwnerEmail = registerDto.Email,
                CreatedAt = DateTime.UtcNow
            };
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            var user = new User
            {
                CompanyId = company.Id,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Email = registerDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                Role = "admin",
                Designation = "Administrator",
                JoinDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EmploymentStatus = "active",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);

            var defaultDesignations = new List<Designation>
            {
                new Designation { CompanyId = company.Id, Title = "Administrator", MapsToRole = "admin" },
                new Designation { CompanyId = company.Id, Title = "HR Manager", MapsToRole = "hr_manager" },
                new Designation { CompanyId = company.Id, Title = "Department Manager", MapsToRole = "department_manager" },
                new Designation { CompanyId = company.Id, Title = "Employee", MapsToRole = "employee" }
            };
            await _context.Designations.AddRangeAsync(defaultDesignations);

            await _context.SaveChangesAsync();

            _logger.LogInformation("New company '{CompanyName}' with admin '{AdminEmail}' registered successfully.", company.Name, user.Email);
            return Ok(new { message = "Company and Admin user registered successfully!" });
        }
        catch (Exception ex)
        {
            // 5. Koi bhi anjaan error aane par use log file mein save karein
            _logger.LogError(ex, "An unexpected error occurred during the registration process for company {CompanyName}", registerDto.CompanyName);

            // User ko ek simple error message bhejein taaki system ki details leak na hon
            return StatusCode(500, "An internal server error occurred. Please try again later.");
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            if (loginDto.CompanyCode.Equals("superadmin", StringComparison.OrdinalIgnoreCase))
            {
                var superAdmin = await _context.SuperAdmins.FirstOrDefaultAsync(sa => sa.Email.ToLower() == loginDto.Email.ToLower());
                if (superAdmin != null && BCrypt.Net.BCrypt.Verify(loginDto.Password, superAdmin.PasswordHash))
                {
                    var token = GenerateJwtToken(superAdmin.Id.ToString(), superAdmin.Email, "super_admin", null);
                    _logger.LogInformation("Super Admin '{AdminEmail}' logged in successfully.", superAdmin.Email);
                    return Ok(new
                    {
                        Message = "Super Admin login successful!",
                        Token = token,
                        User = new { Id = superAdmin.Id, FirstName = "Super", LastName = "Admin", Email = superAdmin.Email, Role = "super_admin" }
                    });
                }
                _logger.LogWarning("Failed Super Admin login attempt for email {Email}.", loginDto.Email);
                return Unauthorized(new { message = "Invalid Super Admin credentials." });
            }

            var company = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyCode.ToLower() == loginDto.CompanyCode.ToLower());
            if (company == null)
            {
                _logger.LogWarning("Login failed: Invalid company code '{CompanyCode}' provided.", loginDto.CompanyCode);
                return Unauthorized(new { message = "Invalid Company Code." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == loginDto.Email.ToLower() && u.CompanyId == company.Id);
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Failed login attempt for user {Email} in company {CompanyCode}.", loginDto.Email, loginDto.CompanyCode);
                return Unauthorized(new { message = "Invalid Email or Password for this company." });
            }

            var userToken = GenerateJwtToken(user.Id.ToString(), user.Email, user.Role, user.CompanyId.ToString());
            _logger.LogInformation("User '{Email}' from company '{CompanyCode}' logged in successfully.", user.Email, company.CompanyCode);
            return Ok(new
            {
                Message = "Login successful!",
                Token = userToken,
                User = new { user.Id, user.FirstName, user.LastName, user.Email, user.Role, user.CompanyId, user.Designation, user.FaceDescriptor }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during the login process for user {Email}", loginDto.Email);
            return StatusCode(500, "An internal server error occurred. Please try again later.");
        }
    }

    private string GenerateJwtToken(string userId, string email, string role, string? companyId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]!);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };

        if (companyId != null)
        {
            claims.Add(new Claim("urn:ems:companyid", companyId));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
