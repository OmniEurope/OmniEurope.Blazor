using System.Text.Json;

namespace OmniEurope.Blazor.Internal;

/// <summary>The JSON of a preference value: web defaults, and a value that no longer reads as <c>T</c> reads as <c>default</c>.</summary>
internal static class PreferenceJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    internal static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    internal static T? Deserialize<T>(string? json)
    {
        if (json is null)
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
