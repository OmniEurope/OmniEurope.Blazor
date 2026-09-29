namespace OmniEurope.Blazor.Components;

/// <summary>
/// The formatting at the caret of an <see cref="OmniHtmlEditor"/>, as its visual surface reports it:
/// the marks in force, the block, the alignment and the font size.
/// </summary>
internal sealed record HtmlEditorFormatState(IReadOnlySet<string> Marks, string Block, string Align, string Size)
{
    /// <summary>A caret in a plain left-aligned paragraph of normal size, with no mark.</summary>
    public static HtmlEditorFormatState Empty { get; } = new(new HashSet<string>(StringComparer.Ordinal), "p", "left", "normal");

    /// <summary>Reads <c>marks|block|align|size</c>; anything else gives <see cref="Empty"/>.</summary>
    public static HtmlEditorFormatState Parse(string? state)
    {
        var parts = (state ?? string.Empty).Split('|');
        if (parts.Length != 4)
        {
            return Empty;
        }

        return new(
            parts[0].Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal),
            parts[1].Length == 0 ? "p" : parts[1],
            parts[2].Length == 0 ? "left" : parts[2],
            parts[3].Length == 0 ? "normal" : parts[3]);
    }
}
