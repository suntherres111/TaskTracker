namespace TaskTracker.Infrastructure.Auth;

/// <summary>
/// One demo account per role, bound from the "DemoUsers" configuration section, keyed by role:
/// <code>
/// DemoUsers:Admin:Username      (appsettings.json, not secret)
/// DemoUsers:Admin:PasswordHash  (User Secrets)
/// DemoUsers:User:Username       (appsettings.json, not secret)
/// DemoUsers:User:PasswordHash   (User Secrets)
/// </code>
/// Only a PBKDF2 hash of each password is stored, never the password itself.
/// </summary>
public sealed class DemoUserOptions
{
    public const string SectionName = "DemoUsers";

    public string Username { get; set; } = string.Empty;

    /// <summary>Hash produced by ASP.NET Core Identity's PasswordHasher (PBKDF2).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Set from the configuration key (e.g. "Admin"), not read from configuration values.</summary>
    public string Role { get; set; } = string.Empty;
}
