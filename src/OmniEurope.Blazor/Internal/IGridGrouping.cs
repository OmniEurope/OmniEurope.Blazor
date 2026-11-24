using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>The grouping of a grid as its other collaborators use it, from <see cref="GridGrouping{TItem}"/>.</summary>
/// <typeparam name="TItem">The type of the rows.</typeparam>
internal interface IGridGrouping<TItem>
{
    IReadOnlyList<OmniDataGridGroup> ActiveGroups { get; }

    IReadOnlyList<OmniDataGridGroup>? ChosenGroups { get; }

    bool Restore(IReadOnlyList<OmniDataGridGroup>? groups);

    IReadOnlyList<GridRenderRow<TItem>> GroupedRows(IReadOnlyList<OmniDataGridGroup> groups, IReadOnlyList<TItem> items);
}
