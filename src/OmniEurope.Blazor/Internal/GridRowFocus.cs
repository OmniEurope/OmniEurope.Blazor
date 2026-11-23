using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The single tab stop of the rows a host made interactive (a roving tabindex): the row that last took
/// the focus, otherwise the first one on screen, carries <c>tabindex="0"</c> and the others
/// <c>tabindex="-1"</c>, so Tab crosses the rows in one stop. The arrows, Home and End move the focus
/// between the rows in the page script (<c>omni-focus.js</c>, rows marked <c>data-omni-row-nav</c>), which
/// is the only place that sees which element a key was pressed on; the focus it moves reaches this class
/// through <see cref="FocusInCallback"/>, which turns the row it landed on into the tab stop.
/// </summary>
internal sealed class GridRowFocus<TItem>(IGridHost<TItem> grid)
{
    // The key of the row that last took the focus; it stays the tab stop for as long as it is on screen.
    private object? _tabStopKey;

    /// <summary>Whether the row can take the focus: the host listens to rows and lets this one be used.</summary>
    internal bool IsNavigable(GridRenderRow<TItem> row) => row.HasItem && row.Index >= 0 && row.Selectable && grid.Selection.RowsAreInteractive;

    /// <summary>The key of the row that carries the tab stop among the rows about to render, or null when none can take it.</summary>
    internal object? TabStopKey(IReadOnlyList<GridRenderRow<TItem>> rows)
    {
        object? first = null;
        foreach (var row in rows.Where(IsNavigable))
        {
            var key = grid.ItemKey(row.Item);
            if (_tabStopKey is not null && Equals(key, _tabStopKey))
            {
                return key;
            }

            first ??= key;
        }

        return first;
    }

    /// <summary>The <c>tabindex</c> of a row: 0 for the tab stop, -1 for the other rows that can take the focus, none for the rest.</summary>
    internal string? TabIndex(GridRenderRow<TItem> row, object? tabStopKey) => !IsNavigable(row)
        ? null
        : Equals(grid.ItemKey(row.Item), tabStopKey) ? "0" : "-1";

    /// <summary>The focus entering a row, or a control inside it: that row becomes the tab stop. Renders only when it changes.</summary>
    internal EventCallback<FocusEventArgs> FocusInCallback(GridRenderRow<TItem> row) => IsNavigable(row)
        ? EventCallback.Factory.Create<FocusEventArgs>(this, () => TakeTabStopAsync(grid.ItemKey(row.Item)))
        : default;

    private async Task TakeTabStopAsync(object key)
    {
        if (!Equals(key, _tabStopKey))
        {
            _tabStopKey = key;
            await grid.RenderLaterAsync();
        }

        // The page keys come with the first focus, as for the other components that use them.
        await grid.Script.EnsureRowKeysAsync();
    }
}
