namespace OmniEurope.Blazor.Components;

public partial class OmniTimeline
{
    [Parameter] public string Label { get; set; } = string.Empty;
    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localize("TimelineLabel")
        : Label;
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
