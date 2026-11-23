using System.Globalization;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The virtual window of a virtualized grid: the scroll offset and viewport height the script
/// reports, the measured row heights and the range of rows (or slots) rendered between the spacers.
/// </summary>
internal sealed class GridVirtualViewport<TItem>(IGridHost<TItem> grid) : IGridVirtualViewport
{
    private readonly GridVirtualWindow _window = new();
    private GridVirtualRange _range;
    private double _scrollTop;
    private double _viewportHeight;
    private double? _cssRowEstimate;

    /// <summary>
    /// Whether every row has one exact height, which rows are then never measured for: the height the
    /// consumer gave, or, when none, the row height of the density, which the stylesheet draws.
    /// </summary>
    public bool RowsAreFixed => grid.FixedRowHeight && grid.EstimatedRowHeight is null or > 0d;

    /// <summary>The height the consumer imposed on every row, or null: measured rows, or the density's row height.</summary>
    public double? FixedHeight => RowsAreFixed ? grid.EstimatedRowHeight : null;

    public GridVirtualRange Range => _range;

    /// <summary>
    /// The heights of the spacers as the render writes them, invariant: they reach the page in the same
    /// batch as the rows, where the script's would come one round trip later and show the rows out of place.
    /// </summary>
    internal string TopSpacerHeight => SpacerHeight(_range.TopSpacer);

    internal string BottomSpacerHeight => SpacerHeight(_range.BottomSpacer);

    private static string SpacerHeight(double height) => Math.Max(0d, height).ToString(CultureInfo.InvariantCulture);

    public void Sync()
    {
        var estimate = FixedHeight ?? _cssRowEstimate ?? (grid.EstimatedRowHeight is > 0d ? grid.EstimatedRowHeight.Value : 40d);
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

    /// <summary>
    /// Forgets the slot layout after an expansion change and recomputes the window over the new one. The
    /// rows of a tree that is not slotted move to other indexes when a branch opens or closes, so the
    /// heights measured by index no longer describe them.
    /// </summary>
    public void RefreshSlots()
    {
        grid.Rows.ForgetSlots();
        if (!grid.View.Virtualized)
        {
            return;
        }

        if (!grid.View.StructuredVirtual)
        {
            _window.ResetMeasurements();
        }

        Sync();
    }

    public void ResetMeasurements() => _window.ResetMeasurements();

    /// <summary>Back to the top with no measured row, after the query changed.</summary>
    public void ResetScroll()
    {
        _scrollTop = 0d;
        _window.ResetMeasurements();
    }

    internal double OffsetOf(int index) => _window.OffsetOf(index);

    /// <summary>Takes what the script measured; true when the window has to move.</summary>
    public bool ApplySnapshot(GridViewportSnapshot? snapshot)
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
        moved |= GridVirtualWindow.TakeMeasured(snapshot.ViewportHeight, ref _viewportHeight);
        moved |= GridVirtualWindow.TakeMeasured(snapshot.ScrollTop, ref _scrollTop);

        if ((RowsAreFixed && !grid.View.StructuredVirtual) || snapshot.Rows is null)
        {
            return moved;
        }

        return _window.MeasureAll(snapshot.Rows) | moved;
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
