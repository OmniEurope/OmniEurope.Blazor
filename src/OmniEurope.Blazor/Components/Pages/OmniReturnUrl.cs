using System.Diagnostics.CodeAnalysis;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The page to come back to after signing in, carried in a query parameter. Only a path of the
/// application itself is ever accepted, so the parameter cannot be turned into an open redirect.
/// </summary>
public static class OmniReturnUrl
{
    /// <summary>The query parameter the helpers use unless told otherwise.</summary>
    public const string DefaultParameterName = "returnUrl";

    private const int MaximumLength = 2048;

    /// <summary>
    /// Whether <paramref name="url"/> is a path of this application: it starts with a single
    /// <c>/</c>, names no scheme or host (<c>//evil</c>, <c>/\evil</c>, <c>https://evil</c>), and holds
    /// no backslash or control character a browser could normalize into one.
    /// </summary>
    public static bool IsLocal([NotNullWhen(true)] string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Length > MaximumLength)
        {
            return false;
        }

        if (url[0] != '/' || url.StartsWith("//", StringComparison.Ordinal))
        {
            return false;
        }

        if (url.Contains('\\', StringComparison.Ordinal) || url.Any(char.IsControl))
        {
            return false;
        }

        // Only the path is checked for "://": "/go?to=https://x" is a local page, while a path holding a
        // scheme separator ("/https://x") is refused, no page of an application needing one.
        var end = url.IndexOfAny(['?', '#']);
        var path = end < 0 ? url : url[..end];
        return !path.Contains("://", StringComparison.Ordinal);
    }

    /// <summary>Where to go once signed in: <paramref name="returnUrl"/> when it is local, else <paramref name="fallback"/>.</summary>
    public static string Resolve(string? returnUrl, string fallback = "/") => IsLocal(returnUrl) ? returnUrl : fallback;

    /// <summary>
    /// <paramref name="path"/> carrying <paramref name="returnUrl"/> in its query when it is local; the
    /// bare path when there is nothing safe to come back to.
    /// </summary>
    public static string Append(string path, string? returnUrl, string parameterName = DefaultParameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);
        if (!IsLocal(returnUrl))
        {
            return path;
        }

        var separator = path.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{path}{separator}{Uri.EscapeDataString(parameterName)}={Uri.EscapeDataString(returnUrl)}";
    }
}
