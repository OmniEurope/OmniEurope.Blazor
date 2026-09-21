using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Placed once in a layout, it replaces the browser's tooltip of every element carrying a title
/// attribute with the package tooltip: at most 12rem wide, shown after a short delay at the pointer,
/// with a chevron toward it, and kept inside the window. It renders nothing itself.
/// </summary>
public sealed class OmniTitleTooltips : ComponentBase, IAsyncDisposable
{
    private const string ModulePath = "./_content/OmniEurope.Blazor/omni-tooltip.js";

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    private IJSObjectReference? _module;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _module = await JavaScript.InvokeAsync<IJSObjectReference>("import", ModulePath);
        await _module.InvokeVoidAsync("installTitleTooltips");
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
