using TaskTracker.Domain.Notifications;

namespace TaskTracker.Api.Tests;

/// <summary>
/// Test double for <see cref="INotificationPublisher"/>: records every publish attempt and,
/// when <see cref="FailWith"/> is set, throws it to simulate a broker outage.
/// </summary>
internal sealed class FakeNotificationPublisher : INotificationPublisher
{
    private readonly List<TaskCreatedNotification> _attempts = new();

    public Exception? FailWith { get; init; }

    /// <summary>Every notification passed to PublishAsync, including ones that then failed.</summary>
    public IReadOnlyList<TaskCreatedNotification> Attempts => _attempts;

    public Task PublishAsync(TaskCreatedNotification notification, CancellationToken cancellationToken)
    {
        _attempts.Add(notification);

        if (FailWith is not null)
        {
            throw FailWith;
        }

        return Task.CompletedTask;
    }
}
