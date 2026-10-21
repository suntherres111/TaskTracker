# External integration: task notifications

## What runs today (local)

When a task is created, `TaskService` saves it and then publishes a `TaskCreatedNotification`
through `INotificationPublisher`. Locally the publisher is `LoggingNotificationPublisher`, which
writes one structured log line. No external service, keys or emulator are needed.

| Piece | Project | Notes |
|---|---|---|
| `INotificationPublisher`, `TaskCreatedNotification` | Domain | Contract only; plain .NET types, no Azure SDK |
| `LoggingNotificationPublisher` | Infrastructure | Local implementation (logs IDs and status, not free text) |
| Call site: `TaskService.CreateAsync` | Api | Publishes **after** `SaveChangesAsync` succeeds |
| Registration | Api `Program.cs` | `AddSingleton<INotificationPublisher, LoggingNotificationPublisher>()` |

Example log line:

```
Notification published (local): task.created 3f2c… for task 13 in project 2, status Todo, due none, occurred 2026-10-21T09:15:02.1234567Z.
```

### Failure behaviour (current)

Publishing is **best-effort**:

- The task is committed first. If publishing then throws or exceeds the 5-second timeout,
  `TaskService` logs an error with the event ID and task ID, and the API still returns
  **201 Created**. The user's work is never lost or rolled back because a notification failed.
- The publish uses its own timeout rather than the HTTP request's cancellation token, so a
  client that disconnects after the save does not cause the notification to be skipped.
- Trade-off: a failed notification is lost; it is logged but not retried later. The production
  design below closes that gap with an outbox.

**Try it (Development only):** start the API with a publisher that always fails, then create a task.
The API returns 201, the task is saved, and the console shows
`Failed to publish task.created … The task was saved; the notification was not sent.`

```powershell
dotnet run --project src/TaskTracker.Api --launch-profile https -- --Notifications:SimulateFailure=true
```

## Production design: Azure Service Bus

```
 Blazor ──JWT──▶ TaskTracker.Api ──(1) INSERT Task + OutboxMessage, one transaction──▶ Azure SQL
                       │
                       └─(2) background dispatcher ──Managed Identity──▶ Service Bus topic "task-events"
                                                                              │
                                                     subscription "notify" ───┴──▶ Azure Function
                                                                                   (ServiceBusTrigger)
                                                                                   → email (SendGrid) / Teams / etc.
```

### Implementation

A `ServiceBusNotificationPublisher : INotificationPublisher` in Infrastructure, using the
`Azure.Messaging.ServiceBus` and `Azure.Identity` packages. Only Infrastructure gains Azure
dependencies; Domain, the API and the Blazor UI do not change.

```csharp
// Infrastructure — registration (replaces the logging publisher outside Development)
services.AddSingleton(_ => new ServiceBusClient(
    options.FullyQualifiedNamespace,           // "tasktracker-sb.servicebus.windows.net"
    new DefaultAzureCredential(),               // Managed Identity in Azure, dev login locally
    new ServiceBusClientOptions
    {
        RetryOptions = new ServiceBusRetryOptions
        {
            Mode = ServiceBusRetryMode.Exponential,
            MaxRetries = 3,
            Delay = TimeSpan.FromSeconds(0.5),
            MaxDelay = TimeSpan.FromSeconds(10),
            TryTimeout = TimeSpan.FromSeconds(10)
        }
    }));
services.AddSingleton<INotificationPublisher, ServiceBusNotificationPublisher>();

// Publisher — one long-lived sender (thread-safe), JSON body, metadata for routing and de-duplication
var message = new ServiceBusMessage(BinaryData.FromObjectAsJson(notification))
{
    MessageId = notification.EventId.ToString(),   // enables duplicate detection
    Subject = TaskCreatedNotification.EventType,     // "task.created" — subscriptions filter on it
    ContentType = "application/json",
    CorrelationId = Activity.Current?.TraceId.ToString()
};
await _sender.SendMessageAsync(message, cancellationToken);
```

### Configuration

Non-secret values only. There is **no connection string or SAS key**, because authentication
uses Managed Identity.

```json
"Notifications": {
  "Provider": "ServiceBus",
  "ServiceBus": {
    "FullyQualifiedNamespace": "tasktracker-sb.servicebus.windows.net",
    "TopicName": "task-events"
  }
}
```

In Azure App Service these are app settings, e.g. `Notifications__ServiceBus__TopicName`.
Locally `Provider` stays `Log`, so nobody needs Azure access to run the project.

### Authentication: Managed Identity

1. Enable a **system-assigned managed identity** on the App Service hosting the API.
2. Grant it **Azure Service Bus Data Sender**, scoped to the topic, not the whole namespace:
   ```bash
   az role assignment create \
     --assignee <api-managed-identity-principal-id> \
     --role "Azure Service Bus Data Sender" \
     --scope /subscriptions/<sub>/resourceGroups/<rg>/providers/Microsoft.ServiceBus/namespaces/tasktracker-sb/topics/task-events
   ```
3. The consuming Azure Function gets its own identity with **Azure Service Bus Data Receiver**
   on its subscription (identity-based trigger connection: `ServiceBusConnection__fullyQualifiedNamespace`).
4. Optionally disable local (SAS) auth on the namespace so keys cannot be used at all.

`DefaultAzureCredential` picks up the managed identity in Azure, and the developer's
`az login` / Visual Studio sign-in locally. The same code runs in both places.

### Retries and failure handling

| Layer | Behaviour |
|---|---|
| SDK | Exponential retry (above) for transient faults such as throttling, timeouts and connection drops. |
| Delivery guarantee | **Transactional outbox:** the API writes the task and an `OutboxMessages` row in the same EF Core transaction. A `BackgroundService` sends pending rows, then marks them sent. If Service Bus is down, rows wait and are retried; nothing is lost and the API stays fast. |
| Duplicates | The outbox gives at-least-once delivery. `MessageId = EventId` plus duplicate detection on the topic (e.g. a 10-minute window), and idempotent consumers, handle repeats. |
| Consumer failures | The Function retries per the subscription's `MaxDeliveryCount` (e.g. 5). Messages that keep failing go to the **dead-letter queue**, which is monitored and alerted on via Azure Monitor. |
| Observability | `CorrelationId`/trace ID flows from API to Function in Application Insights. Logs carry event and task IDs, never message bodies with user text, and never credentials. |

### Why the outbox was not built here

The assignment asks for the abstraction and a working local implementation. The outbox needs a
new table and migration plus a background worker, which is out of scope for this step. The
current call site is the single place that would change: it would write an outbox row instead of
calling the publisher directly.
