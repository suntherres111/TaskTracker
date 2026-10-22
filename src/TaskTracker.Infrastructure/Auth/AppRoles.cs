namespace TaskTracker.Infrastructure.Auth;

/// <summary>Application roles, carried in the JWT "role" claim.</summary>
public static class AppRoles
{
    /// <summary>All project and task operations.</summary>
    public const string Admin = "Admin";

    /// <summary>Read everything; create and update tasks (including status). No deletes, no project changes.</summary>
    public const string User = "User";

    public static readonly IReadOnlyList<string> All = new[] { Admin, User };

    /// <summary>Returns the canonical role name for <paramref name="value"/> (case-insensitive), or null if unknown.</summary>
    public static string? Normalize(string? value) =>
        All.FirstOrDefault(role => string.Equals(role, value?.Trim(), StringComparison.OrdinalIgnoreCase));
}
