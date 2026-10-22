namespace TaskTracker.Web.Auth;

/// <summary>
/// Role names as issued by the API in the JWT "role" claim. Used only to show or hide UI;
/// the API enforces every permission itself.
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string User = "User";
}
