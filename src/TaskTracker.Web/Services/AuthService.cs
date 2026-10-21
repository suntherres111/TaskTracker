using System.Net;
using System.Net.Http.Json;
using TaskTracker.Web.Auth;

namespace TaskTracker.Web.Services;

public sealed record LoginResult(bool Succeeded, string? ErrorMessage);

/// <summary>
/// Calls the API's auth endpoints and updates the in-memory session.
/// </summary>
public sealed class AuthService
{
    private readonly HttpClient _http;
    private readonly TokenAuthenticationStateProvider _authState;

    public AuthService(HttpClient http, TokenAuthenticationStateProvider authState)
    {
        _http = http;
        _authState = authState;
    }

    public async Task<LoginResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync("api/auth/login", new { username, password }, cancellationToken);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)
            {
                return new LoginResult(false, "Invalid username or password.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return new LoginResult(false, $"Sign-in failed ({(int)response.StatusCode}). Please try again.");
            }

            var body = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);
            if (string.IsNullOrEmpty(body?.AccessToken))
            {
                return new LoginResult(false, "Sign-in failed: the server returned no token.");
            }

            _authState.SignIn(body.AccessToken, body.ExpiresAtUtc);
            return new LoginResult(true, null);
        }
        catch (HttpRequestException)
        {
            return new LoginResult(false, "Cannot reach the TaskTracker API. Check that it is running.");
        }
        catch (FormatException)
        {
            return new LoginResult(false, "Sign-in failed: the server returned an invalid token.");
        }
    }

    public void Logout() => _authState.SignOut();

    /// <summary>Asks the API who the current token belongs to (a protected endpoint).</summary>
    /// <returns>The username, or null if the API did not accept the token.</returns>
    public async Task<string?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync("api/auth/me", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var body = await response.Content.ReadFromJsonAsync<CurrentUserResponse>(cancellationToken);
        return body?.Username;
    }

    private sealed record LoginResponse(string AccessToken, string TokenType, DateTime ExpiresAtUtc);

    private sealed record CurrentUserResponse(string Username);
}
