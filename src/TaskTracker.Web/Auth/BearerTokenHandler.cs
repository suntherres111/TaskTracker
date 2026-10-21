using System.Net;
using System.Net.Http.Headers;

namespace TaskTracker.Web.Auth;

/// <summary>
/// Adds "Authorization: Bearer {token}" to requests sent to the TaskTracker API (and nowhere else).
/// If the API rejects the token with 401, the local session is cleared so the UI returns to login.
/// </summary>
public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly TokenAuthenticationStateProvider _authState;
    private readonly Uri _apiBaseAddress;

    public BearerTokenHandler(TokenAuthenticationStateProvider authState, Uri apiBaseAddress)
    {
        _authState = authState;
        _apiBaseAddress = apiBaseAddress;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _authState.AccessToken;
        var isApiRequest = request.RequestUri is not null && _apiBaseAddress.IsBaseOf(request.RequestUri);

        if (token is not null && isApiRequest && request.Headers.Authorization is null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized && request.Headers.Authorization is not null)
        {
            // Token expired or was rejected by the server.
            _authState.SignOut();
        }

        return response;
    }
}
