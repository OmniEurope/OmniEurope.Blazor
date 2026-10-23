namespace OmniEurope.Blazor.Internal;

/// <summary>What a control sequence found by <see cref="AnsiScanner.Measure"/> is.</summary>
internal enum AnsiSequenceKind
{
    /// <summary>A control sequence with no graphic meaning here (cursor, erase, title, lone escape...): removed.</summary>
    Other,

    /// <summary>A Select Graphic Rendition, <c>CSI parameters m</c>: its parameters change the style.</summary>
    Sgr
}

/// <summary>
/// The graphic attributes in force while scanning. <see cref="Foreground"/> and <see cref="Background"/>
/// hold an index from 0 to 15, or -1 for the default colour.
/// </summary>
internal readonly record struct AnsiStyle(bool Bold, bool Dim, bool Italic, bool Underline, int Foreground, int Background)
{
    /// <summary>No attribute: the default colours, normal weight.</summary>
    internal static AnsiStyle Plain { get; } = new(false, false, false, false, -1, -1);
}

/// <summary>
/// A hand-written scanner for ANSI control sequences: it measures one sequence at a time and applies the
/// parameters of a Select Graphic Rendition. There is no regular expression, so no backtracking: every
/// call reads each character at most once, and a sequence is bounded (<see cref="MaxSequenceLength"/>
/// for a control sequence, <see cref="MaxStringLength"/> for a string sequence).
/// </summary>
/// <remarks>
/// A character that does not belong to a well-formed sequence is never swallowed with it: a sequence
/// that is malformed in the middle, or longer than its bound, loses its introducer only and the rest is
/// kept as text. Two exceptions, both of the kind a cut line produces: a control sequence that ends the
/// text before its final character is removed whole, and a bare escape is removed.
/// </remarks>
internal static class AnsiScanner
{
    internal const char Escape = '\u001b';
    internal const char ControlSequenceIntroducer = '\u009b';
    internal const int MaxSequenceLength = 256;
    internal const int MaxStringLength = 4096;

    private const char Bell = '\u0007';
    private const char StringTerminator = '\u009c';

    // The sixteen colours of xterm, the reference a 256-colour or true-colour request is reduced against.
    private static readonly (int R, int G, int B)[] Reference =
    [
        (0, 0, 0), (205, 0, 0), (0, 205, 0), (205, 205, 0), (0, 0, 238), (205, 0, 205), (0, 205, 205), (229, 229, 229),
        (127, 127, 127), (255, 0, 0), (0, 255, 0), (255, 255, 0), (92, 92, 255), (255, 0, 255), (0, 255, 255), (255, 255, 255),
    ];

    /// <summary>Whether the text holds a character that starts a control sequence.</summary>
    internal static bool HasSequence(string text) => text.AsSpan().IndexOfAny(Escape, ControlSequenceIntroducer) >= 0;

    /// <summary>
    /// Measures the control sequence that starts at <paramref name="start"/>, which must hold an escape or
    /// a single-character control sequence introducer. Returns the number of characters it spans, never
    /// less than one. For a <see cref="AnsiSequenceKind.Sgr"/> the parameters lie in
    /// [<paramref name="parametersStart"/>, <paramref name="parametersEnd"/>).
    /// </summary>
    internal static int Measure(string text, int start, out AnsiSequenceKind kind, out int parametersStart, out int parametersEnd)
    {
        kind = AnsiSequenceKind.Other;
        parametersStart = parametersEnd = start;
        if (text[start] == ControlSequenceIntroducer)
        {
            return MeasureControlSequence(text, start, start + 1, ref kind, out parametersStart, out parametersEnd);
        }

        if (start + 1 >= text.Length)
        {
            return 1;
        }

        var next = text[start + 1];
        return next switch
        {
            '[' => MeasureControlSequence(text, start, start + 2, ref kind, out parametersStart, out parametersEnd),
            ']' or 'P' or 'X' or '^' or '_' => MeasureString(text, start),
            _ => MeasureEscape(text, start),
        };
    }

