using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// What <see cref="GridContentTexts{TItem}"/> hands the script: the texts by column key, and whether they are
/// added to the ones it holds (<see cref="Append"/>) or replace them.
/// </summary>
internal sealed record GridContentTextsUpdate(IReadOnlyDictionary<string, string[]> Texts, bool Append);

/// <summary>
/// The texts the script measures when the grid sizes its columns to their content
/// (<see cref="OmniDataGridColumnSizing.Content"/>): the distinct texts of every column the script
/// measures, over every loaded row, the virtualized rows off screen included, the same texts the fit to
/// content of a column measures. They are handed over when the loaded rows or the measured columns
/// change, never on a render that only scrolls or pages through the same rows, so a column keeps its width
/// from one page to the next. A value edited in place, in a row list that keeps its identity and its size,
/// is not seen until the rows change; the cells on screen are always measured as they are.
/// A virtualized grid that fetches blocks grows one row collection: each block then hands over only the
/// texts the script does not hold yet, to be added to its own, instead of the whole set again, whose size
/// grows with every block fetched (a scroll through the cache would send it over and over).
/// </summary>
internal sealed class GridContentTexts<TItem>(OmniDataGrid<TItem> grid)
{
    /// <summary>
    /// Most texts of a column the script holds while blocks keep adding to them: twice the most a fit to
    /// content receives. Past it (rows scrolled out of the cache leave their texts behind), the current set
    /// replaces them, so the script's memory and the work to rank them stay bounded.
    /// </summary>
    internal const int MaximumKeptTexts = 2 * GridColumnLayout<TItem>.MaximumFitTexts;

    private object? _items;
    private int _count;
    private string? _columns;
    // The texts the script holds by column key, as handed over (replaced, then added to).
    private Dictionary<string, HashSet<string>>? _sent;

    /// <summary>
    /// The texts by column key when they changed since the last call, otherwise null. Null as well, and
    /// forgotten, while the grid does not size to content, so a return to content sizing hands them over again.
    /// </summary>
    internal GridContentTextsUpdate? TakeChanges()
    {
        if (!grid.SizesToContent)
        {
            (_items, _columns, _sent) = (null, null, null);
            return null;
        }

        var columns = grid.ColumnSet.VisibleColumns.Where(IsMeasuredFromTexts).ToList();
        var items = grid.View.LoadedItems();
        // Every source of the loaded rows is a collection (a list, a page, the values of the block cache): a count
        // without enumerating. The fetched blocks grow inside the same collection, which the count catches.
        var count = items.Count();
        var signature = string.Join('|', columns.Select(column => $"{column.Key}:{column.FormatString}"));
        var sameRows = ReferenceEquals(items, _items) && signature == _columns;
        if (sameRows && count == _count)
        {
            return null;
        }

        // The same collection with more rows and the same columns: blocks fetched into the cache.
        var grown = sameRows && count > _count;
        (_items, _count, _columns) = (items, count, signature);

        var fresh = columns.ToDictionary(column => column.Key, column => grid.ColumnLayout.LoadedTexts(column) ?? [], StringComparer.Ordinal);
        if (grown && NewTexts(fresh) is { } added)
        {
            return added.Count == 0 ? null : new GridContentTextsUpdate(added, true);
        }

        // A page or a sort hands the grid another list of the same rows: the texts are then the same, and the
        // script keeps what it measured (a column does not move when the reader turns a page).
        if (_sent is not null && SameTexts(_sent, fresh))
        {
            return null;
        }

        _sent = fresh.ToDictionary(pair => pair.Key, pair => new HashSet<string>(pair.Value, StringComparer.Ordinal), StringComparer.Ordinal);
        return new GridContentTextsUpdate(fresh, false);
    }

    // The texts of the grown rows the script does not hold yet, by column key (only the columns that gained
    // some), remembered as held. Null when a column would hold more than the bound: the caller replaces them.
    private Dictionary<string, string[]>? NewTexts(Dictionary<string, string[]> fresh)
    {
        var added = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var (key, texts) in fresh)
        {
            var held = _sent![key];
            var news = texts.Where(text => !held.Contains(text)).ToArray();
            if (held.Count + news.Length > MaximumKeptTexts)
            {
                return null;
            }

            if (news.Length > 0)
            {
                added[key] = news;
            }
        }

        foreach (var (key, news) in added)
        {
            _sent![key].UnionWith(news);
        }

        return added;
    }

    private static bool SameTexts(Dictionary<string, HashSet<string>> sent, Dictionary<string, string[]> fresh) =>
        sent.Count == fresh.Count
        && fresh.All(pair => sent.TryGetValue(pair.Key, out var before)
            && before.Count == pair.Value.Length
            && before.SetEquals(pair.Value));

    // A column the script measures: not one the reader sized, nor one that wraps or is cut, nor a template
    // (its rendered cells are measured instead, .NET does not know what a template shows).
    private bool IsMeasuredFromTexts(OmniDataGridColumnDefinition<TItem> column) =>
        column.Template is null
        && !column.Truncate
        && !column.Wrap
        && !grid.ColumnLayout.Widths.ContainsKey(column.Key);
}
