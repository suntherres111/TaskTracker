namespace TaskTracker.Domain.Enums;

/// <summary>
/// Workflow state of a task. Named TaskItemStatus to avoid clashing with
/// System.Threading.Tasks.TaskStatus, which is in scope via implicit usings.
/// </summary>
public enum TaskItemStatus
{
    Todo = 0,
    InProgress = 1,
    Done = 2
}
