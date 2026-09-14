namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The one back action of the page compositions: to the page named when there is one, otherwise one
/// step back in the browser history, as the browser's own back button would.
/// </summary>
internal static class OmniBackNavigation
{
    private const string InteropModulePath = "./_content/OmniEurope.Blazor/omniInterop.js";

    internal static async Task GoBackAsync(NavigationManager navigation, IJSRuntime javaScript, string? href)
    {
        if (!string.IsNullOrWhiteSpace(href))
        {
            navigation.NavigateTo(OmniUriPolicy.EnsureSafe(href, "BackHref")!);
            return;
        }

        try
        {
            await using var module = await javaScript.InvokeAsync<IJSObjectReference>("import", InteropModulePath);
            await module.InvokeVoidAsync("historyBack");
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
