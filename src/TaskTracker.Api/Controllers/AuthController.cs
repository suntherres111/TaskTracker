using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Api.Contracts.Auth;
using TaskTracker.Infrastructure.Auth;

namespace TaskTracker.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<LoginResponse> Login(LoginRequest request)
    {
        var token = _auth.Login(request.Username, request.Password);
        if (token is null)
        {
            // Same response for unknown user and wrong password.
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid credentials",
                detail: "The username or password is incorrect.");
        }

        return Ok(new LoginResponse(token.Token, "Bearer", token.ExpiresAtUtc));
    }

    /// <summary>Returns the user the bearer token belongs to. Lets clients confirm their token is still valid.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentUserResponse> Me()
    {
        return Ok(new CurrentUserResponse(User.Identity?.Name ?? string.Empty));
    }
}
