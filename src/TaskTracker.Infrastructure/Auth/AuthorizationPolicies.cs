namespace TaskTracker.Infrastructure.Auth;

/// <summary>
/// Named authorization policies used on API actions. Policies (rather than role strings on each
/// action) keep the permission rules in one place: <see cref="AuthenticationSetup"/>.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>Admin only: create/update/delete projects, delete tasks.</summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>Admin or User: create tasks, update task details and status.</summary>
    public const string TaskEditor = "TaskEditor";
}
