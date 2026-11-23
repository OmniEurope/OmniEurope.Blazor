using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>The columns of a grid as its other collaborators read them, from <see cref="GridColumnSet{TItem}"/>.</summary>
/// <typeparam name="TItem">The type of the rows.</typeparam>
internal interface IGridColumnSet<TItem>
{
    IReadOnlyList<OmniDataGridColumnDefinition<TItem>> Declared { get; }

    IReadOnlyList<OmniDataGridColumnDefinition<TItem>> EffectiveColumns { get; }

    IReadOnlyList<OmniDataGridColumnDefinition<TItem>> VisibleColumns { get; }

    bool HasEditing { get; }

    bool ShowDetailColumn { get; }

    bool HasFrozenColumns { get; }

    bool HasGroupFooter { get; }

    OmniDataGridColumnDefinition<TItem>? Find(string key);

    OmniDataGridColumnDefinition<TItem>? FindVisible(string key);
}
