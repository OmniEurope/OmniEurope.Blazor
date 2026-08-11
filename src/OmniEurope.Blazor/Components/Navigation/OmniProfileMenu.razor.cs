namespace OmniEurope.Blazor.Components;

public partial class OmniProfileMenu
{
    [Parameter]
    public string Label { get; set; } = string.Empty;

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localize("ProfileMenuLabel")
        : Label;

    [Parameter, EditorRequired]
    public RenderFragment? Summary { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
