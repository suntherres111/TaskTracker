using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Api.Contracts.Tasks;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Notifications;
using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Api.Services;

/// <summary>
/// Task use cases: EF Core queries and entity/DTO mapping, kept out of the controller.
/// Methods return null when the task (or its project) does not exist; the controller maps that to 404.
/// </summary>
public class TaskService
{
    // Used inside EF queries so only the needed columns are selected and translated to SQL.
    private static readonly Expression<Func<TaskItem, TaskResponse>> ToResponseExpression = t =>
        new TaskResponse(t.Id, t.ProjectId, t.Title, t.Description, t.Status, t.DueDateUtc, t.CreatedAtUtc, t.UpdatedAtUtc);

    // Same mapping for entities already in memory (after create/update).
    private static readonly Func<TaskItem, TaskResponse> ToResponse = ToResponseExpression.Compile();

    // Upper bound on how long task creation waits for the notification.
    private static readonly TimeSpan PublishTimeout = TimeSpan.FromSeconds(5);

    private readonly TaskTrackerDbContext _db;
    private readonly INotificationPublisher _notifications;
    private readonly ILogger<TaskService> _logger;

    public TaskService(TaskTrackerDbContext db, INotificationPublisher notifications, ILogger<TaskService> logger)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    /// <returns>The project's tasks, or null if the project does not exist.</returns>
    public async Task<IReadOnlyList<TaskResponse>?> GetForProjectAsync(int projectId, CancellationToken cancellationToken)
    {
        if (!await ProjectExistsAsync(projectId, cancellationToken))
        {
            return null;
        }

        return await _db.Tasks
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.Id)
            .Select(ToResponseExpression)
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _db.Tasks
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(ToResponseExpression)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <returns>The created task, or null if the project does not exist.</returns>
    public async Task<TaskResponse?> CreateAsync(int projectId, CreateTaskRequest request, CancellationToken cancellationToken)
    {
        if (!await ProjectExistsAsync(projectId, cancellationToken))
        {
            return null;
        }

        var task = new TaskItem
        {
            ProjectId = projectId,
            Title = request.Title.Trim(),
            Description = Normalize(request.Description),
            Status = request.Status ?? TaskItemStatus.Todo,
            DueDateUtc = ToUtc(request.DueDateUtc),
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(cancellationToken);

        await PublishTaskCreatedAsync(task);

        return ToResponse(task);
    }

    public async Task<TaskResponse?> UpdateAsync(int id, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (task is null)
        {
            return null;
        }

        task.Title = request.Title.Trim();
        task.Description = Normalize(request.Description);
        task.DueDateUtc = ToUtc(request.DueDateUtc);
        task.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return ToResponse(task);
    }

    public async Task<TaskResponse?> UpdateStatusAsync(int id, TaskItemStatus status, CancellationToken cancellationToken)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (task is null)
        {
            return null;
        }

        task.Status = status;
        task.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return ToResponse(task);
    }

    /// <returns>True if the task was deleted, false if it does not exist.</returns>
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (task is null)
        {
            return false;
        }

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Best-effort notification after the task is committed. The task is already saved, so a
    /// publishing failure is logged and swallowed rather than failing (or appearing to fail) the
    /// request. Uses its own timeout instead of the request token: a client disconnecting after
    /// the save should not skip the notification. Guaranteed delivery would need an outbox
    /// (see docs/integration-design.md).
    /// </summary>
    private async Task PublishTaskCreatedAsync(TaskItem task)
    {
        var notification = new TaskCreatedNotification(
            Guid.NewGuid(),
            DateTime.UtcNow,
            task.Id,
            task.ProjectId,
            task.Title,
            task.Status.ToString(),
            task.DueDateUtc);

        using var timeout = new CancellationTokenSource(PublishTimeout);
        try
        {
            await _notifications.PublishAsync(notification, timeout.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish {EventType} {EventId} for task {TaskId}. The task was saved; the notification was not sent.",
                TaskCreatedNotification.EventType, notification.EventId, task.Id);
        }
    }

    private Task<bool> ProjectExistsAsync(int projectId, CancellationToken cancellationToken) =>
        _db.Projects.AnyAsync(p => p.Id == projectId, cancellationToken);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // Values with an offset arrive as Local; values without one are treated as already UTC.
    private static DateTime? ToUtc(DateTime? value) => value switch
    {
        null => null,
        { Kind: DateTimeKind.Utc } v => v,
        { Kind: DateTimeKind.Local } v => v.ToUniversalTime(),
        var v => DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
    };
}
