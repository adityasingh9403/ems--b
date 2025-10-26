// DTOs/Auth/RegisterDto.cs
using System.ComponentModel.DataAnnotations;

namespace EMS.Api.DTOs.Auth;

public class RegisterDto
{
    [Required]
    public required string CompanyName { get; set; }
    [Required]
    public required string FirstName { get; set; }
    [Required]
    public required string LastName { get; set; }
    [Required, EmailAddress]
    public required string Email { get; set; }
    [Required]
    public required string Password { get; set; }
}
