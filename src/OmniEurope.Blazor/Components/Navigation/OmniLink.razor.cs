namespace OmniEurope.Blazor.Components;

public partial class OmniLink
{
    [Parameter, EditorRequired]
    public string Href { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public bool NewTab { get; set; }

    [Parameter]
    public string? AriaLabel { get; set; }

    private string? SafeHref => OmniUriPolicy.EnsureSafe(Href, nameof(Href));
}
