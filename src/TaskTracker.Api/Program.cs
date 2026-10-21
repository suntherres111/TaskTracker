using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TaskTracker.Api.Services;
using TaskTracker.Api.Tools;
using TaskTracker.Domain.Notifications;
using TaskTracker.Infrastructure.Auth;
using TaskTracker.Infrastructure.Data;
using TaskTracker.Infrastructure.Notifications;

// Developer helper: generates the DemoUser password hash, then exits (see Tools/HashPasswordCommand.cs).
if (args.Contains(HashPasswordCommand.Argument))
{
    HashPasswordCommand.Run();
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        // Enums travel as names ("InProgress"); unknown names and raw numbers are rejected with 400.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Adds an "Authorize" button: paste the accessToken from POST /api/auth/login.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT access token from POST /api/auth/login (without the \"Bearer \" prefix)."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = Array.Empty<string>()
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is not configured. " +
        "For local development, run: dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"<connection string>\" --project src/TaskTracker.Api. " +
        "In other environments, set the ConnectionStrings__DefaultConnection environment variable or app setting.");
}

// EnableRetryOnFailure retries transient Azure SQL errors (e.g. 40613 while a serverless database resumes).
builder.Services.AddDbContext<TaskTrackerDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<TaskService>();

// External integration: local logging publisher. Swap for a Service Bus implementation in production
// (docs/integration-design.md); nothing else changes.
if (builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Notifications:SimulateFailure"))
{
    // Dev-only: demonstrates that a notification outage does not fail task creation.
    builder.Services.AddSingleton<INotificationPublisher, FailingNotificationPublisher>();
}
else
{
    builder.Services.AddSingleton<INotificationPublisher, LoggingNotificationPublisher>();
}

// JWT Bearer authentication + demo user (settings: "Jwt" in appsettings.json, secrets in User Secrets).
builder.Services.AddJwtAuthentication(builder.Configuration);

// CORS: the Blazor WebAssembly app runs on a different origin and calls the API from the browser.
// Allowed origins come from configuration ("Cors:AllowedOrigins"); the JWT travels in the
// Authorization header, so no cookies/credentials are allowed cross-origin.
const string WebClientCorsPolicy = "WebClient";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
    options.AddPolicy(WebClientCorsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .WithHeaders("Authorization", "Content-Type")
        .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(WebClientCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
