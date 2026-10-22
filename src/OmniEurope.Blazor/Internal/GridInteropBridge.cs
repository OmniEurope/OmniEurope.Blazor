using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The only .NET object the grid scripts (<c>omni-grid.js</c> and <c>./grid/*</c>) can call: it forwards what
/// they report to the grid. Kept apart from the component so that the callable surface is internal rather
/// than public API, as <see cref="KanbanInteropBridge"/> does for the board.
/// </summary>
/// <typeparam name="TItem">The type of the grid's items.</typeparam>
internal sealed class GridInteropBridge<TItem>(OmniDataGrid<TItem> grid)
{
    /// <summary>The viewport left its horizontal start, or came back to it.</summary>
    [JSInvokable]
    public Task OnHorizontalScrollChangedAsync(bool scrolled) => grid.OnHorizontalScrollChangedAsync(scrolled);

    /// <summary>The rendered rows changed size without any scroll.</summary>
    [JSInvokable]
    public Task OnContentResizedAsync() => grid.OnContentResizedAsync();

    /// <summary>The viewport was scrolled or resized.</summary>
    [JSInvokable]
    public Task OnViewportChangedAsync(double scrollTop, double viewportHeight) =>
        grid.OnViewportChangedAsync(scrollTop, viewportHeight);

    /// <summary>The texts a fit to content measures for a column.</summary>
    [JSInvokable]
    public string[]? GetColumnAutoFitTexts(string key) => grid.GetColumnAutoFitTexts(key);

    /// <summary>The width a fit to content chose.</summary>
    [JSInvokable]
    public Task OnColumnAutoFitAsync(string key, double width) => grid.OnColumnAutoFitAsync(key, width);

    /// <summary>The final width of a drag on a resize handle.</summary>
    [JSInvokable]
    public Task OnColumnResizedAsync(string key, double width) => grid.OnColumnResizedAsync(key, width);

    /// <summary>The end of a header drag.</summary>
    [JSInvokable]
    public Task OnColumnDroppedAsync(string key, string targetKey, bool after) => grid.OnColumnDroppedAsync(key, targetKey, after);
}
