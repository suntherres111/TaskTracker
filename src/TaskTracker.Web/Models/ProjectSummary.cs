namespace TaskTracker.Web.Models;

/// <summary>Project as returned by GET /api/projects.</summary>
public sealed record ProjectSummary(
    int Id,
    string Name,
    string? Description,
    DateTime CreatedAtUtc,
    int TaskCount);
