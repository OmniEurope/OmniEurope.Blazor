using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Renders the value control of a grid column filter, and of a body cell: the one place that decides
/// which editor a filterable column gets.
/// </summary>
internal sealed class GridFilterControls<TItem>(OmniDataGrid<TItem> grid)
{
    /// <summary>
    /// The single place that decides which editor a filterable column gets. A column's own
    /// <c>FilterTemplate</c> wins over every built-in shape, which is how a mode none of them covers
    /// is added without touching the grid. Shared by the inline filter row and the header filter
    /// menu, and by the primary and secondary condition of the advanced filter mode.
    /// </summary>
    internal RenderFragment FilterValueControl(OmniDataGridColumnDefinition<TItem> column, string id, string value, Func<string, Task> onChanged, bool inPanel) => builder =>
    {
        var editor = grid.FilterEditor;
        if (column.FilterTemplate is not null)
        {
            // The template's callbacks belong to whichever component declared the column. When that
            // is a component of its own inside Columns, the event re-renders it and not the grid, so
            // the filter was taken and nothing was filtered: the grid renders itself here.
            builder.AddContent(0, column.FilterTemplate(new OmniDataGridFilterContext(
                id,
                value,
                editor.DistinctFilterValues(column),
                grid.Text("GridFilterPlaceholder"),
                async changed =>
                {
                    await onChanged(changed);
                    grid.Render();
                })
            {
                InPopover = inPanel
            }));
            return;
        }

        var onChange = EventCallback.Factory.Create<ChangeEventArgs>(grid, args => onChanged(args.Value?.ToString() ?? string.Empty));
        switch (column.FilterType)
        {
            case OmniDataGridColumnFilterType.DateRange:
                builder.OpenComponent<OmniDataGridFilterDateRange>(0);
                builder.AddComponentParameter(1, nameof(OmniDataGridFilterDateRange.Id), id);
                builder.AddComponentParameter(2, nameof(OmniDataGridFilterDateRange.Value), value);
                builder.AddComponentParameter(3, nameof(OmniDataGridFilterDateRange.IncludesTime), column.FilterIncludesTime);
                builder.AddComponentParameter(
                    4,
                    nameof(OmniDataGridFilterDateRange.ValueChanged),
                    EventCallback.Factory.Create<string>(grid, encoded => onChanged(encoded)));
                builder.CloseComponent();
                break;

            case OmniDataGridColumnFilterType.MultiSelect:
                builder.OpenComponent<OmniDataGridFilterMultiSelect>(0);
                builder.AddComponentParameter(1, nameof(OmniDataGridFilterMultiSelect.Id), id);
                builder.AddComponentParameter(2, nameof(OmniDataGridFilterMultiSelect.Value), value);
                builder.AddComponentParameter(3, nameof(OmniDataGridFilterMultiSelect.Suggestions), editor.DistinctFilterValues(column));
                builder.AddComponentParameter(4, nameof(OmniDataGridFilterMultiSelect.Placeholder), grid.Text("GridFilterPlaceholder"));
                builder.AddComponentParameter(5, nameof(OmniDataGridFilterMultiSelect.Filterable), column.FilterSearchable);
                builder.AddComponentParameter(8, nameof(OmniDataGridFilterMultiSelect.FormatValue), column.FormatFilterValue);
                builder.AddComponentParameter(9, nameof(OmniDataGridFilterMultiSelect.Label), grid.Text("GridFilterColumn", column.Title));
                builder.AddComponentParameter(
                    6,
                    nameof(OmniDataGridFilterMultiSelect.ValueChanged),
                    EventCallback.Factory.Create<string>(grid, encoded => onChanged(encoded)));
                // Inside a popover the list is already on demand; in a header row it has to fold
                // into one line, or it stretches the header to the height of the whole list.
                builder.AddComponentParameter(
                    7,
                    nameof(OmniDataGridFilterMultiSelect.Presentation),
                    inPanel ? OmniMultiSelectPresentation.List : OmniMultiSelectPresentation.Compact);
                builder.CloseComponent();
                break;

            case OmniDataGridColumnFilterType.Select:
                builder.OpenElement(0, "select");
                builder.AddAttribute(1, "id", id);
                builder.AddAttribute(2, "class", "omni-input omni-data-grid__filter");
                builder.AddAttribute(3, "value", value);
                builder.AddAttribute(4, "onchange", onChange);
                builder.OpenElement(5, "option");
                builder.AddAttribute(6, "value", string.Empty);
                builder.AddContent(7, grid.Text("GridFilterPlaceholder"));
                builder.CloseElement();
                var selectSeq = 8;
                foreach (var candidate in editor.DistinctFilterValues(column))
                {
                    builder.OpenElement(selectSeq++, "option");
                    builder.AddAttribute(selectSeq++, "value", candidate);
                    builder.AddAttribute(selectSeq++, "selected", string.Equals(candidate, value, StringComparison.Ordinal));
                    builder.AddContent(selectSeq++, GridFilterEditor<TItem>.CandidateText(column, candidate));
                    builder.CloseElement();
                }
                builder.CloseElement();
                break;

            case OmniDataGridColumnFilterType.Combo:
                builder.OpenComponent<OmniDataGridFilterCombo>(0);
                builder.AddComponentParameter(1, nameof(OmniDataGridFilterCombo.Id), id);
                builder.AddComponentParameter(2, nameof(OmniDataGridFilterCombo.Value), value);
                builder.AddComponentParameter(3, nameof(OmniDataGridFilterCombo.Suggestions), editor.DistinctFilterValues(column));
                builder.AddComponentParameter(4, nameof(OmniDataGridFilterCombo.Placeholder), grid.Text("GridFilterPlaceholder"));
                builder.AddComponentParameter(
                    5,
                    nameof(OmniDataGridFilterCombo.ValueChanged),
                    EventCallback.Factory.Create<string>(grid, typed => onChanged(typed)));
                builder.AddComponentParameter(
                    6,
                    nameof(OmniDataGridFilterCombo.OnPick),
                    EventCallback.Factory.Create(grid, grid.Script.CloseFilterMenusAsync));
                builder.CloseComponent();
                break;

            default:
                builder.OpenElement(0, "input");
                builder.AddAttribute(1, "id", id);
                builder.AddAttribute(2, "class", "omni-input omni-data-grid__filter");
                builder.AddAttribute(3, "placeholder", grid.Text("GridFilterPlaceholder"));
                builder.AddAttribute(4, "value", value);
                // Filters as the user types rather than on blur, so the table follows the keystrokes.
                builder.AddAttribute(5, "oninput", onChange);
                var numeric = column.Numeric || column.FilterType == OmniDataGridColumnFilterType.Number;
                builder.AddAttribute(6, "type", numeric ? "number" : "text");
                if (numeric) builder.AddAttribute(7, "step", "any");
                builder.CloseElement();
                break;
        }
    };

    /// <summary>A body cell: the edit template of a row in edit, else the column's template, else its text.</summary>
    internal RenderFragment Cell(OmniDataGridColumnDefinition<TItem> column, TItem item, bool editing) => builder =>
    {
        if (editing && column.EditTemplate is not null)
        {
            builder.AddContent(0, column.EditTemplate(item));
            return;
        }

        if (column.Template is not null)
        {
            builder.AddContent(1, column.Template(item));
            return;
        }

        builder.AddContent(2, GridColumnLayout<TItem>.CellText(column, item));
    };
}
