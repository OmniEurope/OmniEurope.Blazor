using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
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
        if (column.FilterTemplate is not null)
        {
            RenderTemplate(builder, column, id, value, onChanged, inPanel);
            return;
        }

        switch (column.FilterType)
        {
            case OmniDataGridColumnFilterType.DateRange:
                OpenDateRange(builder, 0, column, id, value, onChanged);
                break;
            case OmniDataGridColumnFilterType.Date:
                RenderDate(builder, column, id, value, onChanged, inPanel);
                break;
            case OmniDataGridColumnFilterType.MultiSelect:
                RenderMultiSelect(builder, column, id, value, onChanged, inPanel);
                break;
            case OmniDataGridColumnFilterType.Select:
                RenderSelect(builder, column, id, value, onChanged);
                break;
            case OmniDataGridColumnFilterType.Combo:
                RenderCombo(builder, column, id, value, onChanged);
                break;
            default:
                RenderText(builder, column, id, value, onChanged);
                break;
        }
    };

    /// <summary>The column's own <c>FilterTemplate</c>, given the value, the distinct values and a change callback that re-renders the grid.</summary>
    private void RenderTemplate(RenderTreeBuilder builder, OmniDataGridColumnDefinition<TItem> column, string id, string value, Func<string, Task> onChanged, bool inPanel)
    {
        // The template's callbacks belong to whichever component declared the column. When that
        // is a component of its own inside Columns, the event re-renders it and not the grid, so
        // the filter was taken and nothing was filtered: the grid renders itself here.
        builder.AddContent(0, column.FilterTemplate!(new OmniDataGridFilterContext(
            id,
            value,
            grid.FilterEditor.DistinctFilterValues(column),
            grid.Text("GridFilterPlaceholder"),
            async changed =>
            {
                await onChanged(changed);
                grid.Render();
            })
        {
            InPopover = inPanel
        }));
    }

    private void RenderDate(RenderTreeBuilder builder, OmniDataGridColumnDefinition<TItem> column, string id, string value, Func<string, Task> onChanged, bool inPanel)
    {
        // The header menu holds the range or the typed field, by the column's choice; the row, the typed
        // field. The range alone takes the id of the editor, so the label of the editor points at it.
        var editor = grid.FilterEditor;
        var text = editor.ShowsDateText(column);
        if (text)
        {
            builder.OpenComponent<OmniDataGridFilterDate>(0);
            builder.AddComponentParameter(1, nameof(OmniDataGridFilterDate.Id), id);
            builder.AddComponentParameter(2, nameof(OmniDataGridFilterDate.Value), value);
            builder.AddComponentParameter(
                3,
                nameof(OmniDataGridFilterDate.ValueChanged),
                EventCallback.Factory.Create<string>(grid, encoded => onChanged(encoded)));
            builder.CloseComponent();
        }

        if (editor.HoldsRange(column, inPanel))
        {
            OpenDateRange(builder, 4, column, text ? $"{id}-range" : id, value, onChanged);
        }
    }

    private void RenderMultiSelect(RenderTreeBuilder builder, OmniDataGridColumnDefinition<TItem> column, string id, string value, Func<string, Task> onChanged, bool inPanel)
    {
        builder.OpenComponent<OmniDataGridFilterMultiSelect>(0);
        builder.AddComponentParameter(1, nameof(OmniDataGridFilterMultiSelect.Id), id);
        builder.AddComponentParameter(2, nameof(OmniDataGridFilterMultiSelect.Value), value);
        builder.AddComponentParameter(3, nameof(OmniDataGridFilterMultiSelect.Suggestions), grid.FilterEditor.DistinctFilterValues(column));
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
            nameof(OmniDataGridFilterMultiSelect.Display),
            inPanel ? OmniMultiSelectDisplay.List : OmniMultiSelectDisplay.Compact);
        builder.CloseComponent();
    }

    private void RenderSelect(RenderTreeBuilder builder, OmniDataGridColumnDefinition<TItem> column, string id, string value, Func<string, Task> onChanged)
    {
        builder.OpenElement(0, "select");
        builder.AddAttribute(1, "id", id);
        builder.AddAttribute(2, "class", "omni-input omni-data-grid__filter");
        builder.AddAttribute(3, "value", value);
        builder.AddAttribute(4, "onchange", ChangeCallback(onChanged));
        builder.OpenElement(5, "option");
        builder.AddAttribute(6, "value", string.Empty);
        builder.AddContent(7, grid.Text("GridFilterPlaceholder"));
        builder.CloseElement();
        var sequence = 8;
        foreach (var candidate in grid.FilterEditor.DistinctFilterValues(column))
        {
            builder.OpenElement(sequence++, "option");
            builder.AddAttribute(sequence++, "value", candidate);
            builder.AddAttribute(sequence++, "selected", string.Equals(candidate, value, StringComparison.Ordinal));
            builder.AddContent(sequence++, GridFilterEditor<TItem>.CandidateText(column, candidate));
            builder.CloseElement();
        }

        builder.CloseElement();
    }

    private void RenderCombo(RenderTreeBuilder builder, OmniDataGridColumnDefinition<TItem> column, string id, string value, Func<string, Task> onChanged)
    {
        builder.OpenComponent<OmniDataGridFilterCombo>(0);
        builder.AddComponentParameter(1, nameof(OmniDataGridFilterCombo.Id), id);
        builder.AddComponentParameter(2, nameof(OmniDataGridFilterCombo.Value), value);
        builder.AddComponentParameter(3, nameof(OmniDataGridFilterCombo.Suggestions), grid.FilterEditor.DistinctFilterValues(column));
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
    }

    private void RenderText(RenderTreeBuilder builder, OmniDataGridColumnDefinition<TItem> column, string id, string value, Func<string, Task> onChanged)
    {
        builder.OpenElement(0, "input");
        builder.AddAttribute(1, "id", id);
        builder.AddAttribute(2, "class", "omni-input omni-data-grid__filter");
        builder.AddAttribute(3, "placeholder", grid.Text("GridFilterPlaceholder"));
        builder.AddAttribute(4, "value", value);
        // Filters as the user types rather than on blur, so the table follows the keystrokes.
        builder.AddAttribute(5, "oninput", ChangeCallback(onChanged));
        var numeric = column.Numeric || column.FilterType == OmniDataGridColumnFilterType.Number;
        builder.AddAttribute(6, "type", numeric ? "number" : "text");
        if (numeric)
        {
            builder.AddAttribute(7, "step", "any");
        }

        builder.CloseElement();
    }

    private EventCallback<ChangeEventArgs> ChangeCallback(Func<string, Task> onChanged) =>
        EventCallback.Factory.Create<ChangeEventArgs>(grid, args => onChanged(args.Value?.ToString() ?? string.Empty));

    /// <summary>The From and To range of a date column, alone: what its header calendar menu holds.</summary>
    internal RenderFragment DateRangeControl(OmniDataGridColumnDefinition<TItem> column, string id, string value, Func<string, Task> onChanged) =>
        builder => OpenDateRange(builder, 0, column, id, value, onChanged);

    // The two pickers, reading and writing the filter's one encoded value. The sequence numbers start at
    // the caller's: the Date editor renders this after its text input in the same fragment.
    private void OpenDateRange(RenderTreeBuilder builder, int sequence, OmniDataGridColumnDefinition<TItem> column, string id, string value, Func<string, Task> onChanged)
    {
        builder.OpenComponent<OmniDataGridFilterDateRange>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(OmniDataGridFilterDateRange.Id), id);
        builder.AddComponentParameter(sequence + 2, nameof(OmniDataGridFilterDateRange.Value), value);
        builder.AddComponentParameter(sequence + 3, nameof(OmniDataGridFilterDateRange.IncludesTime), column.FilterIncludesTime);
        builder.AddComponentParameter(
            sequence + 4,
            nameof(OmniDataGridFilterDateRange.ValueChanged),
            EventCallback.Factory.Create<string>(grid, encoded => onChanged(encoded)));
        builder.CloseComponent();
    }

    /// <summary>
    /// A body cell: the edit template of a row in edit, else the column's template, else its text; the cell of a
    /// column that edits its row on a click (<c>EditTrigger</c> Cell) also holds the button the keyboard reaches.
    /// </summary>
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
        }
        else
        {
            builder.AddContent(2, GridColumnLayout<TItem>.CellText(column, item));
        }

        if (!editing && grid.Editing.TriggersFrom(column))
        {
            EditCellButton(builder, column, item);
        }
    };

    // A real button, visually hidden and named after the column: the cell is reached by Tab and Enter or Space
    // start the edit, whatever the cell shows. A pointer click anywhere in the cell goes through the script
    // instead, which leaves the clicks of the cell's own controls alone. The click and the keys stop here, as
    // those of the edit column's buttons do: they are not a click or a key of the row.
    private void EditCellButton(RenderTreeBuilder builder, OmniDataGridColumnDefinition<TItem> column, TItem item)
    {
        builder.OpenElement(3, "button");
        builder.AddAttribute(4, "type", "button");
        builder.AddAttribute(5, "class", "omni-visually-hidden omni-data-grid__edit-cell");
        builder.AddAttribute(6, "aria-label", grid.Text("GridEditCell", column.Title));
        builder.AddAttribute(7, "onclick", EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(grid, _ => grid.Editing.EditAsync(item, column.Key)));
        builder.AddEventStopPropagationAttribute(8, "onclick", true);
        builder.AddEventStopPropagationAttribute(9, "onkeydown", true);
        builder.CloseElement();
    }
}
