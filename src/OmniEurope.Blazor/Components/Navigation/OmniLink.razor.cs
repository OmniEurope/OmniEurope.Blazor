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

    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>
    /// Runs when the link is clicked, besides following <see cref="Href"/>: an action that goes with
    /// the navigation (recording a choice, closing a panel). The navigation itself is unchanged.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    private bool ShowsExternalIcon => NewTab;

    private string? SafeHref => OmniUriPolicy.EnsureSafe(Href, nameof(Href));
}
