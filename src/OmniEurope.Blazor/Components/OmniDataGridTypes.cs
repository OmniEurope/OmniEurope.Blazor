using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Components;

public enum OmniDataGridSelectionMode
{
    None,
    Single,
    Multiple
}

public enum OmniDataGridFilterOperator
{
    Contains,
    Equals,
    StartsWith,
    EndsWith,
    GreaterThan,
    LessThan
}

public sealed record OmniDataGridSort(string Key, bool Descending);
public sealed record OmniDataGridFilter(string Key, OmniDataGridFilterOperator Operator, string Value);

public enum OmniDataGridColumnWidth
{
    Auto,
    Narrow,
    Medium,
    Wide
}

public sealed record OmniDataGridColumnWidthChange(string Key, OmniDataGridColumnWidth Width);

public sealed record OmniDataGridLoadRequest(
    int Page,
    int PageSize,
    IReadOnlyList<OmniDataGridSort> Sorts,
    IReadOnlyList<OmniDataGridFilter> Filters,
    CancellationToken CancellationToken)
{
    public string? SortKey => Sorts.FirstOrDefault()?.Key;
    public bool SortDescending => Sorts.FirstOrDefault()?.Descending == true;
}

public sealed record OmniDataGridResult<TItem>(IReadOnlyList<TItem> Items, int TotalCount);

public sealed class OmniDataGridColumnDefinition<TItem>
{
    public required string Key { get; init; }
    public required string Title { get; init; }
    public required Func<TItem, object?> Value { get; init; }
    public RenderFragment<TItem>? Template { get; init; }
    public RenderFragment<TItem>? EditTemplate { get; init; }
    public Func<TItem, string, bool>? FilterPredicate { get; init; }
    public Func<object?, string>? Format { get; init; }
    public bool Sortable { get; init; } = true;
    public bool Filterable { get; init; }
    public OmniDataGridFilterOperator FilterOperator { get; init; }
    public bool Visible { get; init; } = true;
    public bool Resizable { get; init; }
    public OmniDataGridColumnWidth Width { get; init; }
}

public sealed class OmniDataGridContext<TItem>
{
    public required Action<OmniDataGridColumnDefinition<TItem>> Register { get; init; }
    public required Action<string> Unregister { get; init; }
}
