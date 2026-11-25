namespace OmniEurope.Blazor.Components;

/// <summary>The rendered rows of a grid as its other collaborators read them, from <see cref="GridRowBuilder{TItem}"/>.</summary>
/// <typeparam name="TItem">The type of the rows.</typeparam>
internal interface IGridRowBuilder<TItem>
{
    IReadOnlyList<GridRenderRow<TItem>> Slots { get; }

    void ForgetSlots();

    bool TakeSlotsRebuilt();

    IReadOnlyList<GridRenderRow<TItem>> RenderRows();

    IReadOnlyList<GridRenderRow<TItem>> FlatRows(IReadOnlyList<TItem> items);

    IEnumerable<GridRenderRow<TItem>> PageRows();

    GridRenderRow<TItem> Describe(TItem item, int index, IReadOnlyList<GridGroupHeader> headers, bool showDetail);

    IEnumerable<TItem> CurrentRows();
}
