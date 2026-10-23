using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The texts the script measures when the grid sizes its columns to their content
/// (<see cref="OmniDataGridColumnSizing.Content"/>): the distinct texts of every column the script
/// measures, over every loaded row, the virtualized rows off screen included, the same texts the fit to
/// content of a column measures. They are handed over when the loaded rows or the measured columns
/// change, never on a render that only scrolls or pages through the same rows, so a column keeps its width
/// from one page to the next. A value edited in place, in a row list that keeps its identity and its size,
/// is not seen until the rows change; the cells on screen are always measured as they are.
/// </summary>
internal sealed class GridContentTexts<TItem>(OmniDataGrid<TItem> grid)
{
    private object? _items;
    private int _count;
    private string? _columns;
    private Dictionary<string, string[]>? _sent;

    /// <summary>
    /// The texts by column key when they changed since the last call, otherwise null. Null as well, and
    /// forgotten, while the grid does not size to content, so a return to content sizing hands them over again.
    /// </summary>
    internal IReadOnlyDictionary<string, string[]>? TakeChanges()
    {
        if (!grid.SizesToContent)
        {
            (_items, _columns, _sent) = (null, null, null);
            return null;
        }

        var columns = grid.ColumnSet.VisibleColumns.Where(IsMeasuredFromTexts).ToList();
        var items = grid.View.LoadedItems();
        var count = items.TryGetNonEnumeratedCount(out var known) ? known : -1;
        var signature = string.Join('|', columns.Select(column => $"{column.Key}:{column.FormatString}"));
        if (ReferenceEquals(items, _items) && count == _count && signature == _columns)
        {
            return null;
        }

        (_items, _count, _columns) = (items, count, signature);

        // A page or a sort hands the grid another list of the same rows: the texts are then the same, and the
        // script keeps what it measured (a column does not move when the reader turns a page).
        var fresh = columns.ToDictionary(column => column.Key, column => grid.ColumnLayout.LoadedTexts(column) ?? [], StringComparer.Ordinal);
        if (_sent is not null && SameTexts(_sent, fresh))
        {
            return null;
        }

        _sent = fresh;
        return fresh;
    }

    private static bool SameTexts(Dictionary<string, string[]> sent, Dictionary<string, string[]> fresh) =>
        sent.Count == fresh.Count
        && fresh.All(pair => sent.TryGetValue(pair.Key, out var before)
            && before.Length == pair.Value.Length
            && before.ToHashSet(StringComparer.Ordinal).SetEquals(pair.Value));

    // A column the script measures: not one the reader sized, nor one that wraps or is cut, nor a template
    // (its rendered cells are measured instead, .NET does not know what a template shows).
    private bool IsMeasuredFromTexts(OmniDataGridColumnDefinition<TItem> column) =>
        column.Template is null
        && !column.Truncate
        && !column.Wrap
        && !grid.ColumnLayout.Widths.ContainsKey(column.Key);
}
