namespace OmniEurope.Blazor.Components;

public partial class OmniCard
{
    [Parameter]
    public RenderFragment? Header { get; set; }

    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public RenderFragment? Footer { get; set; }

    /// <summary>
    /// The density of this component and of what it holds (control heights, paddings, gaps), over
    /// the one it inherits from its theme scope or section. Null, the default, inherits it.
    /// </summary>
    [Parameter]
    public OmniDensity? Density { get; set; }
}
