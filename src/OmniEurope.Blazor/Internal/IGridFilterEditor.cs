
namespace OmniEurope.Blazor.Components;

/// <summary>The column filter editors as the grid's other collaborators read them, from <see cref="GridFilterEditor{TItem}"/>.</summary>
/// <typeparam name="TItem">The type of the rows.</typeparam>
internal interface IGridFilterEditor<TItem>
{
    bool HasPopovers { get; }

    string FilterSummary(OmniDataGridColumnDefinition<TItem> column);

    IReadOnlyList<string> DistinctFilterValues(OmniDataGridColumnDefinition<TItem> column);

    bool ShowsDateText(OmniDataGridColumnDefinition<TItem> column);

    bool HoldsRange(OmniDataGridColumnDefinition<TItem> column, bool inPanel);
}
