namespace OmniEurope.Blazor.Components;

/// <summary>The label of a form control, with the required mark.</summary>
public partial class OmniLabel
{
    /// <summary>The id of the control it names (<c>for</c>).</summary>
    [Parameter, EditorRequired]
    public string For { get; set; } = string.Empty;

    /// <summary>The text of the label.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Adds the required mark, hidden from assistive technologies (the control carries its own requirement).</summary>
    [Parameter]
    public bool Required { get; set; }
}
