using System.Globalization;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The virtual window of a virtualized grid: the scroll offset and viewport height the script
/// reports, the measured row heights and the range of rows (or slots) rendered between the spacers.
/// </summary>
internal sealed class GridVirtualViewport<TItem>(OmniDataGrid<TItem> grid)
{
    private readonly GridVirtualWindow _window = new();
    private GridVirtualRange _range;
    private double _scrollTop;
    private double _viewportHeight;
    private double? _cssRowEstimate;

    /// <summary>The fixed height of every row, or null when rows are measured.</summary>
    internal double? FixedHeight => grid.FixedRowHeight && grid.EstimatedRowHeight > 0d ? grid.EstimatedRowHeight : null;

    internal GridVirtualRange Range => _range;

    internal void Sync()
    {
        var estimate = FixedHeight ?? _cssRowEstimate ?? (grid.EstimatedRowHeight > 0d ? grid.EstimatedRowHeight : 40d);
        if (grid.View.StructuredVirtual)
        {
            var count = grid.Rows.Slots.Count;
            _window.Configure(count, estimate);
            if (grid.Rows.TakeSlotsRebuilt())
            {
                // A group opened or closed, a detail row appeared: the slots moved, so the heights
                // measured for the old ones no longer describe the new ones.
                _window.ResetMeasurements();
            }
        }
        else
        {
            _window.Configure(grid.View.TotalCount, estimate);
        }

        _range = _window.Compute(_scrollTop, _viewportHeight, grid.VirtualizationOverscanCount);
    }

    /// <summary>Forgets the slot layout after an expansion change and recomputes the window over the new one.</summary>
    internal void RefreshSlots()
    {
        grid.Rows.ForgetSlots();
        if (grid.View.StructuredVirtual)
        {
            Sync();
        }
    }

    internal void ResetMeasurements() => _window.ResetMeasurements();

    /// <summary>Back to the top with no measured row, after the query changed.</summary>
    internal void ResetScroll()
    {
        _scrollTop = 0d;
        _window.ResetMeasurements();
    }

    internal double OffsetOf(int index) => _window.OffsetOf(index);

    /// <summary>Takes what the script measured; true when the window has to move.</summary>
    internal bool ApplySnapshot(GridViewportSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return false;
        }

        var moved = false;
        if (snapshot.RowEstimate is > 0d && snapshot.RowEstimate != _cssRowEstimate)
        {
            _cssRowEstimate = snapshot.RowEstimate;
            moved = true;
        }
        if (Math.Abs(snapshot.ViewportHeight - _viewportHeight) > 0.5d)
        {
            _viewportHeight = snapshot.ViewportHeight;
            moved = true;
        }

        if (Math.Abs(snapshot.ScrollTop - _scrollTop) > 0.5d)
        {
            _scrollTop = snapshot.ScrollTop;
            moved = true;
        }

        if ((FixedHeight is not null && !grid.View.StructuredVirtual) || snapshot.Rows is null)
        {
            return moved;
        }

        foreach (var row in snapshot.Rows)
        {
            moved |= _window.Measure(row.Index, row.Height);
        }

        return moved;
    }

    /// <summary>Loads and renders the rows of the new window only when the window moved.</summary>
    internal async Task ViewportChangedAsync(double scrollTop, double viewportHeight)
    {
        _scrollTop = scrollTop;
        _viewportHeight = viewportHeight;
        var previous = _range;
        Sync();
        if (previous == _range)
        {
            return;
        }

        await grid.View.EnsureVirtualDataAsync();
        grid.Render();
    }

    internal string? RowCountAttribute() => grid.View.Virtualized
        ? grid.View.TotalCount.ToString(CultureInfo.InvariantCulture)
        : null;

    internal string? RowIndexAttribute(int rowIndex) => grid.View.Virtualized
        ? (rowIndex + 1).ToString(CultureInfo.InvariantCulture)
        : null;
}
