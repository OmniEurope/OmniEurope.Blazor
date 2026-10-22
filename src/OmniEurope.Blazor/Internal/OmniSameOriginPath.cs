namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Checks that an address a component hands to the browser as the location of a script stays on the
/// origin of the page: a relative path (<c>assets/monaco/vs</c>, <c>../vs</c>) or a root-relative one
/// (<c>/_content/x/vs</c>), never anything the URL parser would read as another origin or as a
/// non-network scheme.
/// </summary>
/// <remarks>
/// A <c>..</c> segment is accepted: however far it climbs, the result stays on the origin of the page.
/// Refused: a blank value, any control character (the URL parser silently drops tabs and line breaks, so
/// <c>/&lt;tab&gt;/host</c> would become <c>//host</c>), a backslash (read as a slash by the parser), a
/// leading <c>//</c>, leading or trailing whitespace, and any scheme (<c>https:</c>, <c>data:</c>,
/// <c>blob:</c>, <c>javascript:</c>, <c>C:</c>): a colon in the first segment.
/// </remarks>
internal static class OmniSameOriginPath
{
    /// <summary>Whether <paramref name="path"/> is a path on the origin of the page.</summary>
    internal static bool IsValid(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || char.IsWhiteSpace(path[0]) || char.IsWhiteSpace(path[^1]))
        {
            return false;
        }

        if (path.Any(character => char.IsControl(character) || character == '\\'))
        {
            return false;
        }

        if (path.StartsWith("//", StringComparison.Ordinal))
        {
            return false;
        }

        // The first segment ends at the first slash, query or fragment: a colon before it is a scheme.
        var end = path.AsSpan().IndexOfAny('/', '?', '#');
        var firstSegment = end < 0 ? path.AsSpan() : path.AsSpan(0, end);
        return !firstSegment.Contains(':');
    }

    /// <summary>Throws when <paramref name="path"/> is not a path on the origin of the page.</summary>
    /// <exception cref="ArgumentException">The path leaves the origin of the page.</exception>
    internal static void Require(string? path, string parameterName)
    {
        if (!IsValid(path))
        {
            throw new ArgumentException($"{parameterName} must be a path on the origin of the page, relative or starting with a single '/': the package never loads a script from another origin.", parameterName);
        }
    }
}
