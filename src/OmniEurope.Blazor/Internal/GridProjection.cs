using System.Globalization;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

internal static class GridProjection<TItem>
{
    internal static GridProjectionResult<TItem> Create(
        IReadOnlyList<TItem> items,
        IReadOnlyList<OmniDataGridColumnDefinition<TItem>> columns,
        IReadOnlyDictionary<string, string> filters,
        IReadOnlyList<OmniDataGridSort> sorts,
        int page,
        int pageSize)
    {
        IEnumerable<(TItem Item, int Index)> query = items.Select((item, index) => (item, index));
        foreach (var filter in filters.Where(pair => !string.IsNullOrWhiteSpace(pair.Value)))
        {
            var column = columns.FirstOrDefault(candidate => candidate.Key == filter.Key);
            if (column is not null)
            {
                query = query.Where(entry => column.FilterPredicate?.Invoke(entry.Item, filter.Value)
                    ?? MatchesFilter(column.Value(entry.Item), filter.Value, column.FilterOperator));
            }
        }

        IOrderedEnumerable<(TItem Item, int Index)>? ordered = null;
        foreach (var sort in sorts)
        {
            var column = columns.FirstOrDefault(candidate => candidate.Key == sort.Key);
            if (column is null)
            {
                continue;
            }

            ordered = ordered is null
                ? (sort.Descending
                    ? query.OrderByDescending(entry => column.Value(entry.Item), GridObjectComparer.Instance)
                    : query.OrderBy(entry => column.Value(entry.Item), GridObjectComparer.Instance))
                : (sort.Descending
                    ? ordered.ThenByDescending(entry => column.Value(entry.Item), GridObjectComparer.Instance)
                    : ordered.ThenBy(entry => column.Value(entry.Item), GridObjectComparer.Instance));
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

    private static bool MatchesFilter(object? candidate, string filter, OmniDataGridFilterOperator filterOperator)
    {
        var text = candidate?.ToString() ?? string.Empty;
        return filterOperator switch
        {
            OmniDataGridFilterOperator.Equals => string.Equals(text, filter, StringComparison.OrdinalIgnoreCase),
            OmniDataGridFilterOperator.StartsWith => text.StartsWith(filter, StringComparison.OrdinalIgnoreCase),
            OmniDataGridFilterOperator.EndsWith => text.EndsWith(filter, StringComparison.OrdinalIgnoreCase),
            OmniDataGridFilterOperator.GreaterThan => CompareNumeric(text, filter) > 0,
            OmniDataGridFilterOperator.LessThan => CompareNumeric(text, filter) < 0,
            _ => text.Contains(filter, StringComparison.OrdinalIgnoreCase)
        };
    }

    private static int CompareNumeric(string left, string right) =>
        decimal.TryParse(left, NumberStyles.Any, CultureInfo.CurrentCulture, out var leftNumber)
        && decimal.TryParse(right, NumberStyles.Any, CultureInfo.CurrentCulture, out var rightNumber)
            ? leftNumber.CompareTo(rightNumber)
            : string.Compare(left, right, StringComparison.CurrentCultureIgnoreCase);

    private sealed class GridObjectComparer : IComparer<object?>
    {
        internal static GridObjectComparer Instance { get; } = new();

        public int Compare(object? left, object? right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left is null) return -1;
            if (right is null) return 1;
            if (left is IComparable comparable) return comparable.CompareTo(right);
            return string.Compare(left.ToString(), right.ToString(), StringComparison.CurrentCulture);
        }
    }
}
