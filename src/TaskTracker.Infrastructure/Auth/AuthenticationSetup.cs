using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace TaskTracker.Infrastructure.Auth;

public static class AuthenticationSetup
{
    /// <summary>
    /// Registers JWT Bearer authentication, authorization and <see cref="AuthService"/>.
    /// Fails fast at startup with a clear message if required settings are missing.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var demoUser = configuration.GetSection(DemoUserOptions.SectionName).Get<DemoUserOptions>() ?? new DemoUserOptions();
        Validate(jwt, demoUser);

        services.AddSingleton(jwt);
        services.AddSingleton(demoUser);
        services.AddSingleton<AuthService>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Keep standard JWT claim names ("sub", "name") instead of mapping them to long URIs.
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
                    NameClaimType = "name"
                };
            });

        services.AddAuthorization();
        return services;
    }

    private static void Validate(JwtOptions jwt, DemoUserOptions demoUser)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(jwt.Issuer)) errors.Add("Jwt:Issuer is required.");
        if (string.IsNullOrWhiteSpace(jwt.Audience)) errors.Add("Jwt:Audience is required.");
        if (jwt.ExpiryMinutes is < 1 or > 1440) errors.Add("Jwt:ExpiryMinutes must be between 1 and 1440.");
        if (Encoding.UTF8.GetByteCount(jwt.SigningKey ?? string.Empty) < JwtOptions.MinSigningKeyBytes)
            errors.Add($"Jwt:SigningKey must be set and at least {JwtOptions.MinSigningKeyBytes} bytes long.");
        if (string.IsNullOrWhiteSpace(demoUser.Username)) errors.Add("DemoUser:Username is required.");
        if (string.IsNullOrWhiteSpace(demoUser.PasswordHash)) errors.Add("DemoUser:PasswordHash is required.");

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Authentication is not configured: " + string.Join(" ", errors) +
                " For local development, set the missing values with 'dotnet user-secrets set' on src/TaskTracker.Api (see README).");
        }
    }
}
