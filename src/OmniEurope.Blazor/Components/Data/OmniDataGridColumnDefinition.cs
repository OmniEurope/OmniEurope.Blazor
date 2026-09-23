using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Components;

internal sealed class OmniDataGridColumnDefinition<TItem>
{
    public required string Key { get; init; }
    public required string Title { get; init; }
    public required Func<TItem, object?> Value { get; init; }
    public string? Property { get; init; }
    public string? SortProperty { get; init; }
    public Func<TItem, object?>? SortValue { get; init; }
    public RenderFragment<TItem>? Template { get; init; }
    public RenderFragment<TItem>? EditTemplate { get; init; }
    public RenderFragment<TItem>? FooterTemplate { get; init; }
    public RenderFragment? HeaderTemplate { get; init; }
    public Func<TItem, string, bool>? FilterPredicate { get; init; }
    public Func<object?, string>? Format { get; init; }
    public string? FormatString { get; init; }
    public bool Sortable { get; init; } = true;
    public OmniDataGridSortOrder? SortOrder { get; init; }
    public bool Filterable { get; init; }
    public OmniDataGridColumnFilterType FilterType { get; init; }
    /// <summary>Adds a narrowing box above a MultiSelect filter; ignored by the other types.</summary>
    public bool FilterSearchable { get; init; }
    /// <summary>Explicit Select/Combo suggestions; null derives them from the column's own values.</summary>
    public IEnumerable<string>? FilterValues { get; init; }
    /// <summary>Column-supplied filter editor, overriding <see cref="FilterType"/>.</summary>
    public RenderFragment<OmniDataGridFilterContext>? FilterTemplate { get; init; }
    /// <summary>Display text of a filter candidate; null shows the value itself.</summary>
    public Func<string, string>? FilterValueText { get; init; }
    /// <summary>Filter applied when the column first registers, unless saved state restored one.</summary>
    public string? DefaultFilterValue { get; init; }
    /// <summary>A DateRange filter picks hours too.</summary>
    public bool FilterIncludesTime { get; init; }
    /// <summary>The enum the column reads, when it reads one: its members are the filter candidates.</summary>
    public Type? EnumType { get; init; }
    /// <summary>The type the column reads (nullable unwrapped), or null when it is not known from a property.</summary>
    public Type? ValueType { get; init; }
    public OmniDataGridFilterOperator FilterOperator { get; init; }
    public OmniDataGridFilterOperator SecondFilterOperator { get; init; }
    public OmniDataGridLogicalOperator LogicalFilterOperator { get; init; }
    public bool Visible { get; init; } = true;
    /// <summary>Null follows the grid's own AllowColumnResize; false pins this column.</summary>
    public bool? Resizable { get; init; }
    public bool Frozen { get; init; }
    public string? Width { get; init; }
    public string? MinWidth { get; init; }
    public OmniDataGridTextAlign TextAlign { get; init; }
    /// <summary>The property read is a number: the column aligns at the end unless its alignment was set.</summary>
    public bool Numeric { get; init; }
    public string? CssClass { get; init; }
    public string? HeaderCssClass { get; init; }
    public bool Groupable { get; init; } = true;
}
