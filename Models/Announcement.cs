using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMS.Api.Models;

[Table("announcements")]
public class Announcement
{
    [Column("id")]
    public int Id { get; set; }

    [Column("company_id")]
    public int CompanyId { get; set; }

    [Column("content")]
    public required string Content { get; set; }

    [Column("author_name")]
    public required string AuthorName { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}