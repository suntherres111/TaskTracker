namespace TaskTracker.Infrastructure.Auth;

public sealed record AccessToken(string Token, DateTime ExpiresAtUtc, string Role);
