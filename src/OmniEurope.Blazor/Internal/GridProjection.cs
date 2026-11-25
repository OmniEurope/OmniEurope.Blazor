using System.Globalization;

namespace OmniEurope.Blazor.Components;

internal static class GridProjection<TItem>
{
    internal static GridProjectionResult<TItem> Create(
        IReadOnlyList<TItem> items,
        IReadOnlyList<OmniDataGridColumnDefinition<TItem>> columns,
        IReadOnlyDictionary<string, GridColumnFilter> filters,
        IReadOnlyList<OmniDataGridSort> sorts,
        bool caseSensitive,
        bool ignoreDiacritics,
        int page,
        int pageSize)
    {
        IEnumerable<(TItem Item, int Index)> query = items.Select((item, index) => (item, index));
        var comparison = OmniDataGridFilterText.Comparison(caseSensitive);

        foreach (var filter in filters.Where(pair => pair.Value.IsActive))
        {
            var column = columns.FirstOrDefault(candidate => candidate.Key == filter.Key);
            if (column is null)
            {
                continue;
            }

            var state = filter.Value;
            query = query.Where(entry => Matches(column, entry.Item, state, comparison, ignoreDiacritics));
        }

        IOrderedEnumerable<(TItem Item, int Index)>? ordered = null;
        foreach (var sort in sorts)
        {
            var column = columns.FirstOrDefault(candidate => candidate.Key == sort.Key);
            if (column is null)
            {
                continue;
            }

            var accessor = column.SortValue ?? column.Value;
            ordered = ordered is null
                ? (sort.Descending
                    ? query.OrderByDescending(entry => accessor(entry.Item), GridObjectComparer.Instance)
                    : query.OrderBy(entry => accessor(entry.Item), GridObjectComparer.Instance))
                : (sort.Descending
                    ? ordered.ThenByDescending(entry => accessor(entry.Item), GridObjectComparer.Instance)
                    : ordered.ThenBy(entry => accessor(entry.Item), GridObjectComparer.Instance));
        }

        if (ordered is not null)
        {
            query = ordered.ThenBy(entry => entry.Index);
        }

        var filtered = query.Select(entry => entry.Item).ToArray();
        var effectiveSize = Math.Max(1, pageSize);
        var pageCount = Math.Max(1, (int)Math.Ceiling(filtered.Length / (double)effectiveSize));
        var effectivePage = Math.Clamp(page, 1, pageCount);
        var visible = filtered.Skip((effectivePage - 1) * effectiveSize).Take(effectiveSize).ToArray();
        return new GridProjectionResult<TItem>(visible, filtered.Length);
    }

    private static bool Matches(
        OmniDataGridColumnDefinition<TItem> column,
        TItem item,
        GridColumnFilter filter,
        StringComparison comparison,
        bool ignoreDiacritics)
    {
        if (column.FilterType is OmniDataGridColumnFilterType.DateRange or OmniDataGridColumnFilterType.Date && column.FilterPredicate is null)
        {
            return OmniDataGridDateRange.Contains(filter.Value, column.Value(item));
        }

        var first = !filter.HasFirst
            || (column.FilterPredicate?.Invoke(item, filter.Value)
                ?? MatchesFilter(column.Value(item), filter.Value, filter.Operator, comparison, ignoreDiacritics));
        if (!filter.HasSecond)
        {
            return first;
        }

        var second = column.FilterPredicate?.Invoke(item, filter.SecondValue)
            ?? MatchesFilter(column.Value(item), filter.SecondValue, filter.SecondOperator, comparison, ignoreDiacritics);
        return filter.LogicalOperator == OmniDataGridLogicalOperator.Or
            ? (filter.HasFirst && first) || second
            : first && second;
    }

    internal static bool MatchesFilter(
        object? candidate,
        string filter,
        OmniDataGridFilterOperator filterOperator,
        StringComparison comparison,
        bool ignoreDiacritics = false)
    {
        var rawText = candidate?.ToString() ?? string.Empty;

        // A multi-valued operator is answered before normalization so each candidate is compared
        // with the same equality rules as a single Equals filter.
        if (OmniDataGridFilterValues.IsMultiValued(filterOperator))
        {
            return MatchesAnyValue(rawText, filter, filterOperator, comparison, ignoreDiacritics);
        }

        var text = OmniDataGridFilterText.Normalize(rawText, ignoreDiacritics);
        filter = OmniDataGridFilterText.Normalize(filter, ignoreDiacritics);
        return MatchesPresence(candidate, text, filterOperator)
            ?? MatchesOrder(text, filter, filterOperator, comparison)
            ?? MatchesText(text, filter, filterOperator, comparison);
    }

