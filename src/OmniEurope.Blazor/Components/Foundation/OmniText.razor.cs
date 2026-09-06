namespace OmniEurope.Blazor.Components;

public partial class OmniText
{
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public OmniTextElement Element { get; set; }

    [Parameter]
    public OmniTextTone Tone { get; set; }

    [Parameter]
    public bool Truncate { get; set; }

    private string TextClass => Css(
        "omni-text",
        $"omni-text--{Tone.ToString().ToLowerInvariant()}",
        Truncate ? "omni-text--truncate" : null);
}
