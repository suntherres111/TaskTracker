using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Api.Contracts.Tasks;

/// <summary>
/// Replaces a task's details. Status is changed separately via PATCH /api/tasks/{id}/status.
/// </summary>
public class UpdateTaskRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public DateTime? DueDateUtc { get; set; }
}
