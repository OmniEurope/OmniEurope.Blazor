using System.Globalization;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The column filter editors of a grid: where they appear (inline row, header menu, popovers), which
/// shape each column gets, their labels, ids and summary, and the staging of a change until it is
/// applied.
/// </summary>
internal sealed class GridFilterEditor<TItem>(OmniDataGrid<TItem> grid)
{
    private bool UsesAdvancedFilter => grid.FilterMode == OmniDataGridFilterMode.Advanced;

    private bool ShowsOperatorSelector => grid.FilterMode != OmniDataGridFilterMode.Simple;

    /// <summary>
    /// A column that says nothing follows its grid, provided it has something to filter on; a column that
    /// says true or false decides for itself.
    /// </summary>
    internal bool IsFilterable(OmniDataGridColumnDefinition<TItem> column) =>
        column.Filterable ?? (grid.Filterable && column.HasFilterSource);

    /// <summary>Whether the filter row shows each column's condition in a popover (advanced mode).</summary>
    internal bool EditsInPopover => UsesAdvancedFilter;

    /// <summary>
    /// The inline filter row and the per-column header menu are two entry points to the same
    /// filter, so only one of them is ever rendered: turning the header menu on moves the value
    /// control into the header and drops the row.
    /// </summary>
    internal bool HasFilterRow => !grid.ShowHeaderFilterMenu && grid.ColumnSet.VisibleColumns.Any(IsFilterable);

    /// <summary>Whether any filter editor of this grid opens something over the rows.</summary>
    internal bool HasPopovers => grid.ColumnSet.VisibleColumns.Any(column => IsFilterable(column)
        && (grid.ShowHeaderFilterMenu
            || UsesAdvancedFilter
            || column.FilterTemplate is not null
            || column.FilterType is OmniDataGridColumnFilterType.MultiSelect or OmniDataGridColumnFilterType.Combo));

    /// <summary>
    /// Whether this column's editor is the two-condition one of the advanced mode. A checkable list
    /// and a date range already express their whole condition (any of these values, between these
    /// dates), so they keep their single editor and apply as they change, in every mode.
    /// </summary>
    internal bool UsesAdvancedEditor(OmniDataGridColumnDefinition<TItem> column) =>
        UsesAdvancedFilter && !HasSelfContainedEditor(column);

    internal bool ShowsOperatorSelectorFor(OmniDataGridColumnDefinition<TItem> column) =>
        ShowsOperatorSelector && !HasSelfContainedEditor(column);

    private static bool HasSelfContainedEditor(OmniDataGridColumnDefinition<TItem> column) =>
        column.FilterTemplate is null
        && column.FilterType is OmniDataGridColumnFilterType.MultiSelect or OmniDataGridColumnFilterType.DateRange;

    internal bool IsOneConditionPanel(OmniDataGridColumnDefinition<TItem> column) =>
        !UsesAdvancedEditor(column) && !HasSelfContainedEditor(column);

    /// <summary>
    /// In a popover, a one-condition editor puts its operator on the first line and its value with
    /// the square clear action on the second; the two-condition editor and the self-contained ones
    /// (a list, a range) stack their parts.
    /// </summary>
    internal string FilterEditorClass(OmniDataGridColumnDefinition<TItem> column, bool inPanel) => !inPanel
        ? "omni-data-grid__filter-editor"
        : IsOneConditionPanel(column)
            ? "omni-data-grid__filter-editor omni-data-grid__filter-editor--panel omni-data-grid__filter-editor--row"
            : "omni-data-grid__filter-editor omni-data-grid__filter-editor--panel";

    internal IReadOnlyList<OmniDataGridFilterOperator> OperatorsFor(OmniDataGridColumnDefinition<TItem> column) =>
        GridFilterOperators<TItem>.OperatorsFor(column);

    internal GridColumnFilter DraftOf(OmniDataGridColumnDefinition<TItem> column) => grid.Query.DraftOf(column);

    internal string FilterValue(OmniDataGridColumnDefinition<TItem> column) => DraftOf(column).Value;

    internal string SecondFilterValue(OmniDataGridColumnDefinition<TItem> column) => DraftOf(column).SecondValue;

    internal string FilterId(OmniDataGridColumnDefinition<TItem> column) => $"{grid.Id ?? "omni-grid"}-filter-{column.Key}";

    internal string HeaderFilterId(OmniDataGridColumnDefinition<TItem> column) => $"{FilterId(column)}-menu";

    /// <summary>What the advanced filter trigger reads: the applied condition, or the placeholder.</summary>
    internal string FilterSummary(OmniDataGridColumnDefinition<TItem> column)
    {
        if (!grid.Query.Filters.TryGetValue(column.Key, out var filter) || !filter.IsActive)
        {
            return grid.Text("GridFilterPlaceholder");
        }

        var first = filter.HasFirst ? Condition(filter.Operator, filter.Value) : null;
        var second = filter.HasSecond ? Condition(filter.SecondOperator, filter.SecondValue) : null;
        return first is not null && second is not null
            ? $"{first} {LogicalLabel(filter.LogicalOperator).ToLower(CultureInfo.CurrentCulture)} {second}"
            : first ?? second ?? string.Empty;

        string Condition(OmniDataGridFilterOperator candidate, string value) => GridColumnFilter.IsValueless(candidate)
            ? OperatorLabel(candidate)
            : $"{OperatorLabel(candidate)} {DisplayFilterValue(column, value)}";
    }

