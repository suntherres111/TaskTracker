using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace TaskTracker.Infrastructure.Auth;

/// <summary>
/// Validates the demo user's credentials and issues signed JWT access tokens.
/// </summary>
public sealed class AuthService
{
    private readonly JwtOptions _jwt;
    private readonly DemoUserOptions _demoUser;
    private readonly ILogger<AuthService> _logger;
    private readonly PasswordHasher<DemoUserOptions> _passwordHasher = new();
    private readonly JsonWebTokenHandler _tokenHandler = new();

    public AuthService(JwtOptions jwt, DemoUserOptions demoUser, ILogger<AuthService> logger)
    {
        _jwt = jwt;
        _demoUser = demoUser;
        _logger = logger;
    }

    /// <returns>A token for valid credentials; null otherwise.</returns>
    public AccessToken? Login(string username, string password)
    {
        // Verify the password even when the username is wrong, so response time
        // doesn't reveal whether the username exists.
        var passwordOk = VerifyPassword(password);
        var usernameOk = string.Equals(username.Trim(), _demoUser.Username, StringComparison.OrdinalIgnoreCase);

        if (!passwordOk || !usernameOk)
        {
            _logger.LogWarning("Failed login attempt.");
            return null;
        }

        _logger.LogInformation("User {Username} logged in.", _demoUser.Username);
        return CreateToken(_demoUser.Username);
    }

    private bool VerifyPassword(string password)
    {
        try
        {
            var result = _passwordHasher.VerifyHashedPassword(_demoUser, _demoUser.PasswordHash, password);
            return result != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            // Configuration error (e.g. a plain-text password instead of a hash). Never log the value.
            _logger.LogError("DemoUser:PasswordHash is not a valid password hash. Regenerate it with --hash-password.");
            return false;
        }
    }

    private AccessToken CreateToken(string username)
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
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim("name", username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }),
            SigningCredentials = new SigningCredentials(CreateSigningKey(_jwt.SigningKey), SecurityAlgorithms.HmacSha256)
        };

        return new AccessToken(_tokenHandler.CreateToken(descriptor), expires);
    }

    internal static SymmetricSecurityKey CreateSigningKey(string signingKey) =>
        new(Encoding.UTF8.GetBytes(signingKey));
}
