using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace TaskTracker.Infrastructure.Auth;

/// <summary>
/// Validates demo-account credentials and issues signed JWT access tokens carrying the user's role.
/// </summary>
public sealed class AuthService
{
    private readonly JwtOptions _jwt;
    private readonly IReadOnlyList<DemoUserOptions> _users;
    private readonly ILogger<AuthService> _logger;
    private readonly PasswordHasher<DemoUserOptions> _passwordHasher = new();
    private readonly JsonWebTokenHandler _tokenHandler = new();

    // Verified against when the username is unknown, so response time doesn't reveal which usernames exist.
    private readonly string _dummyHash;

    public AuthService(JwtOptions jwt, IReadOnlyList<DemoUserOptions> users, ILogger<AuthService> logger)
    {
        _jwt = jwt;
        _users = users;
        _logger = logger;
        _dummyHash = _passwordHasher.HashPassword(new DemoUserOptions(), Guid.NewGuid().ToString());
    }

    /// <returns>A token for valid credentials; null otherwise.</returns>
    public AccessToken? Login(string username, string password)
    {
        var user = _users.FirstOrDefault(u => string.Equals(u.Username, username.Trim(), StringComparison.OrdinalIgnoreCase));

        // Always perform exactly one hash verification, whether or not the user exists.
        var passwordOk = VerifyPassword(user, user?.PasswordHash ?? _dummyHash, password);

        if (user is null || !passwordOk)
        {
            _logger.LogWarning("Failed login attempt.");
            return null;
        }

        _logger.LogInformation("User {Username} logged in with role {Role}.", user.Username, user.Role);
        return CreateToken(user);
    }

    private bool VerifyPassword(DemoUserOptions? user, string hash, string password)
    {
        try
        {
            var result = _passwordHasher.VerifyHashedPassword(user ?? new DemoUserOptions(), hash, password);
            return result != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            // Configuration error (e.g. a plain-text password instead of a hash). Never log the value.
            _logger.LogError("DemoUsers:{Role}:PasswordHash is not a valid password hash. Regenerate it with --hash-password.", user?.Role);
            return false;
        }
    }

    private AccessToken CreateToken(DemoUserOptions user)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_jwt.ExpiryMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim("name", user.Username),
                new Claim("role", user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }),
            SigningCredentials = new SigningCredentials(CreateSigningKey(_jwt.SigningKey), SecurityAlgorithms.HmacSha256)
        };

        return new AccessToken(_tokenHandler.CreateToken(descriptor), expires, user.Role);
    }

    internal static SymmetricSecurityKey CreateSigningKey(string signingKey) =>
        new(Encoding.UTF8.GetBytes(signingKey));
}
