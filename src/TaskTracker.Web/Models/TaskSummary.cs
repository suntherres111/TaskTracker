namespace TaskTracker.Web.Models;

/// <summary>Task as returned by the API. All DateTime values are UTC.</summary>
public sealed record TaskSummary(
    int Id,
    int ProjectId,
    string Title,
    string? Description,
    TaskItemStatus Status,
    DateTime? DueDateUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
