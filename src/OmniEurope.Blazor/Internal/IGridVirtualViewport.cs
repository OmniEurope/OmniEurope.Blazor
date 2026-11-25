namespace OmniEurope.Blazor.Components;

/// <summary>The scroll window of a virtualized grid as its other collaborators use it, from <see cref="GridVirtualViewport{TItem}"/>.</summary>
internal interface IGridVirtualViewport
{
    bool RowsAreFixed { get; }

    double? FixedHeight { get; }

    GridVirtualRange Range { get; }

    bool ApplySnapshot(GridViewportSnapshot? snapshot);

    void Sync();

    void RefreshSlots();

    void ResetMeasurements();

    void ResetScroll();
}
