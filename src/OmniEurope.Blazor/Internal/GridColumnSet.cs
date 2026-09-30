using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The columns of a grid: the ones its column components registered, the implicit single column of a
/// grid that declares none, and the visible set with the control columns that widen each row.
/// </summary>
internal sealed class GridColumnSet<TItem>(OmniDataGrid<TItem> grid)
{
    private readonly List<OmniDataGridColumnDefinition<TItem>> _columns = [];
    private OmniDataGridColumnDefinition<TItem>? _implicitColumn;
    private IReadOnlyList<OmniDataGridColumnDefinition<TItem>> _visibleColumns = Array.Empty<OmniDataGridColumnDefinition<TItem>>();
    private bool _hasEditing;
    private int _columnSpan;
    // The columns fragment whose adopted delegates already queued a render (see RenderAdoptedDelegates).
    private RenderFragment? _adoptedFor;

    /// <summary>The columns registered by the column components, in registration order.</summary>
    internal IReadOnlyList<OmniDataGridColumnDefinition<TItem>> Declared => _columns;

    /// <summary>The registered columns, or the implicit column when there is none.</summary>
    internal IReadOnlyList<OmniDataGridColumnDefinition<TItem>> EffectiveColumns =>
        _columns.Count == 0 ? [ImplicitColumn] : _columns;

    private OmniDataGridColumnDefinition<TItem> ImplicitColumn => _implicitColumn ??= new OmniDataGridColumnDefinition<TItem>
    {
        Key = "value",
        Title = grid.Text("GridValueColumn"),
        Value = item => item
    };

    internal IReadOnlyList<OmniDataGridColumnDefinition<TItem>> VisibleColumns => _visibleColumns;

    /// <summary>Whether the grid appends its own edit column.</summary>
    internal bool HasEditing => _hasEditing;

    /// <summary>Cells in a row, control columns included.</summary>
    internal int ColumnSpan => _columnSpan;

    internal bool ShowDetailColumn => grid.DetailTemplate is not null && grid.ShowExpandColumn;

    internal bool HasFrozenColumns => VisibleColumns.Any(column => column.Frozen);

    internal bool HasFooter => VisibleColumns.Any(column => column.FooterContent is not null);

    internal bool HasTopFooter => HasFooter && grid.FooterPosition is OmniDataGridPosition.Top or OmniDataGridPosition.TopAndBottom;

    internal bool HasBottomFooter => HasFooter && grid.FooterPosition is OmniDataGridPosition.Bottom or OmniDataGridPosition.TopAndBottom;

    /// <summary>The column of a key among the effective columns, or null.</summary>
    internal OmniDataGridColumnDefinition<TItem>? Find(string key) =>
        EffectiveColumns.FirstOrDefault(candidate => candidate.Key == key);

    /// <summary>The visible column of a key, or null.</summary>
    internal OmniDataGridColumnDefinition<TItem>? FindVisible(string key) =>
        VisibleColumns.FirstOrDefault(candidate => candidate.Key == key);

    /// <summary>Forgets the implicit column, so its localized title is read again.</summary>
    internal void ResetImplicitColumn() => _implicitColumn = null;

    /// <summary>Recomputes the visible columns, the edit column and the span of a row.</summary>
    internal void Refresh()
    {
        _visibleColumns = EffectiveColumns.Where(column => column.Visible).ToArray();
        _hasEditing = grid.ShowEditColumn && _visibleColumns.Any(column => column.EditTemplate is not null);
        _columnSpan = _visibleColumns.Count
            + (grid.SelectionMode == OmniDataGridSelectionMode.None ? 0 : 1)
            + (_hasEditing ? 1 : 0)
            + (ShowDetailColumn ? 1 : 0);
    }

    internal void Register(OmniDataGridColumnDefinition<TItem> definition)
    {
        // The render this registration asks for runs the columns fragment it came from: delegates
        // adopted during it are equivalent and need no render of their own.
        _adoptedFor = grid.Columns;
        var index = _columns.FindIndex(column => column.Key == definition.Key);
        if (index >= 0)
        {
            _columns[index] = definition;
        }
        else
        {
            _columns.Add(definition);
        }
        var sorted = grid.Query.ApplyInitialSort(definition);
        var filtered = grid.Query.ApplyDefaultFilter(definition);
        grid.View.InvalidateLocalProjection();
        grid.RebuildRenderSnapshot();
        if ((sorted || filtered) && grid.Load is not null && grid.View.FirstRequestReady)
        {
            // A column rendered after the first request (under a condition, or added later) brings a
            // default the rows on screen were not loaded with: they are loaded again with it.
            _ = grid.DispatchAsync(async () =>
            {
                await grid.ReloadAsync();
                grid.Render();
            });
            return;
        }

        _ = grid.RenderLaterAsync();
    }

    /// <summary>
    /// The grid renders its cells before its columns receive their parameters, so a column declared in a
    /// <c>@foreach</c> hands over the delegates of the new parent render only after the cells were drawn
    /// with the previous ones. One more render, queued in the same batch, draws them with the new ones.
    /// It is asked once per columns fragment: a parent render brings a new fragment, while the grid's
    /// own render runs the same one again, so the render it queues does not queue another.
    /// </summary>
    internal void RenderAdoptedDelegates()
    {
        if (ReferenceEquals(_adoptedFor, grid.Columns))
        {
            return;
        }

        _adoptedFor = grid.Columns;
        grid.Render();
    }

    internal void Unregister(string key)
    {
        _columns.RemoveAll(column => column.Key == key);
        grid.Query.Forget(key);
        grid.ColumnLayout.Forget(key);
        grid.View.InvalidateLocalProjection();
        grid.RebuildRenderSnapshot();
        _ = grid.DispatchAsync(async () =>
        {
            if (grid.Load is not null)
            {
                await grid.ReloadAsync();
            }

            grid.Render();
        });
    }
}
