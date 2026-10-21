using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace TaskTracker.Web.Auth;

/// <summary>
/// Holds the JWT in memory only (never localStorage/sessionStorage) and exposes the signed-in user to Blazor.
/// The token disappears when the tab is closed or refreshed, so the user signs in again.
/// Claims are read from the token for display only; the API validates the token on every request.
/// </summary>
public sealed class TokenAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    private string? _token;
    private DateTime _expiresAtUtc;
    private ClaimsPrincipal _user = Anonymous;
    private Timer? _expiryTimer;

    /// <summary>The current access token, or null when signed out or expired.</summary>
    public string? AccessToken => _token is not null && DateTime.UtcNow < _expiresAtUtc ? _token : null;

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_token is not null && DateTime.UtcNow >= _expiresAtUtc)
        {
            Clear();
        }

        return Task.FromResult(new AuthenticationState(_user));
    }

    public void SignIn(string token, DateTime expiresAtUtc)
    {
        var identity = new ClaimsIdentity(ReadClaims(token), authenticationType: "jwt", nameType: "name", roleType: "role");

        _token = token;
        _expiresAtUtc = expiresAtUtc.ToUniversalTime();
        _user = new ClaimsPrincipal(identity);

        // Sign out automatically when the token expires, so protected pages redirect to login.
        _expiryTimer?.Dispose();
        var dueTime = _expiresAtUtc - DateTime.UtcNow;
        _expiryTimer = new Timer(_ => SignOut(), null, dueTime > TimeSpan.Zero ? dueTime : TimeSpan.Zero, Timeout.InfiniteTimeSpan);

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void SignOut()
    {
        Clear();
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void Dispose() => _expiryTimer?.Dispose();

    private void Clear()
    {
        _expiryTimer?.Dispose();
        _expiryTimer = null;
        _token = null;
        _expiresAtUtc = default;
        _user = Anonymous;
    }

    private static List<Claim> ReadClaims(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3)
        {
            throw new FormatException("Access token is not a JWT.");
        }

        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

        var claims = new List<Claim>();
        using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Array)
            {
                claims.AddRange(property.Value.EnumerateArray().Select(item => new Claim(property.Name, item.ToString())));
            }
            else
            {
                claims.Add(new Claim(property.Name, property.Value.ToString()));
            }
        }

        return claims;
    }
}
