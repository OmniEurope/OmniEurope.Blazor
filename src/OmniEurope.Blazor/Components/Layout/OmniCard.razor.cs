namespace OmniEurope.Blazor.Components;

/// <summary>
/// A bordered surface (<c>section</c>) that groups related content, with an optional header and footer.
/// </summary>
public partial class OmniCard
{
    /// <summary>The top band of the card (<c>header</c>); null, the default, renders none.</summary>
    [Parameter]
    public RenderFragment? Header { get; set; }

    /// <summary>The body of the card. Required.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The bottom band of the card (<c>footer</c>), for its actions; null, the default, renders none.</summary>
    [Parameter]
    public RenderFragment? Footer { get; set; }

    /// <summary>
    /// The density of this component and of what it holds (control heights, paddings, gaps), over
    /// the one it inherits from its theme scope or section. Null, the default, inherits it.
    /// </summary>
    [Parameter]
    public OmniDensity? Density { get; set; }
}
