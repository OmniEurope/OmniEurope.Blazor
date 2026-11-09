namespace OmniEurope.Blazor.Components;

/// <summary>
/// A text with every occurrence of a filter text marked, as the suggestions of the grid filters and of
/// <c>OmniAutocomplete</c> mark theirs (same comparison, same <c>omni-combo__match</c> class). Use
/// it in a list or a grid cell that a filter box narrows. The filter is literal text, never a pattern,
/// and the text is encoded, never written as markup.
/// </summary>
public partial class OmniFilterHighlight
{
    /// <summary>The text to draw. Null draws nothing.</summary>
    [Parameter, EditorRequired]
    public string? Text { get; set; }

    /// <summary>
    /// The text to mark, trimmed. Accents are ignored in the current culture ("liege" marks the accented
    /// name). Null or empty marks nothing.
    /// </summary>
    [Parameter]
    public string? Filter { get; set; }

    /// <summary>Whether upper and lower case match each other. True by default.</summary>
    [Parameter]
    public bool IgnoreCase { get; set; } = true;
}
