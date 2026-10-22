using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TaskTracker.Api.Contracts.Tasks;
using TaskTracker.Api.Services;
using TaskTracker.Domain.Enums;
using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Api.Tests;

/// <summary>
/// Tests for <see cref="TaskService.CreateAsync"/> and its notification behaviour.
/// xUnit creates a new instance per test, so each test gets its own database.
/// </summary>
public sealed class TaskServiceCreateTests : IDisposable
{
    private const int ExistingProjectId = 2;   // seeded "Mobile App Launch"
    private const int MissingProjectId = 999;
    private const int SeededTaskCount = 5;

    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task CreateAsync_ValidRequest_PublishesTaskCreatedNotification()
    {
        var publisher = new FakeNotificationPublisher();
        var request = new CreateTaskRequest
        {
            Title = "  Write release notes  ",
            DueDateUtc = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        TaskResponse? created;
        using (var context = _database.CreateContext())
        {
            created = await CreateService(context, publisher).CreateAsync(ExistingProjectId, request, CancellationToken.None);
        }

        Assert.NotNull(created);
        var notification = Assert.Single(publisher.Attempts);
        Assert.NotEqual(Guid.Empty, notification.EventId);
        Assert.Equal(created.Id, notification.TaskId);
        Assert.Equal(ExistingProjectId, notification.ProjectId);
        Assert.Equal("Write release notes", notification.Title);              // trimmed, as saved
        Assert.Equal(nameof(TaskItemStatus.Todo), notification.Status);       // default status
        Assert.Equal(request.DueDateUtc, notification.DueDateUtc);
        Assert.Equal(DateTimeKind.Utc, notification.OccurredAtUtc.Kind);
    }

    [Fact]
    public async Task CreateAsync_WhenPublisherFails_TaskIsStillSavedAndReturned()
    {
        var publisher = new FakeNotificationPublisher
        {
            FailWith = new InvalidOperationException("Simulated broker outage")
        };
        var request = new CreateTaskRequest { Title = "Survives a notification outage", Status = TaskItemStatus.InProgress };

        TaskResponse? created;
        using (var context = _database.CreateContext())
        {
            // Must not throw: publishing is best-effort after the task is committed.
            created = await CreateService(context, publisher).CreateAsync(ExistingProjectId, request, CancellationToken.None);
        }

        Assert.NotNull(created);
        Assert.Single(publisher.Attempts);   // publishing was attempted (and failed)

        // Read back through a fresh context to prove the row was committed, not just tracked.
        using var verify = _database.CreateContext();
        var saved = await verify.Tasks.AsNoTracking().SingleOrDefaultAsync(t => t.Id == created.Id);
        Assert.NotNull(saved);
        Assert.Equal(ExistingProjectId, saved.ProjectId);
        Assert.Equal("Survives a notification outage", saved.Title);
        Assert.Equal(TaskItemStatus.InProgress, saved.Status);
        Assert.Equal(SeededTaskCount + 1, await verify.Tasks.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_UnknownProject_ReturnsNullAndDoesNotPublish()
    {
        var publisher = new FakeNotificationPublisher();
        var request = new CreateTaskRequest { Title = "Orphan task" };

        TaskResponse? created;
        using (var context = _database.CreateContext())
        {
            created = await CreateService(context, publisher).CreateAsync(MissingProjectId, request, CancellationToken.None);
        }

        Assert.Null(created);                // the controller maps null to 404
        Assert.Empty(publisher.Attempts);

        using var verify = _database.CreateContext();
        Assert.Equal(SeededTaskCount, await verify.Tasks.CountAsync());
    }

    private static TaskService CreateService(TaskTrackerDbContext context, FakeNotificationPublisher publisher) =>
        new(context, publisher, NullLogger<TaskService>.Instance);
}
