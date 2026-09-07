namespace OmniEurope.Blazor.Components;

public partial class OmniBreadcrumb
{
    [Parameter]
    public string Label { get; set; } = string.Empty;

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localize("BreadcrumbLabel")
        : Label;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
