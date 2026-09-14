namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Cuts a text around every occurrence of a searched text, ignoring case and accents in the current
/// culture, so that "liege" finds the accented name. The comparison can match a run whose length differs
/// from the needle's (a precomposed letter against a decomposed one), which is why the length comes
/// from the comparer rather than from the needle.
/// </summary>
internal static class OmniTextMatch
{
    private const CompareOptions Options = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    internal static IReadOnlyList<OmniTextSegment> Split(string text, string? query)
    {
        var needle = query?.Trim();
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(needle))
        {
            return [new OmniTextSegment(text ?? string.Empty, false)];
        }

        var compare = CultureInfo.CurrentCulture.CompareInfo;
        var segments = new List<OmniTextSegment>();
        var start = 0;
        while (start < text.Length)
        {
            var index = compare.IndexOf(text.AsSpan(start), needle, Options, out var length);
            if (index < 0 || length == 0)
            {
                break;
            }

            if (index > 0)
            {
                segments.Add(new OmniTextSegment(text.Substring(start, index), false));
            }

            segments.Add(new OmniTextSegment(text.Substring(start + index, length), true));
            start += index + length;
        }

        if (start < text.Length)
        {
            segments.Add(new OmniTextSegment(text[start..], false));
        }

        return segments;
    }
}
