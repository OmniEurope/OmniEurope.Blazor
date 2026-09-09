namespace OmniEurope.Blazor.Components;

public partial class OmniFieldset
{
    [Parameter, EditorRequired]
    public RenderFragment? Legend { get; set; }

    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Whether the group folds away. A section of rarely used settings is worth grouping without
    /// making every reader scroll past it.
    /// </summary>
    [Parameter]
    public bool Collapsible { get; set; }

    /// <summary>
    /// Whether a collapsible group is folded when it first renders. The disclosure is the browser's
    /// own from then on: it opens and closes without a round trip, and this parameter does not track
    /// it. There is deliberately no CollapsedChanged, because the native element reports no state a
    /// component could read back without script.
    /// </summary>
    [Parameter]
    public bool Collapsed { get; set; }
}
