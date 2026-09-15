namespace OmniEurope.Blazor.Components;

public partial class OmniHeader
{
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public bool Sticky { get; set; }

    /// <summary>The header's colour: the surface by default, or the theme's accent band.</summary>
    [Parameter]
    public OmniHeaderTone Tone { get; set; } = OmniHeaderTone.Surface;
}
