using System.Net;
using System.Net.Http.Json;
using TaskTracker.Web.Models;

namespace TaskTracker.Web.Services;

/// <summary>
/// Typed access to the API's project endpoints. Uses the shared HttpClient,
/// so the JWT is attached automatically by BearerTokenHandler.
/// </summary>
public sealed class ProjectsApiClient
{
    private readonly HttpClient _http;

    public ProjectsApiClient(HttpClient http)
    {
        _http = http;
    }

    /// <exception cref="HttpRequestException">The API is unreachable or returned an error status.</exception>
    public async Task<IReadOnlyList<ProjectSummary>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        var projects = await _http.GetFromJsonAsync<List<ProjectSummary>>("api/projects", cancellationToken);
        return projects ?? new List<ProjectSummary>();
    }

    /// <returns>The project, or null if it does not exist.</returns>
    /// <exception cref="HttpRequestException">The API is unreachable or returned an error status.</exception>
    public async Task<ProjectSummary?> GetProjectAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"api/projects/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProjectSummary>(ApiJson.Options, cancellationToken);
    }
}
