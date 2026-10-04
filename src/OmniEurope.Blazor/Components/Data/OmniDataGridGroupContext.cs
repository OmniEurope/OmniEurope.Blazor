namespace OmniEurope.Blazor.Components;

/// <summary>
/// One group of a grouped <see cref="OmniDataGrid{TItem}"/>, handed to a column's
/// <see cref="OmniDataGridColumn{TItem}.GroupFooterTemplate"/> for the footer row that closes the group: the
/// grouped value, the column grouped on, the nesting level and the rows of the group.
/// </summary>
/// <typeparam name="TItem">The type of the grid rows.</typeparam>
public sealed class OmniDataGridGroupContext<TItem>
{
    internal OmniDataGridGroupContext(string path, object? key, string columnKey, int level, IReadOnlyList<TItem> items)
    {
        Path = path;
        Key = key;
        ColumnKey = columnKey;
        Level = level;
        Items = items;
    }

    internal string Path { get; }

    /// <summary>The value the rows of the group share in the grouped column; null for the group of empty values.</summary>
    public object? Key { get; }

    /// <summary>The key of the column the group is made on.</summary>
    public string ColumnKey { get; }

    /// <summary>The nesting level of the group, 0 for the outermost grouping.</summary>
    public int Level { get; }

    /// <summary>
    /// The rows of the group in the current view, in display order, nested groups included: on a paged grid the
    /// rows of the page shown, as the group header counts them.
    /// </summary>
    public IReadOnlyList<TItem> Items { get; }
}
