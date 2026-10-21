namespace TaskTracker.Infrastructure.Auth;

/// <summary>
/// JWT settings, bound from the "Jwt" configuration section.
/// Issuer, Audience and ExpiryMinutes are non-secret (appsettings.json);
/// SigningKey is a secret (User Secrets locally, Key Vault / app settings in Azure).
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HMAC-SHA256 needs a key of at least 256 bits.</summary>
    public const int MinSigningKeyBytes = 32;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 60;
}
