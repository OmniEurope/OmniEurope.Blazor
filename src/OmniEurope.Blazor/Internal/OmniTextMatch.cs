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
    private const CompareOptions CaseSensitiveOptions = CompareOptions.IgnoreNonSpace;

    /// <summary>
    /// Whether <paramref name="text"/> contains <paramref name="query"/> under the same rule as
    /// <see cref="Split"/>, so a list narrowed with it keeps exactly the entries it can mark. An empty
    /// query matches everything.
    /// </summary>
    internal static bool Contains(string text, string? query)
    {
        var needle = query?.Trim();
        return string.IsNullOrEmpty(needle)
            || CultureInfo.CurrentCulture.CompareInfo.IndexOf(text ?? string.Empty, needle, Options) >= 0;
    }

    internal static IReadOnlyList<OmniTextSegment> Split(string text, string? query, bool ignoreCase = true)
    {
        var needle = query?.Trim();
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(needle))
        {
            return [new OmniTextSegment(text ?? string.Empty, false)];
        }

        var compare = CultureInfo.CurrentCulture.CompareInfo;
        var segments = new List<OmniTextSegment>();
        var start = 0;
        var scan = 0;
        while (scan < text.Length)
        {
            var index = compare.IndexOf(text.AsSpan(scan), needle, ignoreCase ? Options : CaseSensitiveOptions, out var length);
            if (index < 0 || length == 0)
            {
                break;
            }

            var at = scan + index;
            if (!ignoreCase && !SameCase(text.Substring(at, length), needle))
            {
                scan = at + 1;
                continue;
            }

            if (at > start)
            {
                segments.Add(new OmniTextSegment(text[start..at], false));
            }

            segments.Add(new OmniTextSegment(text.Substring(at, length), true));
            start = scan = at + length;
        }

        if (start < text.Length)
        {
            segments.Add(new OmniTextSegment(text[start..], false));
        }

        return segments;
    }

    // The comparer ignores accents but, under ICU, case with them: a match that must respect case is kept only
    // when the two runs are equal once their accents are removed.
    private static bool SameCase(string run, string needle)
        => string.Equals(StripAccents(run), StripAccents(needle), StringComparison.Ordinal);

    private static string StripAccents(string value)
        => string.Concat(value.Normalize(System.Text.NormalizationForm.FormD).Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark));
}
