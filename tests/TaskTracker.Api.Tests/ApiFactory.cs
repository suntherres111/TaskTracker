using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TaskTracker.Infrastructure.Auth;
using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Api.Tests;

/// <summary>
/// Hosts the real API in memory for HTTP-level tests: real routing, JWT validation and
/// authorization policies, with SQLite in-memory instead of SQL Server.
/// Uses throwaway test accounts and a random signing key; no User Secrets, Azure SQL or
/// real credentials are involved ("Testing" environment, so User Secrets are not loaded).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string AdminUsername = "admin";   // same as appsettings.json, so config precedence cannot matter
    public const string AdminPassword = "Admin-Test-Password-1";
    public const string UserUsername = "user";
    public const string UserPassword = "User-Test-Password-1";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public ApiFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var hasher = new PasswordHasher<DemoUserOptions>();

        builder.UseEnvironment("Testing");

        // Program.cs requires these at startup; values are test-only.
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Data Source=replaced-by-sqlite-in-tests");
        builder.UseSetting("Jwt:SigningKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));
        builder.UseSetting("DemoUsers:Admin:Username", AdminUsername);
        builder.UseSetting("DemoUsers:Admin:PasswordHash", hasher.HashPassword(new DemoUserOptions(), AdminPassword));
        builder.UseSetting("DemoUsers:User:Username", UserUsername);
        builder.UseSetting("DemoUsers:User:PasswordHash", hasher.HashPassword(new DemoUserOptions(), UserPassword));

        builder.ConfigureTestServices(services =>
        {
            // Swap the SQL Server registration for SQLite in-memory on a shared open connection.
            services.RemoveAll<DbContextOptions<TaskTrackerDbContext>>();
            services.AddDbContext<TaskTrackerDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<TaskTrackerDbContext>().Database.EnsureCreated();

        return host;
    }

    /// <summary>An HTTPS client (avoids the HTTPS-redirect middleware), optionally signed in.</summary>
    public async Task<HttpClient> CreateClientAsync(string? username = null, string? password = null)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        if (username is null || password is null)
        {
            return client;
        }

        using var response = await client.PostAsJsonAsync("api/auth/login", new { username, password });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginBody>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }

    private sealed record LoginBody(string AccessToken, string Role);
}
