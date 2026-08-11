namespace OmniEurope.Blazor.Components;

public partial class OmniBreadcrumbItem
{
    [Parameter]
    public string? Href { get; set; }

    [Parameter]
    public bool Current { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string? SafeHref => OmniUriPolicy.EnsureSafe(Href, nameof(Href));
}