    /// <summary>
    /// Applies the parameters of a Select Graphic Rendition to a style: reset (0), bold (1), faint (2),
    /// italic (3), underline (4, with <c>4:0</c> off), their resets (22, 23, 24), the sixteen foreground
    /// and background colours (30 to 37, 40 to 47, 90 to 97, 100 to 107) and their defaults (39, 49), and
    /// the extended colours (38 and 48, in the <c>5;n</c> and <c>2;r;g;b</c> forms, with semicolons or
    /// colons), which are read whole and reduced to the nearest of the sixteen. Any other code is ignored.
    /// </summary>
    internal static AnsiStyle Apply(AnsiStyle rendition, string text, int start, int end)
    {
        Span<int> values = stackalloc int[MaxSequenceLength];
        Span<bool> linked = stackalloc bool[MaxSequenceLength];
        var count = ReadParameters(text, start, end, values, linked);
        for (var index = 0; index < count; index++)
        {
            rendition = ApplyCode(rendition, values, linked, count, ref index);

            // The sub-parameters of a code that does not use them (4:3, 21:1...) are skipped with it.
            while (index + 1 < count && linked[index + 1])
            {
                index++;
            }
        }

        return rendition;
    }

    /// <summary>Applies the code at <paramref name="index"/>; an extended colour moves <paramref name="index"/> to its last value.</summary>
    private static AnsiStyle ApplyCode(AnsiStyle rendition, ReadOnlySpan<int> values, ReadOnlySpan<bool> linked, int count, ref int index)
    {
        var code = values[index];
        switch (code)
        {
            case 0:
                return AnsiStyle.Plain;
            case 4:
                return rendition with { Underline = !(index + 1 < count && linked[index + 1] && values[index + 1] == 0) };
            case 38 or 48 or 58:
                index = ReadExtendedColor(values, linked, count, index, out var color);
                return ApplyExtendedColor(rendition, code, color);
            default:
                return ApplyAttribute(rendition, code);
        }
    }

    private static AnsiStyle ApplyExtendedColor(AnsiStyle rendition, int code, int color)
    {
        if (color < 0)
        {
            return rendition;
        }

        return code switch
        {
            38 => rendition with { Foreground = color },
            48 => rendition with { Background = color },
            _ => rendition,
        };
    }

    /// <summary>The weight and decoration codes, then the colour ranges; any other code leaves the style as it is.</summary>
    private static AnsiStyle ApplyAttribute(AnsiStyle rendition, int code) => code switch
    {
        1 => rendition with { Bold = true },
        2 => rendition with { Dim = true },
        3 => rendition with { Italic = true },
        22 => rendition with { Bold = false, Dim = false },
        23 => rendition with { Italic = false },
        24 => rendition with { Underline = false },
        _ => ApplyColorCode(rendition, code),
    };

    private static AnsiStyle ApplyColorCode(AnsiStyle rendition, int code) => code switch
    {
        >= 30 and <= 37 => rendition with { Foreground = code - 30 },
        39 => rendition with { Foreground = -1 },
        >= 40 and <= 47 => rendition with { Background = code - 40 },
        49 => rendition with { Background = -1 },
        >= 90 and <= 97 => rendition with { Foreground = code - 90 + 8 },
        >= 100 and <= 107 => rendition with { Background = code - 100 + 8 },
        _ => rendition,
    };

    /// <summary>The nearest of the sixteen colours to an 8-bit colour of the 256-colour table.</summary>
    internal static int Nearest256(int number)
    {
        if (number < 16)
        {
            return Math.Max(0, number);
        }

        if (number >= 232)
        {
            var level = 8 + (10 * (Math.Min(number, 255) - 232));
            return Nearest16(level, level, level);
        }

        var cube = number - 16;
        return Nearest16(CubeLevel(cube / 36), CubeLevel(cube / 6 % 6), CubeLevel(cube % 6));
    }

