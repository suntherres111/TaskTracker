using TaskTracker.Domain.Enums;

namespace TaskTracker.Api.Contracts.Tasks;

public record TaskResponse(
    int Id,
    int ProjectId,
    string Title,
    string? Description,
    TaskItemStatus Status,
    DateTime? DueDateUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
