using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The sizes a grid pushes to its script: the table height, floor and ceiling, the column widths and
/// frozen offsets, and the fixed row height, each sent again only when it changed.
/// </summary>
internal sealed class GridLayoutInterop<TItem>(OmniDataGrid<TItem> grid)
{
    private string? _appliedHeight;
    private string? _appliedMaxHeight;
    private string? _appliedColumnLayout;
    private double? _appliedRowHeight;

    /// <summary>Minimum viewport height pushed to CSS, only meaningful while filling.</summary>
    private string? EffectiveMinHeight => grid.FillAvailableHeight && !string.IsNullOrWhiteSpace(grid.MinHeight)
        ? grid.MinHeight
        : null;

    /// <summary>Both height inputs in one value, so a change to either re-runs the layout interop.</summary>
    private string HeightSignature => $"{grid.Height}|{EffectiveMinHeight}";

    /// <summary>The ceiling pushed to CSS: only when no other height input already sizes the table.</summary>
    internal string? EffectiveMaxHeight => string.IsNullOrWhiteSpace(grid.MaxHeight) || grid.Height is not null || grid.FillAvailableHeight
        ? null
        : grid.MaxHeight.Trim();

    /// <summary>A resized column: the widths are sent again on the next layout pass.</summary>
    internal void InvalidateColumnLayout() => _appliedColumnLayout = null;

    /// <summary>
    /// Pushes the ceiling on its own call, only once one was asked for, so a grid without one runs
    /// exactly the interop it ran before the parameter existed.
    /// </summary>
    internal async Task ApplyMaxHeightAsync()
    {
        var maxHeight = EffectiveMaxHeight;
        if (string.Equals(maxHeight, _appliedMaxHeight, StringComparison.Ordinal))
        {
            return;
        }

        var module = await grid.Script.ModuleAsync();
        await module.InvokeVoidAsync("applyMaxHeight", grid.Viewport, maxHeight);
        _appliedMaxHeight = maxHeight;
    }

    /// <summary>Applies the table height and the column widths outside the virtualized path.</summary>
    internal async Task ApplyLayoutAsync()
    {
        var signature = grid.ColumnLayout.Signature();
        if (!RequiresLayoutInterop && _appliedHeight is null && _appliedColumnLayout is null && _appliedRowHeight is null)
        {
            return;
        }

        var rowHeight = grid.Virtual.FixedHeight;
        if (HeightSignature == _appliedHeight && signature == _appliedColumnLayout && rowHeight == _appliedRowHeight)
        {
            await ApplyFrozenOffsetsAsync();
            return;
        }

        var module = await grid.Script.ModuleAsync();
        await module.InvokeVoidAsync("applyLayout", grid.Viewport, 0d, 0d, grid.Height, EffectiveMinHeight);
        _appliedHeight = HeightSignature;
        await ApplyColumnLayoutAsync();
        await ApplyRowHeightAsync(rowHeight);
    }

    /// <summary>Applies the table height with the spacers of the virtual window around its rows.</summary>
    internal async Task ApplyVirtualLayoutAsync(IJSObjectReference module)
    {
        var range = grid.Virtual.Range;
        await module.InvokeVoidAsync("applyLayout", grid.Viewport, range.TopSpacer, range.BottomSpacer, grid.Height, EffectiveMinHeight);
        _appliedHeight = HeightSignature;
    }

    /// <summary>
    /// Re-anchors the frozen cells after a render that kept the column layout: the rows of a new
    /// page, a sort or a filter are new cells, which the offsets set on the previous ones do not
    /// reach, and a second frozen column would otherwise slide over the first.
    /// </summary>
    private async Task ApplyFrozenOffsetsAsync()
    {
        if (!grid.ColumnSet.HasFrozenColumns || grid.Script.Module is not { } module)
        {
            return;
        }

        await module.InvokeVoidAsync("applyFrozen", grid.Viewport);
    }

    internal async Task ApplyRowHeightAsync(double? rowHeight)
    {
        if (rowHeight == _appliedRowHeight)
        {
            return;
        }

        _appliedRowHeight = rowHeight;
        var module = await grid.Script.ModuleAsync();
        await module.InvokeVoidAsync("applyRowHeight", grid.Viewport, rowHeight);
    }

    /// <summary>
    /// A plain grid with no explicit height and no column sizing needs no script at all, so the
    /// module is only imported once something actually has to be measured or positioned.
    /// </summary>
    private bool RequiresLayoutInterop => grid.Height is not null
        || grid.FillAvailableHeight
        || grid.ColumnLayout.Widths.Count > 0
        || grid.Virtual.FixedHeight is not null
        || grid.ColumnSet.VisibleColumns.Any(column => column.Width is not null || column.MinWidth is not null || column.Frozen);

    internal async Task ApplyColumnLayoutAsync()
    {
        var signature = grid.ColumnLayout.Signature();
        if (grid.Script.Module is not { } module)
        {
            return;
        }

        if (signature == _appliedColumnLayout)
        {
            await ApplyFrozenOffsetsAsync();
            return;
        }

        _appliedColumnLayout = signature;
        await module.InvokeVoidAsync("applyColumns", grid.Viewport, grid.ColumnLayout.Specs(), grid.ColumnLayout.TableMinimumWidth());
    }
}
