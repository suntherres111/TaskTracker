using TaskTracker.Domain.Enums;

namespace TaskTracker.Domain.Entities;

/// <summary>
/// A unit of work within a project. Named TaskItem to avoid clashing with
/// System.Threading.Tasks.Task.
/// </summary>
public class TaskItem
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;

    public DateTime? DueDateUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public int ProjectId { get; set; }

    public Project? Project { get; set; }
}
