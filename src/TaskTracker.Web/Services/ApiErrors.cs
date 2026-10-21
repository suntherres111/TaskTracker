using System.Net;

namespace TaskTracker.Web.Services;

/// <summary>User-facing messages for failed API calls.</summary>
public static class ApiErrors
{
    /// <param name="status">HTTP status, or null when the API could not be reached.</param>
    /// <param name="action">What was being attempted, e.g. "load tasks".</param>
    public static string Describe(HttpStatusCode? status, string action) => status switch
    {
        null => "Cannot reach the TaskTracker API. Check that it is running, then try again.",
        HttpStatusCode.Unauthorized => "Your session has expired. Please sign in again.",
        HttpStatusCode.Forbidden => "You are not allowed to do that.",
        _ => $"The API could not {action} ({(int)status}). Please try again."
    };
}
