using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Internal;

/// <summary>The layout pushed to the grid script as the grid's other collaborators use it, from <see cref="GridLayoutInterop{TItem}"/>.</summary>
internal interface IGridLayoutInterop
{
    string? EffectiveMaxHeight { get; }

    void InvalidateColumnLayout();

    Task ApplyLayoutAsync();

    Task ApplyMaxHeightAsync();

    Task ApplyVirtualLayoutAsync(IJSObjectReference module);

    Task ApplyRowHeightAsync(double? rowHeight);

    Task ApplyColumnLayoutAsync();
}
