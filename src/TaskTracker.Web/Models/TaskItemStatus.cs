namespace TaskTracker.Web.Models;

/// <summary>
/// Mirrors TaskTracker.Domain.Enums.TaskItemStatus. The Web project talks to the API over HTTP only,
/// so it keeps its own copy; values travel as names ("Todo", "InProgress", "Done").
/// </summary>
public enum TaskItemStatus
{
    Todo,
    InProgress,
    Done
}
