using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace TaskTracker.Infrastructure.Auth;

public static class AuthenticationSetup
{
    /// <summary>
    /// Registers JWT Bearer authentication, role-based authorization policies and <see cref="AuthService"/>.
    /// Fails fast at startup with a clear message if required settings are missing.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var demoUsers = ReadDemoUsers(configuration);
        Validate(jwt, demoUsers);

        services.AddSingleton(jwt);
        services.AddSingleton<IReadOnlyList<DemoUserOptions>>(demoUsers);
        services.AddSingleton<AuthService>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Keep standard JWT claim names ("sub", "name", "role") instead of mapping them to long URIs.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = AuthService.CreateSigningKey(jwt.SigningKey),
                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };
            });

        // Authentication failures stay 401; an authenticated user without the required role gets 403.
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.AdminOnly, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(AppRoles.Admin));

            options.AddPolicy(AuthorizationPolicies.TaskEditor, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(AppRoles.Admin, AppRoles.User));
        });

        return services;
    }

    /// <summary>Reads "DemoUsers:{Role}:Username/PasswordHash" entries; the key becomes the role.</summary>
    private static List<DemoUserOptions> ReadDemoUsers(IConfiguration configuration) =>
        configuration.GetSection(DemoUserOptions.SectionName).GetChildren()
            .Select(section =>
            {
                var user = section.Get<DemoUserOptions>() ?? new DemoUserOptions();
                user.Role = AppRoles.Normalize(section.Key) ?? section.Key;
                return user;
            })
            .ToList();

    private static void Validate(JwtOptions jwt, IReadOnlyList<DemoUserOptions> demoUsers)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(jwt.Issuer)) errors.Add("Jwt:Issuer is required.");
        if (string.IsNullOrWhiteSpace(jwt.Audience)) errors.Add("Jwt:Audience is required.");
        if (jwt.ExpiryMinutes is < 1 or > 1440) errors.Add("Jwt:ExpiryMinutes must be between 1 and 1440.");
        if (Encoding.UTF8.GetByteCount(jwt.SigningKey ?? string.Empty) < JwtOptions.MinSigningKeyBytes)
            errors.Add($"Jwt:SigningKey must be set and at least {JwtOptions.MinSigningKeyBytes} bytes long.");
        foreach (var user in demoUsers.Where(u => AppRoles.Normalize(u.Role) is null))
            errors.Add($"DemoUsers:{user.Role} is not a known role (expected: {string.Join(", ", AppRoles.All)}).");

        foreach (var role in AppRoles.All)
        {
            var user = demoUsers.FirstOrDefault(u => u.Role == role);
            if (string.IsNullOrWhiteSpace(user?.Username)) errors.Add($"DemoUsers:{role}:Username is required.");
            if (string.IsNullOrWhiteSpace(user?.PasswordHash)) errors.Add($"DemoUsers:{role}:PasswordHash is required.");
        }

        var duplicates = demoUsers
            .Where(u => !string.IsNullOrWhiteSpace(u.Username))
            .GroupBy(u => u.Username.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
        foreach (var name in duplicates)
            errors.Add($"Demo username '{name}' is used by more than one role.");

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Authentication is not configured: " + string.Join(" ", errors) +
                " For local development, set the missing values with 'dotnet user-secrets set' on src/TaskTracker.Api (see README).");
        }
    }
}
