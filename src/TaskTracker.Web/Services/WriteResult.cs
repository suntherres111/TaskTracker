namespace TaskTracker.Web.Services;

/// <summary>
/// Outcome of a create/update call: the saved value, or an error message plus any
/// per-field validation errors returned by the API (400 ValidationProblemDetails).
/// </summary>
public sealed record WriteResult<T>(T? Value, string? ErrorMessage, IReadOnlyDictionary<string, string[]> FieldErrors)
    where T : class
{
    private static readonly IReadOnlyDictionary<string, string[]> NoFieldErrors = new Dictionary<string, string[]>();

    public bool Succeeded => Value is not null;

    public static WriteResult<T> Ok(T value) => new(value, null, NoFieldErrors);

    public static WriteResult<T> Fail(string message, IReadOnlyDictionary<string, string[]>? fieldErrors = null) =>
        new(null, message, fieldErrors ?? NoFieldErrors);
}
