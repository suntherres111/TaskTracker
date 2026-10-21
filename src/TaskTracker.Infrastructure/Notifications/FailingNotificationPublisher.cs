using TaskTracker.Domain.Notifications;

namespace TaskTracker.Infrastructure.Notifications;

/// <summary>
/// Development-only publisher that always fails, used to demonstrate that a notification
/// outage does not fail or roll back task creation. Enabled only in the Development
/// environment with: dotnet run ... -- --Notifications:SimulateFailure=true
/// </summary>
public sealed class FailingNotificationPublisher : INotificationPublisher
{
    public Task PublishAsync(TaskCreatedNotification notification, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Simulated notification failure (Notifications:SimulateFailure=true).");
}
