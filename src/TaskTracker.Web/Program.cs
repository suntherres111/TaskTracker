using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TaskTracker.Web;
using TaskTracker.Web.Auth;
using TaskTracker.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API location comes from wwwroot/appsettings.json (public, non-secret); override per environment.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    throw new InvalidOperationException("ApiBaseUrl is not configured in wwwroot/appsettings.json.");
}
var apiBaseAddress = new Uri(apiBaseUrl);

// Authentication state: JWT kept in memory by TokenAuthenticationStateProvider.
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<TokenAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<TokenAuthenticationStateProvider>());

// HttpClient for the API; BearerTokenHandler attaches the JWT to API requests.
builder.Services.AddScoped(sp => new HttpClient(
    new BearerTokenHandler(sp.GetRequiredService<TokenAuthenticationStateProvider>(), apiBaseAddress)
    {
        InnerHandler = new HttpClientHandler()
    })
{
    BaseAddress = apiBaseAddress
});

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ProjectsApiClient>();
builder.Services.AddScoped<TasksApiClient>();

await builder.Build().RunAsync();
