// DTOs/Auth/LoginDto.cs
using System.ComponentModel.DataAnnotations;

namespace EMS.Api.DTOs.Auth;

public class LoginDto
{
    [Required]
    public required string CompanyCode { get; set; }
    [Required, EmailAddress]
    public required string Email { get; set; }
    [Required]
    public required string Password { get; set; }
}
