
namespace OmniEurope.Blazor.Components;

/// <summary>
/// The detach cycle of a grid's frozen columns (docs/data-components.md): offered once rows pass
/// under them, detached by the reader, frozen again when the viewport comes back to its start.
/// </summary>
internal sealed class GridFrozenState<TItem>(IGridHost<TItem> grid) : IGridFrozenState
{
    private bool _horizontallyScrolled;
    private bool _frozenDetached;

    public bool HorizontallyScrolled => _horizontallyScrolled;

    /// <summary>The detach control is offered only while frozen columns have rows passing under them.</summary>
    internal bool ShowsFrozenToggle => grid.ColumnSet.HasFrozenColumns && _horizontallyScrolled;

    /// <summary>Whether the frozen columns currently scroll with the rest of the row.</summary>
    public bool FrozenDetached => ShowsFrozenToggle && _frozenDetached;

    /// <summary>Columns that stop being frozen end a detachment, so freezing one again starts frozen.</summary>
    public void EndDetachmentWithoutFrozenColumns()
    {
        if (!grid.ColumnSet.HasFrozenColumns)
        {
            _frozenDetached = false;
        }
    }

    /// <summary>Coming back to the horizontal start ends a detachment.</summary>
    public void ApplyHorizontalScroll(bool scrolled)
    {
        _horizontallyScrolled = scrolled;
        if (!scrolled)
        {
            _frozenDetached = false;
        }
    }

    /// <summary>
    /// Detaches the frozen columns, or freezes them again. A detachment never exists at the start of
    /// the table, so the control does nothing there (it is hidden anyway).
    /// </summary>
    internal void ToggleFrozenDetached()
    {
        if (!ShowsFrozenToggle)
        {
            return;
        }

        _frozenDetached = !_frozenDetached;
    }

    /// <summary>The script reports the viewport leaving or reaching its horizontal start.</summary>
    internal Task HorizontalScrollChangedAsync(bool scrolled)
    {
        if (scrolled != _horizontallyScrolled)
        {
            ApplyHorizontalScroll(scrolled);
            grid.Render();
        }

        return Task.CompletedTask;
    }
}
