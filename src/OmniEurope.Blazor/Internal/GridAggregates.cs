using System.Globalization;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The figures of the columns that declare an <see cref="OmniDataGridAggregate"/>: computed over every row the
/// grid holds for its footer row, over the rows of a group for that group's footer row, and written with the
/// column's aggregate format.
/// </summary>
internal sealed class GridAggregates<TItem>(OmniDataGrid<TItem> grid)
{
    /// <summary>The text of a column's cell in the grid footer: its aggregate over the rows the grid holds.</summary>
    internal string FooterText(OmniDataGridColumnDefinition<TItem> column) =>
        Format(column, column.AggregateFormat, Compute(grid.Tree.Active ? grid.Tree.Roots : grid.View.LoadedItems(), column.Value, column.Aggregate));

    /// <summary>The text of a column's cell in a group footer: its aggregate over the rows of the group.</summary>
    internal string GroupText(OmniDataGridColumnDefinition<TItem> column, OmniDataGridGroupContext<TItem> group) =>
        Format(column, column.GroupAggregateFormat ?? column.AggregateFormat, Compute(group.Items, column.Value, column.Aggregate));

    private static string Format(OmniDataGridColumnDefinition<TItem> column, string? format, object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var pattern = format ?? column.FormatString;
        return string.IsNullOrWhiteSpace(pattern)
            ? Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty
            : string.Format(CultureInfo.CurrentCulture, pattern, value);
    }

    /// <summary>
    /// The aggregate of the values read from <paramref name="items"/>. Sum and Average add the numeric values,
    /// in <see cref="decimal"/> unless a value is a <see cref="double"/> or a <see cref="float"/>; Min and Max
    /// compare values of one type; Count counts the rows. Null means nothing to show.
    /// </summary>
    internal static object? Compute(IEnumerable<TItem> items, Func<TItem, object?> read, OmniDataGridAggregate aggregate)
    {
        if (aggregate == OmniDataGridAggregate.Count)
        {
            return items.Count();
        }

        var values = items.Select(read).Where(value => value is not null).Select(value => value!).ToList();
        switch (aggregate)
        {
            case OmniDataGridAggregate.Sum:
            case OmniDataGridAggregate.Average:
                var numbers = values.Where(IsNumber).ToList();
                if (aggregate == OmniDataGridAggregate.Average && numbers.Count == 0)
                {
                    return null;
                }

                if (numbers.Any(value => value is double or float))
                {
                    var total = numbers.Sum(value => Convert.ToDouble(value, CultureInfo.InvariantCulture));
                    return aggregate == OmniDataGridAggregate.Sum ? total : total / numbers.Count;
                }

                var sum = numbers.Sum(value => Convert.ToDecimal(value, CultureInfo.InvariantCulture));
                return aggregate == OmniDataGridAggregate.Sum ? sum : sum / numbers.Count;
            case OmniDataGridAggregate.Min:
            case OmniDataGridAggregate.Max:
                var comparable = values.OfType<IComparable>().Where(value => value.GetType() == values[0].GetType()).ToList();
                if (comparable.Count == 0)
                {
                    return null;
                }

                var best = comparable[0];
                foreach (var value in comparable)
                {
                    var order = value.CompareTo(best);
                    if (aggregate == OmniDataGridAggregate.Min ? order < 0 : order > 0)
                    {
                        best = value;
                    }
                }

                return best;
            default:
                return null;
        }
    }

    private static bool IsNumber(object value) =>
        value is byte or sbyte or short or ushort or int or uint or long or ulong or decimal or double or float;
}
