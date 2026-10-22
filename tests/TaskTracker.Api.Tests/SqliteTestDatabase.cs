using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Api.Tests;

/// <summary>
/// A private SQLite in-memory database per test. The schema and seed data
/// (projects 1–2, tasks 1–5) are created from the real EF Core model; the database
/// lives as long as the open connection and is discarded on Dispose.
/// Never touches SQL Server or Azure SQL.
/// </summary>
internal sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    /// <summary>A new context on the shared connection (no tracked entities carried over).</summary>
    public TaskTrackerDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TaskTrackerDbContext>()
            .UseSqlite(_connection)
            .Options);

    public void Dispose() => _connection.Dispose();
}
