using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Resources;

namespace OmniEurope.Blazor.Components;

public partial class OmniLink
{
    [Inject] private IStringLocalizer<AppStrings> StringLocalizer { get; set; } = default!;

    [Parameter, EditorRequired]
    public string Href { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public bool NewTab { get; set; }

    /// <summary>
    /// Marks a link that opens a new tab with the external-link icon after its text, and says so to
    /// assistive technologies. On by default; turn it off where the context already makes it plain.
    /// </summary>
    [Parameter]
    public bool ShowNewTabIcon { get; set; } = true;

    [Parameter]
    public string? AriaLabel { get; set; }

    private bool ShowsExternalIcon => NewTab && ShowNewTabIcon;

    private string? SafeHref => OmniUriPolicy.EnsureSafe(Href, nameof(Href));
}