    /// <summary>The nearest of the sixteen colours to a true colour, by squared distance in RGB; the lowest index wins a tie.</summary>
    internal static int Nearest16(int red, int green, int blue)
    {
        var best = 0;
        var bestDistance = int.MaxValue;
        for (var index = 0; index < Reference.Length; index++)
        {
            var (r, g, b) = Reference[index];
            var distance = ((red - r) * (red - r)) + ((green - g) * (green - g)) + ((blue - b) * (blue - b));
            if (distance < bestDistance)
            {
                best = index;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static int CubeLevel(int step) => step == 0 ? 0 : 55 + (40 * step);

    /// <summary>
    /// <c>CSI parameter-bytes intermediate-bytes final-byte</c>. The sequence is an SGR only when the final
    /// byte is <c>m</c> and the parameters are digits, semicolons and colons.
    /// </summary>
    private static int MeasureControlSequence(string text, int start, int bodyStart, ref AnsiSequenceKind kind, out int parametersStart, out int parametersEnd)
    {
        parametersStart = bodyStart;
        parametersEnd = bodyStart;
        var plain = true;
        var limit = Math.Min(text.Length, bodyStart + MaxSequenceLength);
        for (var index = bodyStart; index < limit; index++)
        {
            var current = text[index];
            if (current is >= '@' and <= '~')
            {
                if (current == 'm' && plain)
                {
                    kind = AnsiSequenceKind.Sgr;
                    parametersEnd = index;
                }

                return index + 1 - start;
            }

            if (current is < ' ' or > '?')
            {
                // Not part of a sequence: the introducer goes, what follows stays as text.
                return bodyStart - start;
            }

            plain &= current is (>= '0' and <= '9') or ';' or ':';
        }

        // A sequence cut off by the end of the text is removed whole; one longer than its bound is not a sequence.
        return limit == text.Length ? text.Length - start : bodyStart - start;
    }

    /// <summary>
    /// A string sequence (operating system command, device control, application command...) up to its
    /// terminator, a bell or <c>ESC \</c> or the 8-bit string terminator. Without a terminator within
    /// the bound only the two-character introducer is removed and the rest stays as text.
    /// </summary>
    private static int MeasureString(string text, int start)
    {
        var limit = Math.Min(text.Length, start + 2 + MaxStringLength);
        for (var index = start + 2; index < limit; index++)
        {
            var current = text[index];
            if (current is Bell or StringTerminator)
            {
                return index + 1 - start;
            }

            if (current == Escape && index + 1 < text.Length && text[index + 1] == '\\')
            {
                return index + 2 - start;
            }
        }

        return 2;
    }

    /// <summary>A two-character escape, or one with intermediate bytes (<c>ESC ( B</c>); a lone escape before anything else is removed alone.</summary>
    private static int MeasureEscape(string text, int start)
    {
        var index = start + 1;
        while (index < text.Length && text[index] is >= ' ' and <= '/')
        {
            index++;
        }

        return index < text.Length && text[index] is >= '0' and <= '~' ? index + 1 - start : 1;
    }

    /// <summary>Reads the numbers of an SGR into <paramref name="values"/>; an empty one is 0, and one after a colon is marked in <paramref name="linked"/>.</summary>
    private static int ReadParameters(string text, int start, int end, Span<int> values, Span<bool> linked)
    {
        var count = 0;
        var current = 0;
        var colon = false;
        for (var index = start; index <= end; index++)
        {
            var character = index < end ? text[index] : ';';
            if (character is >= '0' and <= '9')
            {
                current = Math.Min(current * 10 + (character - '0'), 65535);
                continue;
            }

            values[count] = current;
            linked[count] = colon;
            count++;
            current = 0;
            colon = character == ':';
        }

        return count;
    }

    /// <summary>
    /// Reads the colour that follows a 38, 48 or 58 at <paramref name="index"/>: <c>5;n</c> or
    /// <c>2;r;g;b</c> (with colons, <c>2:cs:r:g:b</c> or <c>2:r:g:b</c>). Returns the index of the last
    /// value read; <paramref name="color"/> is the nearest of the sixteen, or -1 when the colour is incomplete.
    /// </summary>
    private static int ReadExtendedColor(ReadOnlySpan<int> values, ReadOnlySpan<bool> linked, int count, int index, out int color)
    {
        color = -1;
        if (index + 1 >= count)
        {
            return index;
        }

        var kind = values[index + 1];
        if (kind == 5)
        {
            if (index + 2 >= count)
            {
                return count - 1;
            }

            color = Nearest256(values[index + 2]);
            return index + 2;
        }

        if (kind != 2)
        {
            return index + 1;
        }

        var first = index + 2;
        if (linked[index + 1])
        {
            // Colon form: a colour-space identifier may precede the three components.
            var run = 0;
            while (first + run < count && linked[first + run])
            {
                run++;
            }

            first += run >= 4 ? 1 : 0;
        }

        if (first + 2 >= count)
        {
            return count - 1;
        }

        color = Nearest16(Math.Min(values[first], 255), Math.Min(values[first + 1], 255), Math.Min(values[first + 2], 255));
        return first + 2;
    }
}
