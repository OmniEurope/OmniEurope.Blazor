namespace OmniEurope.Blazor.Components;

/// <summary>A run of text in one of the typographic elements, with a colour and an optional truncation.</summary>
public partial class OmniText
{
    /// <summary>The text.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The element drawn: a span by default, or a paragraph, strong, emphasis or small text.</summary>
    [Parameter]
    public OmniTextElement Element { get; set; }

    /// <summary>The colour of the text; <see cref="OmniTextTone.Neutral"/> by default.</summary>
    [Parameter]
    public OmniTextTone Tone { get; set; }

    /// <summary>Keeps the text on one line, cut with an ellipsis when it does not fit.</summary>
    [Parameter]
    public bool Truncate { get; set; }

    private string TextClass => Css(
        "omni-text",
        $"omni-text--{Tone.ToString().ToLowerInvariant()}",
        Truncate ? "omni-text--truncate" : null);
}
