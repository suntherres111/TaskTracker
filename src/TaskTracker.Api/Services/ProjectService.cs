using Microsoft.EntityFrameworkCore;
using TaskTracker.Api.Contracts.Projects;
using TaskTracker.Domain.Entities;
using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Api.Services;

public enum DeleteProjectResult
{
    Deleted,
    NotFound,
    HasTasks
}

/// <summary>
/// Project use cases: EF Core queries and entity/DTO mapping, kept out of the controller.
/// </summary>
public class ProjectService
{
    private readonly TaskTrackerDbContext _db;

    public ProjectService(TaskTrackerDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ProjectResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _db.Projects
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => new ProjectResponse(p.Id, p.Name, p.Description, p.CreatedAtUtc, p.Tasks.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _db.Projects
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProjectResponse(p.Id, p.Name, p.Description, p.CreatedAtUtc, p.Tasks.Count))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var project = new Project
        {
            Name = request.Name.Trim(),
            Description = Normalize(request.Description),
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync(cancellationToken);

        return new ProjectResponse(project.Id, project.Name, project.Description, project.CreatedAtUtc, 0);
    }

    /// <returns>The updated project, or null if it does not exist.</returns>
    public async Task<ProjectResponse?> UpdateAsync(int id, UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (project is null)
        {
            return null;
        }

        project.Name = request.Name.Trim();
        project.Description = Normalize(request.Description);
        await _db.SaveChangesAsync(cancellationToken);

        var taskCount = await _db.Tasks.CountAsync(t => t.ProjectId == id, cancellationToken);
        return new ProjectResponse(project.Id, project.Name, project.Description, project.CreatedAtUtc, taskCount);
    }

    /// <summary>
    /// The FK is configured with cascade delete, so deleting a project in the database would also
    /// delete its tasks. To avoid silently losing tasks, a project can only be deleted once it is empty.
    /// </summary>
    public async Task<DeleteProjectResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (project is null)
        {
            return DeleteProjectResult.NotFound;
        }

        var hasTasks = await _db.Tasks.AnyAsync(t => t.ProjectId == id, cancellationToken);
        if (hasTasks)
        {
            return DeleteProjectResult.HasTasks;
        }

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync(cancellationToken);
        return DeleteProjectResult.Deleted;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
