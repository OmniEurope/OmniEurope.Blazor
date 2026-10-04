using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>The classes a grid's markup puts on its root, viewport, rows, cells and controls.</summary>
internal sealed class GridCssClasses<TItem>(OmniDataGrid<TItem> grid)
{
    internal string LoadingBarClass => grid.LoadingBarMode == OmniLoadingBarMode.Continuous
        ? "omni-loading-bar omni-loading-bar--continuous omni-loading-bar--active omni-data-grid__loading-bar"
        : "omni-loading-bar omni-loading-bar--sweep omni-loading-bar--active omni-data-grid__loading-bar";

    /// <summary>The root classes, followed by the host's <c>Class</c>.</summary>
    internal string GridClass() => CssClassBuilder.Combine([
        "omni-data-grid",
        grid.Script.Veiled ? "omni-data-grid--preparing" : null,
        grid.FillAvailableHeight ? "omni-data-grid--fill" : null,
        grid.HighlightRowOnHover ? "omni-data-grid--row-hover" : null,
        grid.HighlightColumnOnHover ? "omni-data-grid--column-hover" : null,
        grid.AllowAlternatingRows ? "omni-data-grid--striped" : null,
        grid.View.Virtualized ? "omni-data-grid--virtual" : null,
        grid.Responsive ? "omni-data-grid--responsive" : null,
        grid.Virtual.FixedHeight is not null ? "omni-data-grid--fixed-row-height" : null,
        grid.HeaderWrap == OmniDataGridHeaderWrap.Truncate ? "omni-data-grid--header-truncate" : null,
        grid.ColumnSet.HasFrozenColumns ? "omni-data-grid--has-frozen" : null,
        grid.FrozenState.FrozenDetached ? "omni-data-grid--frozen-detached" : null,
        grid.GridLines == OmniDataGridLines.Horizontal ? null : $"omni-data-grid--lines-{grid.GridLines.ToString().ToLowerInvariant()}",
        grid.Class]);

    internal string ViewportClass() => CssClassBuilder.Combine([
        "omni-data-grid__viewport",
        grid.View.Virtualized ? "omni-data-grid__viewport--virtual" : null,
        grid.FillAvailableHeight ? "omni-data-grid__viewport--fill" : null,
        grid.Height is null ? null : "omni-data-grid__viewport--sized",
        grid.LayoutInterop.EffectiveMaxHeight is null ? null : "omni-data-grid__viewport--capped"
    ]);

    // A numeric column left at the default alignment takes the end, in figures of one width, so its
    // values compare at a glance; an alignment the consumer set (Center, End) is kept as is. A column
    // with its own Template is not the bare figure any more (a link, a label, buttons laid out from
    // the start), so it keeps the start: aligning only its header at the end split the two apart.
    private static bool IsNumericCell(OmniDataGridColumnDefinition<TItem> column) =>
        column.Numeric && column.Template is null && column.TextAlign == OmniDataGridTextAlign.Start;

    internal string ColumnClass(OmniDataGridColumnDefinition<TItem> column, bool header) => CssClassBuilder.Combine([
        IsNumericCell(column)
            ? "omni-data-grid__column--align-end omni-data-grid__cell--numeric"
            : $"omni-data-grid__column--align-{column.TextAlign.ToString().ToLowerInvariant()}",
        column.Frozen ? "omni-data-grid__column--frozen" : null,
        grid.HighlightActiveColumn && grid.Query.IsColumnActive(column) ? "omni-data-grid__column--active" : null,
        header && grid.Query.IsSortable(column) ? "omni-data-grid__column--sortable" : null,
        grid.HighlightColumnOnHover && !column.HighlightOnHover ? "omni-data-grid__column--no-hover" : null,
        header ? column.HeaderClass : column.Class
    ]);

    /// <summary>
    /// A body cell that shows the column's text (no template, or a row in edit without an edit
    /// template) keeps to one line and ends in an ellipsis instead of spilling into the next column.
    /// A templated cell stays unclipped, so its badges, buttons, menus and edit inputs keep their
    /// focus rings and popups; a column opts its template in through
    /// <c>Class="omni-data-grid__cell--text"</c>.
    /// </summary>
    internal string BodyCellClass(OmniDataGridColumnDefinition<TItem> column, bool editing) => CssClassBuilder.Combine([
        ColumnClass(column, false),
        column.Template is null && !(editing && column.EditTemplate is not null) ? "omni-data-grid__cell--text" : null
    ]);

    internal string RowClass(GridRenderRow<TItem> row) => CssClassBuilder.Combine([
        "omni-data-grid__row",
        row.HasItem && grid.Selection.IsSelected(grid.ItemKey(row.Item)) ? "omni-data-grid__row--selected" : null,
        row.HasItem && grid.Highlight.IsNewRow(row.Item) ? "omni-data-grid__row--new" : null,
        grid.AllowAlternatingRows && row.Index % 2 == 1 ? "omni-data-grid__row--alternate" : null,
        grid.Selection.RowsAreInteractive && row.Selectable ? "omni-data-grid__row--interactive" : null,
        row.Class
    ]);

    /// <summary>
    /// Secondary header affordances stay hidden until the header is hovered or focused, unless the
    /// affordance is currently active, in which case it remains visible so the column's state can be
    /// read without pointing at it.
    /// </summary>
    internal string HeaderActionClass(string baseClass, bool active) => CssClassBuilder.Combine([
        "omni-data-grid__icon-button",
        baseClass,
        "omni-data-grid__header-action",
        active ? "omni-data-grid__header-action--active" : null
    ]);

    /// <summary>
    /// The filter entry point is always visible: it is the column's primary control once the inline
    /// filter row has been replaced by the menu, so hiding it until hover would leave no sign that
    /// the column can be filtered at all.
    /// </summary>
    internal string FilterToggleClass(OmniDataGridColumnDefinition<TItem> column) => CssClassBuilder.Combine([
        "omni-data-grid__filter-menu-toggle",
        grid.Query.HasActiveFilter(column) ? "omni-data-grid__filter-menu-toggle--active" : null
    ]);

    internal string FilterCellClass(OmniDataGridColumnDefinition<TItem> column) => CssClassBuilder.Combine([
        "omni-data-grid__filter-cell",
        column.Frozen ? "omni-data-grid__column--frozen" : null
    ]);

    /// <summary>
    /// The expand and selection columns carry the grid's own controls. They are frozen with the
    /// data columns as soon as one of those is, since a frozen column that slides over the controls
    /// of its own row hides them.
    /// </summary>
    internal string ControlClass(string kind) => CssClassBuilder.Combine([
        "omni-data-grid__control",
        $"omni-data-grid__control--{kind}",
        grid.ColumnSet.HasFrozenColumns ? "omni-data-grid__column--frozen" : null
    ]);

    internal string ExpandButtonClass(bool expanded) => expanded
        ? "omni-data-grid__expand omni-data-grid__expand--open"
        : "omni-data-grid__expand";

    internal string GroupExpandClass(bool expanded) => expanded
        ? "omni-data-grid__icon-button omni-data-grid__group-expand omni-data-grid__expand--open"
        : "omni-data-grid__icon-button omni-data-grid__group-expand";

    /// <summary>
    /// The sort indicator keeps its slot at all times so the header does not reflow when a column
    /// gains or loses its sort; only its visibility changes.
    /// </summary>
    internal string SortIconClass(OmniDataGridColumnDefinition<TItem> column) => CssClassBuilder.Combine([
        "omni-data-grid__sort-icon",
        grid.Query.SortIcon(column) is null ? "omni-data-grid__sort-icon--idle" : null
    ]);
}
