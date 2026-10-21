namespace TaskTracker.Domain.Notifications;

/// <summary>
/// Event raised after a task has been saved. Contains business data only, never user
/// credentials or tokens. <see cref="EventId"/> is unique per event so consumers can
/// de-duplicate if a message is delivered more than once.
/// </summary>
public sealed record TaskCreatedNotification(
    Guid EventId,
    DateTime OccurredAtUtc,
    int TaskId,
    int ProjectId,
    string Title,
    string Status,
    DateTime? DueDateUtc)
{
    public const string EventType = "task.created";
}
