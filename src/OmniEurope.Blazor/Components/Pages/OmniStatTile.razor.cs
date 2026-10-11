namespace OmniEurope.Blazor.Components;

/// <summary>
/// A key figure on a tile: an icon, the value, what it measures and an optional detail line. Given
/// <see cref="OnClick"/>, the whole tile becomes one button that opens the figure's detail.
/// </summary>
public partial class OmniStatTile
{
    private ElementReference _tile;
    private IJSObjectReference? _module;
    private ElementReference? _attached;
    private bool _disposed;

    /// <summary>A decorative icon on a tinted square before the text. None when not given.</summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>The figure itself, already formatted: "12,4 k", "83 %".</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>What the figure measures ("Tokens today"), written under the value; with <see cref="OnClick"/>, the start of the accessible name of the button.</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>An optional muted line under the label: a reset time, a comparison.</summary>
    [Parameter]
    public string? Detail { get; set; }

    /// <summary>
    /// Makes the tile a button, named by <see cref="Label"/> followed by <see cref="Value"/>, as the
    /// tile reads them.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Attaches the script that keeps the label on one line: smaller when it does not fit, then cut with
    /// an ellipsis and its whole text in the hover tooltip. The tile is a button with <see cref="OnClick"/>
    /// and a plain block without it, two different elements: when <see cref="OnClick"/> comes or goes, the
    /// script leaves the former element and follows the new one. A lost circuit is ignored.
    /// </summary>
    /// <param name="firstRender">True on the first render of the component.</param>
    /// <returns>A task that completes once the script follows the element drawn.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (firstRender)
            {
                var module = await JavaScript.InvokeAsync<IJSObjectReference>("import", Internal.OmniModules.StatTile);
                if (_disposed)
                {
                    await module.DisposeAsync();
                    return;
                }

                _module = module;
            }

            // On Blazor Server a later render can run while the first one still awaits the module: it
            // leaves the attachment to the first render, which attaches the element drawn by then.
            if (_module is null || _attached?.Id == _tile.Id)
            {
                return;
            }

            if (_attached is { } former)
            {
                await _module.InvokeVoidAsync("detach", former);
            }

            await _module.InvokeVoidAsync("attach", _tile);
            _attached = _tile;
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the tile with it.
        }
    }

    /// <summary>Detaches the script: its observers are released.</summary>
    /// <returns>A task that completes once the script is detached; a lost circuit is ignored.</returns>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_module is not null)
        {
            try
            {
                if (_attached is { } attached)
                {
                    await _module.InvokeVoidAsync("detach", attached);
                }

                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        GC.SuppressFinalize(this);
    }

    private string AccessibleName => string.IsNullOrWhiteSpace(Value) ? Label : Localize("LabelValuePair", Label, Value);
}