    /// <summary>The In and NotIn operators: whether the text equals one of the listed values.</summary>
    private static bool MatchesAnyValue(
        string rawText,
        string filter,
        OmniDataGridFilterOperator filterOperator,
        StringComparison comparison,
        bool ignoreDiacritics)
    {
        var wanted = OmniDataGridFilterValues.Split(filter);
        var normalizedText = OmniDataGridFilterText.Normalize(rawText, ignoreDiacritics);
        var any = wanted.Any(value => string.Equals(
            normalizedText,
            OmniDataGridFilterText.Normalize(value, ignoreDiacritics),
            comparison));
        return filterOperator == OmniDataGridFilterOperator.In ? any : !any;
    }

    /// <summary>The null and empty operators; null for any other operator.</summary>
    private static bool? MatchesPresence(object? candidate, string text, OmniDataGridFilterOperator filterOperator) =>
        filterOperator switch
        {
            OmniDataGridFilterOperator.IsNull => candidate is null,
            OmniDataGridFilterOperator.IsNotNull => candidate is not null,
            OmniDataGridFilterOperator.IsEmpty => text.Length == 0,
            OmniDataGridFilterOperator.IsNotEmpty => text.Length != 0,
            _ => null
        };

    /// <summary>The ordering operators, numeric when both sides are numbers; null for any other operator.</summary>
    private static bool? MatchesOrder(
        string text, string filter, OmniDataGridFilterOperator filterOperator, StringComparison comparison) =>
        filterOperator switch
        {
            OmniDataGridFilterOperator.GreaterThan => Compare(text, filter, comparison) > 0,
            OmniDataGridFilterOperator.GreaterThanOrEquals => Compare(text, filter, comparison) >= 0,
            OmniDataGridFilterOperator.LessThan => Compare(text, filter, comparison) < 0,
            OmniDataGridFilterOperator.LessThanOrEquals => Compare(text, filter, comparison) <= 0,
            _ => null
        };

    /// <summary>The text operators; an operator answered nowhere else means Contains.</summary>
    private static bool MatchesText(
        string text, string filter, OmniDataGridFilterOperator filterOperator, StringComparison comparison) =>
        filterOperator switch
        {
            OmniDataGridFilterOperator.Equals => string.Equals(text, filter, comparison),
            OmniDataGridFilterOperator.NotEquals => !string.Equals(text, filter, comparison),
            OmniDataGridFilterOperator.StartsWith => text.StartsWith(filter, comparison),
            OmniDataGridFilterOperator.EndsWith => text.EndsWith(filter, comparison),
            OmniDataGridFilterOperator.DoesNotContain => !text.Contains(filter, comparison),
            _ => text.Contains(filter, comparison)
        };

    private static int Compare(string left, string right, StringComparison comparison)
    {
        if (decimal.TryParse(left, NumberStyles.Any, CultureInfo.CurrentCulture, out var leftNumber)
            && decimal.TryParse(right, NumberStyles.Any, CultureInfo.CurrentCulture, out var rightNumber))
        {
            return leftNumber.CompareTo(rightNumber);
        }

        if (DateTimeOffset.TryParse(left, CultureInfo.CurrentCulture, out var leftDate)
            && DateTimeOffset.TryParse(right, CultureInfo.CurrentCulture, out var rightDate))
        {
            return leftDate.CompareTo(rightDate);
        }

        return string.Compare(left, right, comparison);
    }

    private sealed class GridObjectComparer : IComparer<object?>
    {
        internal static GridObjectComparer Instance { get; } = new();

        public int Compare(object? left, object? right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left is null) return -1;
            if (right is null) return 1;
            if (left is IComparable comparable && left.GetType() == right.GetType()) return comparable.CompareTo(right);
            return string.Compare(left.ToString(), right.ToString(), StringComparison.CurrentCulture);
        }
    }
}
