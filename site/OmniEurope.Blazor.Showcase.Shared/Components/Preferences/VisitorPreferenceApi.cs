using System.Buffers;
using System.Buffers.Text;
using System.Security.Cryptography;

namespace OmniEurope.Blazor.Showcase.Shared.Components.Preferences;

/// <summary>
/// The contract of the anonymous preference API: its route, the header that names the visitor, and
/// the formats both sides validate. The visitor id is the only thing that gives access to a visitor's
/// preferences, so it is a 256-bit random value that travels in a header, never in an address.
/// </summary>
public static class VisitorPreferenceApi
{
    /// <summary>The route of the preferences, relative to the API base address.</summary>
    public const string Route = "api/preferences";

    /// <summary>
    /// The header that carries the visitor id. A custom header also forces a CORS preflight, so a page
    /// of another origin cannot send a request the API would act on.
    /// </summary>
    public const string VisitorHeader = "X-Showcase-Visitor";

    /// <summary>The length of a visitor id: 32 random bytes in unpadded base64url.</summary>
    public const int VisitorIdLength = 43;

    /// <summary>The longest preference key accepted.</summary>
    public const int MaxKeyLength = 200;

    private const int VisitorIdBytes = 32;

    private static readonly SearchValues<char> Base64UrlAlphabet =
        SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_");

    private static readonly SearchValues<char> KeyAlphabet =
        SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_.");

    /// <summary>A new visitor id from the cryptographic random generator.</summary>
    /// <returns>43 characters of unpadded base64url.</returns>
    public static string CreateVisitorId() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(VisitorIdBytes));

    /// <summary>Whether <paramref name="visitorId"/> has the exact shape of an id made by <see cref="CreateVisitorId"/>.</summary>
    /// <param name="visitorId">The value to check; null is invalid.</param>
    /// <returns>True for exactly 43 base64url characters.</returns>
    public static bool IsValidVisitorId(string? visitorId) =>
        visitorId is { Length: VisitorIdLength } && !visitorId.AsSpan().ContainsAnyExcept(Base64UrlAlphabet);

    /// <summary>
    /// Whether <paramref name="key"/> is a key the API stores: 1 to <see cref="MaxKeyLength"/> characters,
    /// ASCII letters, digits, dot, dash and underscore, starting with a letter or a digit (the package keys
    /// are <c>omni.appearance</c> and <c>omni.grid.&lt;state key&gt;</c>).
    /// </summary>
    /// <param name="key">The key to check; null is invalid.</param>
    /// <returns>True when the key is accepted.</returns>
    public static bool IsValidKey(string? key) =>
        key is { Length: > 0 and <= MaxKeyLength }
        && char.IsAsciiLetterOrDigit(key[0])
        && !key.AsSpan().ContainsAnyExcept(KeyAlphabet);
}
