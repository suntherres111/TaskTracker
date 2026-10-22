using System.Net;
using System.Net.Http.Json;

namespace TaskTracker.Api.Tests;

/// <summary>
/// HTTP-level authorization rules:
/// anonymous → 401 on writes; User → task create/update/status allowed, project changes and
/// deletes forbidden (403); Admin → everything. Reads stay anonymous.
/// </summary>
public sealed class AuthorizationTests : IClassFixture<ApiFactory>
{
    // Seeded data: project 1 with tasks 1–3, project 2 with tasks 4–5.
    private const int SeededProjectId = 1;
    private const int SeededTaskId = 1;

    private readonly ApiFactory _factory;

    public AuthorizationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    public static TheoryData<string, string> AllMutations => new()
    {
        { "POST", "api/projects" },
        { "PUT", $"api/projects/{SeededProjectId}" },
        { "DELETE", $"api/projects/{SeededProjectId}" },
        { "POST", $"api/projects/{SeededProjectId}/tasks" },
        { "PUT", $"api/tasks/{SeededTaskId}" },
        { "PATCH", $"api/tasks/{SeededTaskId}/status" },
        { "DELETE", $"api/tasks/{SeededTaskId}" }
    };

    public static TheoryData<string, string> AdminOnlyMutations => new()
    {
        { "POST", "api/projects" },
        { "PUT", $"api/projects/{SeededProjectId}" },
        { "DELETE", $"api/projects/{SeededProjectId}" },
        { "DELETE", $"api/tasks/{SeededTaskId}" }
    };

    [Theory]
    [MemberData(nameof(AllMutations))]
    public async Task Anonymous_Mutation_Returns401(string method, string url)
    {
        var client = await _factory.CreateClientAsync();

        using var response = await client.SendAsync(Request(method, url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_Reads_AreAllowed()
    {
        var client = await _factory.CreateClientAsync();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("api/projects")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"api/projects/{SeededProjectId}/tasks")).StatusCode);
    }

    [Theory]
    [MemberData(nameof(AdminOnlyMutations))]
    public async Task User_AdminOnlyOperation_Returns403(string method, string url)
    {
        var client = await _factory.CreateClientAsync(ApiFactory.UserUsername, ApiFactory.UserPassword);

        using var response = await client.SendAsync(Request(method, url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task User_CanCreateUpdateAndChangeStatusOfTasks()
    {
        var client = await _factory.CreateClientAsync(ApiFactory.UserUsername, ApiFactory.UserPassword);

        using var created = await client.PostAsJsonAsync($"api/projects/{SeededProjectId}/tasks", new { title = "Created by user" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var task = await created.Content.ReadFromJsonAsync<IdBody>();

        using var updated = await client.PutAsJsonAsync($"api/tasks/{task!.Id}", new { title = "Edited by user" });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        using var status = await client.PatchAsJsonAsync($"api/tasks/{task.Id}/status", new { status = "Done" });
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
    }

    [Fact]
    public async Task Admin_CanManageProjectsAndDeleteTasks()
    {
        var client = await _factory.CreateClientAsync(ApiFactory.AdminUsername, ApiFactory.AdminPassword);

        using var project = await client.PostAsJsonAsync("api/projects", new { name = "Admin project" });
        Assert.Equal(HttpStatusCode.Created, project.StatusCode);
        var projectId = (await project.Content.ReadFromJsonAsync<IdBody>())!.Id;

        using var renamed = await client.PutAsJsonAsync($"api/projects/{projectId}", new { name = "Admin project (renamed)" });
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);

        using var task = await client.PostAsJsonAsync($"api/projects/{projectId}/tasks", new { title = "Admin task" });
        Assert.Equal(HttpStatusCode.Created, task.StatusCode);
        var taskId = (await task.Content.ReadFromJsonAsync<IdBody>())!.Id;

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"api/tasks/{taskId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"api/projects/{projectId}")).StatusCode);
    }

    [Theory]
    [InlineData(ApiFactory.AdminUsername, ApiFactory.AdminPassword, "Admin")]
    [InlineData(ApiFactory.UserUsername, ApiFactory.UserPassword, "User")]
    public async Task Login_IssuesTokenWithRole(string username, string password, string expectedRole)
    {
        var client = await _factory.CreateClientAsync(username, password);

        var me = await client.GetFromJsonAsync<MeBody>("api/auth/me");

        Assert.Equal(username, me!.Username);
        Assert.Equal(expectedRole, me.Role);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = await _factory.CreateClientAsync();

        using var response = await client.PostAsJsonAsync("api/auth/login",
            new { username = ApiFactory.UserUsername, password = "not-the-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>A request with a valid body for each endpoint, so only authorization decides the outcome.</summary>
    private static HttpRequestMessage Request(string method, string url)
    {
        object body = url.EndsWith("/status") ? new { status = "Done" }
            : url.Contains("/tasks") ? new { title = "Authorization test" }
            : new { name = "Authorization test" };

        return new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method == "DELETE" ? null : JsonContent.Create(body)
        };
    }

    private sealed record IdBody(int Id);

    private sealed record MeBody(string Username, string Role);
}
