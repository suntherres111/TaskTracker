namespace TaskTracker.Infrastructure.Auth;

/// <summary>
/// The single demo account for this assessment, bound from the "DemoUser" configuration section
/// (User Secrets). Only a PBKDF2 hash of the password is stored, never the password itself.
/// </summary>
public sealed class DemoUserOptions
{
    public const string SectionName = "DemoUser";

    public string Username { get; set; } = string.Empty;

    /// <summary>Hash produced by ASP.NET Core Identity's PasswordHasher (PBKDF2).</summary>
    public string PasswordHash { get; set; } = string.Empty;
}
