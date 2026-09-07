namespace OmniEurope.Blazor.Components;

public partial class OmniProfileMenuItem
{
    [Parameter]
    public string? Href { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public EventCallback OnClick { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string? SafeHref => OmniUriPolicy.EnsureSafe(Href, nameof(Href));
}
