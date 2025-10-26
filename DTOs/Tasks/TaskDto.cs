using System;
using System.ComponentModel.DataAnnotations;

namespace EMS.Api.DTOs.Tasks;

public class TaskDto
{
    public int Id { get; set; } // Update ke liye zaroori

    [Required]
    public required string Title { get; set; }
    public string? Description { get; set; }
    [Required]
    public int AssignedToId { get; set; }
    [Required]
    public DateOnly DueDate { get; set; }
    public string Priority { get; set; } = "medium";
    public string Status { get; set; } = "todo";
}

