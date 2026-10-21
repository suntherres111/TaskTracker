namespace TaskTracker.Domain.Notifications;

/// <summary>
/// Publishes domain events to an external system (message broker, email, webhook, ...).
/// Implementations live in Infrastructure; the application depends only on this contract,
/// so the local logger can be swapped for e.g. Azure Service Bus without touching callers.
/// </summary>
public interface INotificationPublisher
{
    /// <summary>Publishes the notification. Throws if it could not be published.</summary>
    Task PublishAsync(TaskCreatedNotification notification, CancellationToken cancellationToken);
}
