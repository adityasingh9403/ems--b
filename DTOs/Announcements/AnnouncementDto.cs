using System.ComponentModel.DataAnnotations;

namespace EMS.Api.DTOs.Announcements;

public class AnnouncementDto
{
    [Required]
    public required string Content { get; set; }
}