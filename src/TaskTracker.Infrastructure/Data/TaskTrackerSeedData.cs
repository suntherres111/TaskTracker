using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Infrastructure.Data;

/// <summary>
/// Initial reference data applied through EF Core's HasData (migration-based seeding).
/// IDs and timestamps are fixed constants: if they were generated (e.g. DateTime.UtcNow),
/// every new migration would detect a "change" and emit UpdateData statements.
/// </summary>
internal static class TaskTrackerSeedData
{
    private static readonly DateTime SeededAtUtc = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);

    public static readonly Project[] Projects =
    {
        new()
        {
            Id = 1,
            Name = "Website Redesign",
            Description = "Refresh the public marketing website.",
            CreatedAtUtc = SeededAtUtc
        },
        new()
        {
            Id = 2,
            Name = "Mobile App Launch",
            Description = "Prepare the first public release of the mobile app.",
            CreatedAtUtc = SeededAtUtc
        }
    };

    public static readonly TaskItem[] Tasks =
    {
        new()
        {
            Id = 1,
            ProjectId = 1,
            Title = "Create wireframes",
            Description = "Low-fidelity wireframes for the home and pricing pages.",
            Status = TaskItemStatus.Done,
            DueDateUtc = new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAtUtc = SeededAtUtc
        },
        new()
        {
            Id = 2,
            ProjectId = 1,
            Title = "Build responsive layout",
            Description = "Implement the approved design with mobile breakpoints.",
            Status = TaskItemStatus.InProgress,
            DueDateUtc = new DateTime(2026, 10, 31, 0, 0, 0, DateTimeKind.Utc),
            CreatedAtUtc = SeededAtUtc
        },
        new()
        {
            Id = 3,
            ProjectId = 1,
            Title = "Run accessibility audit",
            Status = TaskItemStatus.Todo,
            CreatedAtUtc = SeededAtUtc
        },
        new()
        {
            Id = 4,
            ProjectId = 2,
            Title = "Set up app store listings",
            Description = "Screenshots, descriptions and privacy details for both stores.",
            Status = TaskItemStatus.Todo,
            DueDateUtc = new DateTime(2026, 11, 15, 0, 0, 0, DateTimeKind.Utc),
            CreatedAtUtc = SeededAtUtc
        },
        new()
        {
            Id = 5,
            ProjectId = 2,
            Title = "Beta test with internal users",
            Status = TaskItemStatus.InProgress,
            CreatedAtUtc = SeededAtUtc
        }
    };
}