    /// <summary>A multi-valued filter travels encoded; the summary shows its values, not the encoding.</summary>
    private static string DisplayFilterValue(OmniDataGridColumnDefinition<TItem> column, string value) =>
        column.FilterType == OmniDataGridColumnFilterType.DateRange
            ? string.Join(" - ", OmniDataGridDateRange.Split(value).Start, OmniDataGridDateRange.Split(value).End)
            : string.Join(", ", OmniDataGridFilterValues.Split(value).Select(candidate => CandidateText(column, candidate)));

    /// <summary>What one filter candidate reads as: the column's text for it, or the value itself.</summary>
    internal static string CandidateText(OmniDataGridColumnDefinition<TItem> column, string candidate) =>
        column.FormatFilterValue?.Invoke(candidate) ?? candidate;

    internal string OperatorLabel(OmniDataGridFilterOperator candidate) => candidate switch
    {
        OmniDataGridFilterOperator.Contains => grid.Text("GridFilterContains"),
        OmniDataGridFilterOperator.DoesNotContain => grid.Text("GridFilterDoesNotContain"),
        OmniDataGridFilterOperator.Equals => grid.Text("GridFilterEquals"),
        OmniDataGridFilterOperator.NotEquals => grid.Text("GridFilterNotEquals"),
        OmniDataGridFilterOperator.StartsWith => grid.Text("GridFilterStartsWith"),
        OmniDataGridFilterOperator.EndsWith => grid.Text("GridFilterEndsWith"),
        OmniDataGridFilterOperator.GreaterThan => grid.Text("GridFilterGreaterThan"),
        OmniDataGridFilterOperator.GreaterThanOrEquals => grid.Text("GridFilterGreaterThanOrEquals"),
        OmniDataGridFilterOperator.LessThan => grid.Text("GridFilterLessThan"),
        OmniDataGridFilterOperator.LessThanOrEquals => grid.Text("GridFilterLessThanOrEquals"),
        OmniDataGridFilterOperator.IsNull => grid.Text("GridFilterIsNull"),
        OmniDataGridFilterOperator.IsNotNull => grid.Text("GridFilterIsNotNull"),
        OmniDataGridFilterOperator.IsEmpty => grid.Text("GridFilterIsEmpty"),
        _ => grid.Text("GridFilterIsNotEmpty")
    };

    internal string LogicalLabel(OmniDataGridLogicalOperator candidate) => candidate == OmniDataGridLogicalOperator.Or
        ? grid.Text("GridFilterOr")
        : grid.Text("GridFilterAnd");

    /// <summary>
    /// Distinct string values for a Select/Combo filter, read from the locally held items. A remote
    /// grid only ever sees the current page, so Select/Combo on such a grid is a known limitation
    /// rather than a silent wrong answer.
    /// </summary>
    internal IReadOnlyList<string> DistinctFilterValues(OmniDataGridColumnDefinition<TItem> column) =>
        column.FilterValues is { } declared
            // Declared values keep their order, as an enum's members do: a host lists them in their own
            // sense (severity, workflow), which an alphabetical sort would break.
            ? declared.Where(value => !string.IsNullOrEmpty(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : DerivedFilterValues(column);

    /// <summary>
    /// A column reading an enum offers every member, in declaration order, whatever rows are held:
    /// a remote grid only holds a page, and a member absent from it is still a valid choice.
    /// </summary>
    private IReadOnlyList<string> DerivedFilterValues(OmniDataGridColumnDefinition<TItem> column) => column.EnumType is { } enumType
        ? Enum.GetNames(enumType)
        : grid.Items
        .Select(item => column.Value(item)?.ToString())
        .Where(value => !string.IsNullOrEmpty(value))
        .Select(value => value!)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    internal Task FilterValueChangedAsync(OmniDataGridColumnDefinition<TItem> column, string value) =>
        StageAsync(column, DraftOf(column) with { Value = value });

    internal Task SecondFilterValueChangedAsync(OmniDataGridColumnDefinition<TItem> column, string value) =>
        StageAsync(column, DraftOf(column) with { SecondValue = value });

    internal Task FilterOperatorChangedAsync(OmniDataGridColumnDefinition<TItem> column, string? value) =>
        StageAsync(column, DraftOf(column) with { Operator = GridFilterOperators<TItem>.Parse(value, column.FilterOperator) });

    internal Task SecondFilterOperatorChangedAsync(OmniDataGridColumnDefinition<TItem> column, string? value) =>
        StageAsync(column, DraftOf(column) with
        {
            SecondOperator = GridFilterOperators<TItem>.Parse(value, GridFilterOperators<TItem>.DefaultFilter(column).SecondOperator)
        });

    internal Task LogicalOperatorChangedAsync(OmniDataGridColumnDefinition<TItem> column, string? value) =>
        StageAsync(column, DraftOf(column) with
        {
            LogicalOperator = Enum.TryParse<OmniDataGridLogicalOperator>(value, out var parsed) ? parsed : OmniDataGridLogicalOperator.And
        });

    /// <summary>
    /// Stores a pending filter change. In advanced mode it waits for the explicit apply action; in
    /// the other modes it applies immediately.
    /// </summary>
    private Task StageAsync(OmniDataGridColumnDefinition<TItem> column, GridColumnFilter filter)
    {
        grid.Query.Stage(column, filter);
        return UsesAdvancedEditor(column) ? Task.CompletedTask : ApplyFilterAsync(column);
    }

    internal async Task ApplyFilterAsync(OmniDataGridColumnDefinition<TItem> column)
    {
        grid.Query.Commit(column);
        await grid.View.QueryChangedAsync();
    }

    internal async Task ClearFilterAsync(OmniDataGridColumnDefinition<TItem> column)
    {
        grid.Query.Clear(column);
        await grid.View.QueryChangedAsync();
    }
}
