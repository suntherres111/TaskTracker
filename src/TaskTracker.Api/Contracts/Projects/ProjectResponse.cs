namespace TaskTracker.Api.Contracts.Projects;

public record ProjectResponse(
    int Id,
    string Name,
    string? Description,
    DateTime CreatedAtUtc,
    int TaskCount);
