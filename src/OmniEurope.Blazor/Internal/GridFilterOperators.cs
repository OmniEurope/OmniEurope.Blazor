using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The filter operators a grid column offers, from the type it reads and the shape of its filter, and
/// the empty filter a column starts from.
/// </summary>
internal static class GridFilterOperators<TItem>
{
    private static IReadOnlyList<OmniDataGridFilterOperator> TextOperators { get; } =
    [
        OmniDataGridFilterOperator.Contains, OmniDataGridFilterOperator.DoesNotContain,
        OmniDataGridFilterOperator.Equals, OmniDataGridFilterOperator.NotEquals,
        OmniDataGridFilterOperator.StartsWith, OmniDataGridFilterOperator.EndsWith,
        OmniDataGridFilterOperator.IsNull, OmniDataGridFilterOperator.IsNotNull,
        OmniDataGridFilterOperator.IsEmpty, OmniDataGridFilterOperator.IsNotEmpty
    ];

    private static IReadOnlyList<OmniDataGridFilterOperator> OrderedOperators { get; } =
    [
        OmniDataGridFilterOperator.Equals, OmniDataGridFilterOperator.NotEquals,
        OmniDataGridFilterOperator.GreaterThan, OmniDataGridFilterOperator.GreaterThanOrEquals,
        OmniDataGridFilterOperator.LessThan, OmniDataGridFilterOperator.LessThanOrEquals,
        OmniDataGridFilterOperator.IsNull, OmniDataGridFilterOperator.IsNotNull
    ];

    /// <summary>
    /// A declared number filter reads the figure as shown: "contains" finds 12 in 112 and 120, which is how
    /// a list numbered on screen is searched, and it comes first so an untouched filter never shows an
    /// empty operator. The ordered comparisons follow. Declared after <see cref="OrderedOperators"/>,
    /// which static initialization reads in textual order.
    /// </summary>
    private static IReadOnlyList<OmniDataGridFilterOperator> NumberOperators { get; } =
        [OmniDataGridFilterOperator.Contains, .. OrderedOperators];

    private static IReadOnlyList<OmniDataGridFilterOperator> EqualityOperators { get; } =
    [
        OmniDataGridFilterOperator.Equals, OmniDataGridFilterOperator.NotEquals,
        OmniDataGridFilterOperator.IsNull, OmniDataGridFilterOperator.IsNotNull
    ];

    /// <summary>
    /// The operators a column's condition can use, from the type it reads: text compares as text, a
    /// number or a date is ordered, an enum or a boolean is only equal or not. Offering "contains" on
    /// a number, or "greater than" on a name, only let the user build a condition that a remote
    /// loader must refuse. A column read through a function has no known type and keeps the text set;
    /// a declared Number filter takes <see cref="NumberOperators"/>, the figure as shown included.
    /// The multi-valued operators belong to the checkable list, never to this menu.
    /// </summary>
    internal static IReadOnlyList<OmniDataGridFilterOperator> OperatorsFor(OmniDataGridColumnDefinition<TItem> column)
    {
        var allowed = column.FilterType == OmniDataGridColumnFilterType.Number ? NumberOperators : OperatorsForType(column);
        if (column.FilterOperators is not { Count: > 0 } chosen)
        {
            return allowed;
        }

        // The column's own list narrows the set and orders it; an operator its type cannot use is
        // dropped rather than offered, and a list left empty by that falls back to the whole set.
        var narrowed = chosen.Distinct().Where(allowed.Contains).ToArray();
        return narrowed.Length > 0 ? narrowed : allowed;
    }

    private static IReadOnlyList<OmniDataGridFilterOperator> OperatorsForType(OmniDataGridColumnDefinition<TItem> column) =>
        column.ValueType switch
        {
            null => TextOperators,
            var type when type == typeof(string) => TextOperators,
            var type when type.IsEnum || type == typeof(bool) || type == typeof(Guid) => EqualityOperators,
            var type when column.Numeric || type == typeof(DateTime) || type == typeof(DateTimeOffset)
                || type == typeof(DateOnly) || type == typeof(TimeOnly) || type == typeof(TimeSpan) => OrderedOperators,
            _ => TextOperators
        };

    /// <summary>A column's declared operator when its type offers it, else the first one it does offer.</summary>
    internal static OmniDataGridFilterOperator Offered(OmniDataGridColumnDefinition<TItem> column, OmniDataGridFilterOperator declared)
    {
        var offered = OperatorsFor(column);
        return offered.Contains(declared) ? declared : offered[0];
    }

    /// <summary>The empty filter a column starts from, with the operator its filter shape implies.</summary>
    internal static GridColumnFilter DefaultFilter(OmniDataGridColumnDefinition<TItem> column) => new(
        DefaultOperator(column),
        string.Empty,
        OmniDataGridLogicalOperator.And,
        column.FilterType == OmniDataGridColumnFilterType.Text ? Offered(column, default) : default,
        string.Empty);

    /// <summary>
    /// The operator a filter shape implies. A closed dropdown means equality and a checkable list
    /// means membership, whatever the column's text-oriented default says; only the shapes that
    /// really are free text keep it.
    /// </summary>
    private static OmniDataGridFilterOperator DefaultOperator(OmniDataGridColumnDefinition<TItem> column) => column.FilterType switch
    {
        OmniDataGridColumnFilterType.Select or OmniDataGridColumnFilterType.DateRange => OmniDataGridFilterOperator.Equals,
        OmniDataGridColumnFilterType.MultiSelect => OmniDataGridFilterOperator.In,
        OmniDataGridColumnFilterType.Text or OmniDataGridColumnFilterType.Number => Offered(column, column.FilterOperator),
        _ => column.FilterOperator
    };

    /// <summary>The operator named by <paramref name="value"/>, or <paramref name="fallback"/> when it names none.</summary>
    internal static OmniDataGridFilterOperator Parse(string? value, OmniDataGridFilterOperator fallback) =>
        Enum.TryParse<OmniDataGridFilterOperator>(value, out var parsed) ? parsed : fallback;
}
