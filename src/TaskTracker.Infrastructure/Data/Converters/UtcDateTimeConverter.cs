using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TaskTracker.Infrastructure.Data.Converters;

/// <summary>
/// SQL Server datetime2 has no notion of DateTimeKind, so EF Core materialises values as
/// <see cref="DateTimeKind.Unspecified"/> and they serialise without a "Z". This application
/// stores all timestamps in UTC, so values read from the database are tagged as UTC.
/// <para>
/// Writes are stored unchanged; no conversion happens here. Converting client input to UTC is the
/// API layer's job. A <see cref="DateTimeKind.Local"/> value reaching the database indicates a bug,
/// so it is rejected rather than silently stored as the server's local clock time.
/// </para>
/// </summary>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            value => EnsureNotLocal(value),
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }

    private static DateTime EnsureNotLocal(DateTime value) =>
        value.Kind == DateTimeKind.Local
            ? throw new InvalidOperationException(
                "Local DateTime values cannot be stored. Convert to UTC before saving.")
            : value;
}
