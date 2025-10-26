using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMS.Api.Models;

[Table("notifications")]
public class Notification
{
    [Column("id")]
    public int Id { get; set; }

    [Column("company_id")]
    public int CompanyId { get; set; }

    [Column("message")]
    public required string Message { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}