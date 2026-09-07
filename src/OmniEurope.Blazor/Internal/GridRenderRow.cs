namespace OmniEurope.Blazor.Internal;

/// <summary>
/// One entry of the grid body as the markup should emit it: the group headers that open above it,
/// the row itself (or a placeholder while its data is still loading) and an optional detail row.
/// </summary>
internal sealed record GridRenderRow<TItem>(
    int Index,
    TItem Item,
    bool HasItem,
    IReadOnlyList<GridGroupHeader> Headers,
    bool ShowDetail,
    string? CssClass,
    bool Expandable,
    bool Selectable);
