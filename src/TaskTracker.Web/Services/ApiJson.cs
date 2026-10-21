using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskTracker.Web.Services;

/// <summary>JSON settings matching the API: camelCase, enums as names.</summary>
public static class ApiJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
