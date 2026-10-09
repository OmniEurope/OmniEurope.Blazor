using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The skip link (WCAG 2.4.1): the first element of the page, shown only while it has the focus, which
/// moves the focus past the header and the navigation to the content, an <see cref="OmniMain"/> by
/// default. Place it first in the layout, before the header.
/// </summary>
/// <remarks>
/// The click moves the focus by script instead of following the link: under <c>&lt;base href="/"&gt;</c>
/// a fragment link leads to the root page, not to a place in the current one. Before the page is
/// interactive, the link follows its address, the current page with <see cref="TargetId"/> as fragment.
/// </remarks>
public partial class OmniSkipLink
{
    private const string InteropModulePath = Internal.OmniModules.Interop;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// The id of the element that receives the focus; null, the default, takes the first <c>main</c>
    /// landmark of the page (an <see cref="OmniMain"/>, focusable from script by default). A target that
    /// cannot take the focus is given <c>tabindex="-1"</c>.
    /// </summary>
    [Parameter]
    public string? TargetId { get; set; }

    /// <summary>The text of the link; null or blank takes the localized "Skip to main content".</summary>
    [Parameter]
    public string? Text { get; set; }

    private string EffectiveText => string.IsNullOrWhiteSpace(Text) ? Localize("SkipLinkText") : Text;

    private string Href
    {
        get
        {
            var page = Navigation.Uri;
            var hash = page.IndexOf('#', StringComparison.Ordinal);
            if (hash >= 0)
            {
                page = page[..hash];
            }

            return string.IsNullOrWhiteSpace(TargetId) ? page : $"{page}#{Uri.EscapeDataString(TargetId)}";
        }
    }

    private async Task FocusTargetAsync()
    {
        try
        {
            var module = await JavaScript.InvokeAsync<IJSObjectReference>("import", InteropModulePath);
            await using (module.ConfigureAwait(true))
            {
                await module.InvokeAsync<bool>("focusSkipTarget", TargetId);
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the page with it.
        }
    }
}
