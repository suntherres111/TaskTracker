using Microsoft.Extensions.Logging;
using TaskTracker.Domain.Notifications;

namespace TaskTracker.Infrastructure.Notifications;

/// <summary>
/// Local stand-in for a real message broker: "publishes" by writing a structured log entry.
/// Logs identifiers and status only. Free-text fields such as the title are deliberately
/// left out of logs, since users can type anything into them. No credentials or tokens
/// are part of the notification.
/// See docs/integration-design.md for the Azure Service Bus implementation.
/// </summary>
public sealed class LoggingNotificationPublisher : INotificationPublisher
{
    private readonly ILogger<LoggingNotificationPublisher> _logger;

    public LoggingNotificationPublisher(ILogger<LoggingNotificationPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(TaskCreatedNotification notification, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(
            "Notification published (local): {EventType} {EventId} for task {TaskId} in project {ProjectId}, status {Status}, due {DueDateUtc}, occurred {OccurredAtUtc}.",
            TaskCreatedNotification.EventType,
            notification.EventId,
            notification.TaskId,
            notification.ProjectId,
            notification.Status,
            notification.DueDateUtc?.ToString("O") ?? "none",
            notification.OccurredAtUtc.ToString("O"));

        return Task.CompletedTask;
    }
}
