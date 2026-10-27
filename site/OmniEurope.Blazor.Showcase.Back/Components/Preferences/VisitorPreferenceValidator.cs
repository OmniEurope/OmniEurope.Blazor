using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OmniEurope.Blazor.Showcase.Back.Exceptions;
using OmniEurope.Blazor.Showcase.Shared.Components.Preferences;

namespace OmniEurope.Blazor.Showcase.Back.Components.Preferences;

/// <summary>The checks of the request itself, before anything reaches the database.</summary>
public static class VisitorPreferenceValidator
{
    /// <summary>The deepest JSON nesting a value may have; OE's own values nest two or three levels.</summary>
    public const int MaxJsonDepth = 32;

    private static readonly JsonDocumentOptions JsonOptions = new() { MaxDepth = MaxJsonDepth };

    /// <summary>
    /// The database key of a visitor: the SHA-256 of its id. The id never reaches the database or the
    /// logs; the hash cannot be turned back into an id that the API would accept.
    /// </summary>
    /// <exception cref="BadRequestException">The header is missing or is not a visitor id.</exception>
    public static string VisitorKey(string? visitorId)
    {
        if (!VisitorPreferenceApi.IsValidVisitorId(visitorId))
        {
            throw new BadRequestException($"The {VisitorPreferenceApi.VisitorHeader} header must hold a visitor id of {VisitorPreferenceApi.VisitorIdLength} base64url characters.");
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.ASCII.GetBytes(visitorId!)));
    }

    /// <exception cref="BadRequestException">The key is not one the API stores.</exception>
    public static string Key(string? key) =>
        VisitorPreferenceApi.IsValidKey(key)
            ? key!
            : throw new BadRequestException(
                $"A preference key has 1 to {VisitorPreferenceApi.MaxKeyLength} characters: ASCII letters, digits, '.', '-' and '_', starting with a letter or a digit.");

    /// <summary>Checks the body of a write and returns its date in UTC, moved back to <paramref name="now"/> when it is in the future.</summary>
    /// <exception cref="BadRequestException">The body is missing, undated, too long or not JSON.</exception>
    public static DateTimeOffset Request(SaveVisitorPreferenceRequest? request, PreferenceLimitsOptions limits, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(limits);
        if (request is null)
        {
            throw new BadRequestException("The body must hold the value and its date.");
        }

        if (request.UpdatedAt == default)
        {
            throw new BadRequestException("A preference needs the date it was written.");
        }

        if (request.Value is { } value)
        {
            if (value.Length > limits.MaxValueLength)
            {
                throw new BadRequestException($"A preference value holds at most {limits.MaxValueLength} characters.");
            }

            EnsureJson(value);
        }

        // A browser clock in the future would otherwise win over every later write of the visitor.
        var updatedAt = request.UpdatedAt.ToUniversalTime();
        return updatedAt > now ? now : updatedAt;
    }

    private static void EnsureJson(string value)
    {
        try
        {
            using var document = JsonDocument.Parse(value, JsonOptions);
        }
        catch (JsonException)
        {
            throw new BadRequestException($"A preference value must be JSON nested at most {MaxJsonDepth} levels deep.");
        }
    }
}
