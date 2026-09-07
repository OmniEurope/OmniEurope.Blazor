namespace OmniEurope.Blazor.Components;

public partial class OmniPanelMenu
{
    [Parameter]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Narrow the items down to their icons. The text stays in the markup so a screen reader still
    /// announces the destination, and a title attribute is not a substitute for it.
    /// </summary>
    [Parameter]
    public OmniPanelMenuDisplayStyle DisplayStyle { get; set; } = OmniPanelMenuDisplayStyle.IconAndText;

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localize("PanelMenuLabel")
        : Label;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
