using System.Net;
using System.Net.Http.Json;
using TaskTracker.Web.Models;

namespace TaskTracker.Web.Services;

/// <summary>
/// Typed access to the API's task endpoints. Uses the shared HttpClient,
/// so the JWT is attached automatically by BearerTokenHandler.
/// </summary>
public sealed class TasksApiClient
{
    private readonly HttpClient _http;

    public TasksApiClient(HttpClient http)
    {
        _http = http;
    }

    /// <returns>The project's tasks, or null if the project does not exist.</returns>
    /// <exception cref="HttpRequestException">The API is unreachable or returned an error status.</exception>
    public async Task<List<TaskSummary>?> GetTasksAsync(int projectId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"api/projects/{projectId}/tasks", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<TaskSummary>>(ApiJson.Options, cancellationToken) ?? new();
    }

    /// <param name="dueDate">An unambiguous instant (with offset), or null for no due date.</param>
    public Task<WriteResult<TaskSummary>> CreateTaskAsync(
        int projectId, string title, string? description, DateTimeOffset? dueDate, TaskItemStatus status,
        CancellationToken cancellationToken = default)
    {
        var body = new { title, description, dueDateUtc = dueDate, status };
        return SendAsync(() => _http.PostAsJsonAsync($"api/projects/{projectId}/tasks", body, ApiJson.Options, cancellationToken),
            "add the task", cancellationToken);
    }

    public Task<WriteResult<TaskSummary>> UpdateStatusAsync(int taskId, TaskItemStatus status, CancellationToken cancellationToken = default)
    {
        var body = new { status };
        return SendAsync(() => _http.PatchAsJsonAsync($"api/tasks/{taskId}/status", body, ApiJson.Options, cancellationToken),
            "update the status", cancellationToken);
    }

    /// <returns>Null on success (or if the task was already gone); otherwise a user-facing error message.</returns>
    public async Task<string?> DeleteTaskAsync(int taskId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _http.DeleteAsync($"api/tasks/{taskId}", cancellationToken);
            return response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound
                ? null
                : ApiErrors.Describe(response.StatusCode, "delete the task");
        }
        catch (HttpRequestException)
        {
            return ApiErrors.Describe(null, "delete the task");
        }
    }

    private static async Task<WriteResult<TaskSummary>> SendAsync(
        Func<Task<HttpResponseMessage>> send, string action, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await send();

            if (response.IsSuccessStatusCode)
            {
                var task = await response.Content.ReadFromJsonAsync<TaskSummary>(ApiJson.Options, cancellationToken);
                return task is null
                    ? WriteResult<TaskSummary>.Fail("The API returned an empty response.")
                    : WriteResult<TaskSummary>.Ok(task);
            }

            return response.StatusCode switch
            {
                HttpStatusCode.BadRequest => WriteResult<TaskSummary>.Fail(
                    "Please correct the highlighted fields.", await ReadValidationErrorsAsync(response, cancellationToken)),
                HttpStatusCode.NotFound => WriteResult<TaskSummary>.Fail(
                    "This project or task no longer exists. Go back to Projects and try again."),
                _ => WriteResult<TaskSummary>.Fail(ApiErrors.Describe(response.StatusCode, action))
            };
        }
        catch (HttpRequestException)
        {
            return WriteResult<TaskSummary>.Fail(ApiErrors.Describe(null, action));
        }
    }

    private static async Task<IReadOnlyDictionary<string, string[]>> ReadValidationErrorsAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>(ApiJson.Options, cancellationToken);
            return problem?.Errors ?? new Dictionary<string, string[]>();
        }
        catch (System.Text.Json.JsonException)
        {
            return new Dictionary<string, string[]>();
        }
    }

    private sealed record ValidationProblem(Dictionary<string, string[]>? Errors);
}
