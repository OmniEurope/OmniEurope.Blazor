using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>A run of a displayed line: its text, the attributes it is drawn with, and whether it matches the searched text.</summary>
internal readonly record struct AnsiPiece(string Text, OmniAnsiSegment? Style, bool Matched);

/// <summary>
/// Turns a line that may hold ANSI sequences into what is displayed for an <see cref="OmniAnsiMode"/>,
/// shared by <see cref="OmniAnsiText"/> and <see cref="OmniLogViewer"/> so both draw a line the same way.
/// A search runs on the displayed text (the text without its sequences in <see cref="OmniAnsiMode.Render"/>
/// and <see cref="OmniAnsiMode.Hide"/>, the line as written in <see cref="OmniAnsiMode.Raw"/>), and a match
/// that crosses two runs of different attributes is marked in both.
/// </summary>
internal static class OmniAnsiMarkup
{
    private static readonly string[] ForegroundClasses = [.. Enumerable.Range(0, 16).Select(index => $"omni-ansi-fg-{index}")];
    private static readonly string[] BackgroundClasses = [.. Enumerable.Range(0, 16).Select(index => $"omni-ansi-bg-{index}")];

    /// <summary>The text a reader sees (and a search runs on) for the mode.</summary>
    internal static string DisplayText(string? text, OmniAnsiMode mode) =>
        mode == OmniAnsiMode.Raw ? text ?? string.Empty : OmniAnsi.Strip(text);

    /// <summary>The runs of the displayed line, with the matches of <paramref name="search"/> cut out.</summary>
    internal static IReadOnlyList<AnsiPiece> Pieces(string? text, OmniAnsiMode mode, string? search)
    {
        if (mode != OmniAnsiMode.Render)
        {
            return [.. OmniTextMatch.Split(DisplayText(text, mode), search).Select(part => new AnsiPiece(part.Text, null, part.Matched))];
        }

        var segments = OmniAnsi.Parse(text);
        if (string.IsNullOrWhiteSpace(search))
        {
            return [.. segments.Select(segment => new AnsiPiece(segment.Text, segment, false))];
        }

        var matches = OmniTextMatch.Split(string.Concat(segments.Select(segment => segment.Text)), search);
        var pieces = new List<AnsiPiece>();
        var current = 0;
        var consumed = 0;
        foreach (var segment in segments)
        {
            var taken = 0;
            while (taken < segment.Text.Length && current < matches.Count)
            {
                var match = matches[current];
                var take = Math.Min(match.Text.Length - consumed, segment.Text.Length - taken);
                pieces.Add(new AnsiPiece(segment.Text.Substring(taken, take), segment, match.Matched));
                taken += take;
                consumed += take;
                if (consumed == match.Text.Length)
                {
                    current++;
                    consumed = 0;
                }
            }
        }

        return pieces;
    }

    /// <summary>The CSS classes that draw a run, or null when it carries no attribute.</summary>
    internal static string? ClassOf(OmniAnsiSegment segment)
    {
        var classes = new List<string>(4);
        if (segment.Bold)
        {
            classes.Add("omni-ansi-bold");
        }

        if (segment.Dim)
        {
            classes.Add("omni-ansi-dim");
        }

        if (segment.Italic)
        {
            classes.Add("omni-ansi-italic");
        }

        if (segment.Underline)
        {
            classes.Add("omni-ansi-underline");
        }

        if (segment.Foreground is >= 0 and <= 15)
        {
            classes.Add(ForegroundClasses[segment.Foreground.Value]);
        }

        if (segment.Background is >= 0 and <= 15)
        {
            classes.Add(BackgroundClasses[segment.Background.Value]);
        }

        return classes.Count == 0 ? null : string.Join(' ', classes);
    }

    /// <summary>The markup of a line: a <c>span</c> with classes per styled run, a <c>mark</c> per match, plain text elsewhere.</summary>
    internal static RenderFragment Fragment(string? text, OmniAnsiMode mode, string? search) => builder =>
    {
        foreach (var piece in Pieces(text, mode, search))
        {
            var css = piece.Style is null ? null : ClassOf(piece.Style);
            if (css is not null)
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", css);
            }

            if (piece.Matched)
            {
                builder.OpenElement(2, "mark");
                builder.AddAttribute(3, "class", "omni-log-viewer__match");
                builder.AddContent(4, piece.Text);
                builder.CloseElement();
            }
            else
            {
                builder.AddContent(5, piece.Text);
            }

            if (css is not null)
            {
                builder.CloseElement();
            }
        }
    };
}
