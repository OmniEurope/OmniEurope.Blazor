using System.Text;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Reads the ANSI escape sequences of a line of terminal output: <see cref="Parse"/> cuts it into runs
/// that carry one set of graphic attributes, <see cref="Strip"/> returns its text alone. The line itself
/// is never changed, so a host stores and exports it as written and only displays it through this class.
/// </summary>
/// <remarks>
/// <para>
/// Recognised: the Select Graphic Rendition <c>ESC [ ... m</c> (and the single-character introducer
/// U+009B) with reset, bold, faint, italic, underline, the sixteen colours and their defaults. A 256-colour
/// (<c>38;5;n</c>) or true-colour (<c>38;2;r;g;b</c>) request is read whole and reduced to the nearest of
/// the sixteen, so it keeps a hue the theme can guarantee to be readable. Every other control sequence
/// (cursor moves, erase, title, hyperlink...) is removed from the text; so is a bare escape. Any other SGR
/// code (blink, inverse, strike...) is read and ignored. Text outside the sequences is kept character for character.
/// </para>
/// <para>
/// A sequence is only a sequence when its escape character is there: <c>[90m</c> without it is ordinary
/// text and is kept (it cannot be told from a bracketed word of the log, and nothing may be lost from a line).
/// A host whose pipeline drops the escape character restores it before storing the line. A malformed
/// sequence loses its introducer and keeps the rest as text; a sequence cut off by the end of the line
/// is removed whole.
/// </para>
/// <para>
/// The scanner is hand-written and reads each character once: no regular expression, so no backtracking,
/// and a sequence longer than 256 characters (4096 for a string sequence) is not one. A line without any
/// escape character is returned as one run, without a copy.
/// </para>
/// </remarks>
public static class OmniAnsi
{
    /// <summary>Cuts a line into runs of one set of attributes, oldest first. Adjacent runs with the same attributes are merged; an empty run is never returned.</summary>
    /// <param name="text">The line as written; null or empty gives an empty list.</param>
    /// <returns>The runs; their texts, joined, are <see cref="Strip"/> of the line.</returns>
    public static IReadOnlyList<OmniAnsiSegment> Parse(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        if (!AnsiScanner.HasSequence(text))
        {
            return [new OmniAnsiSegment(text)];
        }

        var segments = new List<OmniAnsiSegment>();
        var buffer = new StringBuilder(text.Length);
        var style = AnsiStyle.Plain;
        var bufferStyle = style;
        var position = 0;
        while (position < text.Length)
        {
            var found = text.AsSpan(position).IndexOfAny(AnsiScanner.Escape, AnsiScanner.ControlSequenceIntroducer);
            var run = found < 0 ? text.Length - position : found;
            if (run > 0)
            {
                // A run under other attributes than the text gathered so far closes it; the same attributes
                // after a sequence that changed nothing (or changed back) continue it.
                if (style != bufferStyle)
                {
                    Flush(segments, buffer, bufferStyle);
                    bufferStyle = style;
                }

                buffer.Append(text, position, run);
            }

            if (found < 0)
            {
                break;
            }

            position += found;
            var length = AnsiScanner.Measure(text, position, out var kind, out var parametersStart, out var parametersEnd);
            if (kind == AnsiSequenceKind.Sgr)
            {
                style = AnsiScanner.Apply(style, text, parametersStart, parametersEnd);
            }

            position += length;
        }

        Flush(segments, buffer, bufferStyle);
        return segments;
    }

    /// <summary>The text of a line with every control sequence removed; the line itself when it holds none.</summary>
    /// <param name="text">The line as written; null gives an empty string.</param>
    /// <returns>The text a reader sees in <see cref="OmniAnsiMode.Hide"/> and <see cref="OmniAnsiMode.Render"/>, and the text a search runs on.</returns>
    public static string Strip(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        if (!AnsiScanner.HasSequence(text))
        {
            return text;
        }

        var buffer = new StringBuilder(text.Length);
        var position = 0;
        while (position < text.Length)
        {
            var found = text.AsSpan(position).IndexOfAny(AnsiScanner.Escape, AnsiScanner.ControlSequenceIntroducer);
            if (found < 0)
            {
                buffer.Append(text, position, text.Length - position);
                break;
            }

            buffer.Append(text, position, found);
            position += found + AnsiScanner.Measure(text, position + found, out _, out _, out _);
        }

        return buffer.ToString();
    }

    private static void Flush(List<OmniAnsiSegment> segments, StringBuilder buffer, AnsiStyle style)
    {
        if (buffer.Length == 0)
        {
            return;
        }

        segments.Add(new OmniAnsiSegment(
            buffer.ToString(),
            style.Bold,
            style.Foreground < 0 ? null : style.Foreground,
            style.Background < 0 ? null : style.Background,
            style.Dim,
            style.Italic,
            style.Underline));
        buffer.Clear();
    }
}
