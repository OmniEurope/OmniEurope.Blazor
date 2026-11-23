namespace OmniEurope.Blazor.Internal;

/// <summary>The rows of a grid as its other collaborators read them, from <see cref="GridDataView{TItem}"/>.</summary>
/// <typeparam name="TItem">The type of the rows.</typeparam>
internal interface IGridDataView<TItem>
{
    bool Virtualized { get; }

    bool Paged { get; }

    bool StructuredVirtual { get; }

    IReadOnlyList<TItem> VirtualLocalItems { get; }

    IReadOnlyList<TItem> VisibleItems { get; }

    int TotalCount { get; }

    bool FirstRequestReady { get; }

    bool Loading { get; }

    bool IsEmpty { get; }

    int LoadedItemsVersion { get; }

    IEnumerable<TItem> LoadedItems();

    void InvalidateLocalProjection();

    bool TryGetVirtualItem(int index, out TItem item);

    Task EnsureVirtualDataAsync();

    Task QueryChangedAsync();

    Task RefreshAfterQueryChangeAsync();

    Task ReloadAsync();
}
