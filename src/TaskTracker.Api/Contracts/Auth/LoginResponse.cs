namespace TaskTracker.Api.Contracts.Auth;

public record LoginResponse(string AccessToken, string TokenType, DateTime ExpiresAtUtc);
