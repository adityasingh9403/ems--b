using System.ComponentModel.DataAnnotations.Schema;

namespace EMS.Api.Models;

[Table("super_admins")]
public class SuperAdmin
{
    [Column("id")]
    public int Id { get; set; }

    [Column("email")]
    public required string Email { get; set; }

    [Column("password_hash")]
    public required string PasswordHash { get; set; }
}