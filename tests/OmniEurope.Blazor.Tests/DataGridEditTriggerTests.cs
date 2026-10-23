using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <c>OmniDataGrid.EditTrigger</c>: with <see cref="OmniDataGridEditTrigger.Cell"/> a single click anywhere in a
/// cell whose column has an <c>EditTemplate</c> puts the row in edit mode, the field of that cell taking the
/// focus. The pointer is decided by the grid script (bUnit runs none): it reports a press that fell away from
/// the controls of the cell to the grid, which these tests drive through the bridge the script calls, and the
/// script's own contract is read from its source; the real clicks are driven by the scripts probe
/// (eng/scripts-probe/requests.mjs, grid step). The keyboard is a real button in each such cell, which bUnit
/// does drive.
/// </summary>
public sealed class DataGridEditTriggerTests : OmniBunitContext
{
    private const string GridModule = "./_content/OmniEurope.Blazor/omni-grid.js";

    private sealed record Row(int Id, string Name, string Value);

    private static readonly IReadOnlyList<Row> Rows = [new(1, "Alpha", "un"), new(2, "Beta", string.Empty), new(3, "Gamma", "trois")];

    private BunitJSModuleInterop SetupGrid()
    {
        var module = JSInterop.SetupModule(GridModule);
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<bool>("focusEditor", _ => true).SetResult(true);
        return module;
    }

    // Name: no EditTemplate. Value: an EditTemplate and a display template that holds a button next to the text.
    // Note: an EditTemplate and no display template, so the cell shows its plain text.
    private static RenderFragment Columns() => builder =>
    {
        builder.OpenComponent<OmniDataGridColumn<Row>>(0);
        builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Name));
        builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Title), "Nom");
        builder.CloseComponent();
        builder.OpenComponent<OmniDataGridColumn<Row>>(3);
        builder.AddComponentParameter(4, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Value));
        builder.AddComponentParameter(5, nameof(OmniDataGridColumn<Row>.Title), "Valeur");
        builder.AddComponentParameter(6, nameof(OmniDataGridColumn<Row>.Template), (RenderFragment<Row>)(row => inner =>
        {
            inner.AddContent(0, row.Value);
            inner.OpenElement(1, "button");
            inner.AddAttribute(2, "type", "button");
            inner.AddAttribute(3, "class", "inside");
            inner.AddContent(4, "x");
            inner.CloseElement();
        }));
        builder.AddComponentParameter(7, nameof(OmniDataGridColumn<Row>.EditTemplate), (RenderFragment<Row>)(row => inner => Editor(inner, "value", row.Value)));
        builder.CloseComponent();
        builder.OpenComponent<OmniDataGridColumn<Row>>(8);
        builder.AddComponentParameter(9, nameof(OmniDataGridColumn<Row>.Key), "note");
        builder.AddComponentParameter(10, nameof(OmniDataGridColumn<Row>.Value), (Func<Row, object?>)(row => row.Name.ToUpperInvariant()));
        builder.AddComponentParameter(11, nameof(OmniDataGridColumn<Row>.Title), "Note");
        builder.AddComponentParameter(12, nameof(OmniDataGridColumn<Row>.EditTemplate), (RenderFragment<Row>)(row => inner => Editor(inner, "note", row.Name)));
        builder.CloseComponent();
    };

    private static void Editor(RenderTreeBuilder inner, string name, string value)
    {
        inner.OpenElement(0, "input");
        inner.AddAttribute(1, "class", name);
        inner.AddAttribute(2, "value", value);
        inner.CloseElement();
    }

    private IRenderedComponent<OmniDataGrid<Row>> RenderGrid(Action<ComponentParameterCollectionBuilder<OmniDataGrid<Row>>>? configure = null) =>
        Render<OmniDataGrid<Row>>(parameters =>
        {
            parameters.Add(grid => grid.Items, Rows)
                .Add(grid => grid.Columns, Columns())
                .Add(grid => grid.KeyOf, row => row.Id);
            configure?.Invoke(parameters);
        });

    private static GridInteropBridge<Row> BridgeOf(IRenderedComponent<OmniDataGrid<Row>> grid) => new(grid.Instance);

    [Fact]
    public void The_default_is_Action_and_a_grid_that_edits_on_an_action_marks_and_wires_nothing()
    {
        var module = SetupGrid();
        var grid = RenderGrid();

        Assert.Equal(OmniDataGridEditTrigger.Action, grid.Instance.EditTrigger);
        Assert.Empty(grid.FindAll(".omni-data-grid__cell--edit-trigger"));
        Assert.Empty(grid.FindAll(".omni-data-grid__edit-cell"));
        Assert.DoesNotContain(module.Invocations, invocation => invocation.Identifier == "attachEditCell");
        // The edit button of the row is how a row enters edit mode, as before.
        Assert.NotEmpty(grid.FindAll("td.omni-data-grid__actions button"));
    }

    [Fact]
    public async Task On_Action_a_report_from_the_script_does_nothing()
    {
        SetupGrid();
        var grid = RenderGrid();

        await grid.InvokeAsync(() => BridgeOf(grid).OnEditCellAsync(0, "Value"));

        Assert.Empty(grid.FindAll("tr.omni-data-grid__row--editing"));
    }

    [Fact]
    public void On_Cell_only_the_cells_of_columns_with_an_edit_template_are_marked_and_hold_a_named_button()
    {
        SetupGrid();
        var grid = RenderGrid(parameters => parameters.Add(component => component.EditTrigger, OmniDataGridEditTrigger.Cell));

        foreach (var key in new[] { "Value", "note" })
        {
            var cells = grid.FindAll($"tbody td[data-omni-col='{key}']");
            Assert.Equal(Rows.Count, cells.Count);
            Assert.All(cells, cell => Assert.Contains("omni-data-grid__cell--edit-trigger", cell.ClassList));
            Assert.All(cells, cell => Assert.Single(cell.QuerySelectorAll("button.omni-data-grid__edit-cell")));
        }

        // No EditTemplate: nothing to edit there, so no mark and no button.
        var plain = grid.FindAll("tbody td[data-omni-col='Name']");
        Assert.All(plain, cell => Assert.DoesNotContain("omni-data-grid__cell--edit-trigger", cell.ClassList));
        Assert.All(plain, cell => Assert.Empty(cell.QuerySelectorAll("button")));

        // A real button, named after its column by a localized text, never a role on the cell itself.
        var button = grid.Find("tbody td[data-omni-col='Value'] button.omni-data-grid__edit-cell");
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal(grid.Instance.Text("GridEditCell", "Valeur"), button.GetAttribute("aria-label"));
        Assert.Contains("Valeur", button.GetAttribute("aria-label"), StringComparison.Ordinal);
        Assert.All(grid.FindAll("tbody td[data-omni-col]"), cell => Assert.Null(cell.GetAttribute("role")));

        // The cell keeps what it showed, including the control of its own template.
        Assert.Equal(Rows.Count, grid.FindAll("tbody td[data-omni-col='Value'] button.inside").Count);
    }

    [Fact]
    public async Task A_row_in_edit_mode_keeps_its_editors_and_no_trigger_button()
    {
        SetupGrid();
        var grid = RenderGrid(parameters => parameters.Add(component => component.EditTrigger, OmniDataGridEditTrigger.Cell));

        await grid.InvokeAsync(() => BridgeOf(grid).OnEditCellAsync(0, "Value"));

        var editing = grid.Find("tr.omni-data-grid__row--editing");
        Assert.NotNull(editing.QuerySelector("td[data-omni-col='Value'] input.value"));
        Assert.Empty(editing.QuerySelectorAll(".omni-data-grid__edit-cell"));
        Assert.Empty(editing.QuerySelectorAll(".omni-data-grid__cell--edit-trigger"));
        // The other rows still offer theirs.
        Assert.Equal((Rows.Count - 1) * 2, grid.FindAll("tbody tr:not(.omni-data-grid__row--editing) button.omni-data-grid__edit-cell").Count);
    }

    [Fact]
    public void The_script_is_wired_once_on_Cell_and_released_when_the_trigger_goes_back_to_Action_and_at_disposal()
    {
        var module = SetupGrid();
        var grid = RenderGrid(parameters => parameters.Add(component => component.EditTrigger, OmniDataGridEditTrigger.Cell));

        grid.Render();
        Assert.Single(module.Invocations["attachEditCell"]);
        Assert.DoesNotContain(module.Invocations, invocation => invocation.Identifier == "detachEditCell");

        grid.Render(parameters => parameters.Add(component => component.EditTrigger, OmniDataGridEditTrigger.Action));
        Assert.Single(module.Invocations["detachEditCell"]);
        Assert.Empty(grid.FindAll(".omni-data-grid__edit-cell"));

        grid.Render(parameters => parameters.Add(component => component.EditTrigger, OmniDataGridEditTrigger.Cell));
        Assert.Equal(2, module.Invocations["attachEditCell"].Count);
    }

    [Fact]
    public async Task Disposal_releases_the_script_of_a_grid_that_edits_on_Cell()
    {
        var module = SetupGrid();
        var grid = RenderGrid(parameters => parameters.Add(component => component.EditTrigger, OmniDataGridEditTrigger.Cell));

        await grid.InvokeAsync(() => grid.Instance.DisposeAsync().AsTask());

        Assert.Single(module.Invocations["detachEditCell"]);
    }

    [Fact]
    public async Task A_press_on_the_empty_value_cell_puts_its_row_in_edit_mode_with_that_cell_s_field_focused()
    {
        var module = SetupGrid();
        var edited = new List<int>();
        var grid = RenderGrid(parameters => parameters
            .Add(component => component.EditTrigger, OmniDataGridEditTrigger.Cell)
            .Add(component => component.OnRowEdit, row => edited.Add(row.Id)));

        // Beta has an empty Value: its cell is still there to be pressed.
        Assert.Equal(string.Empty, grid.Find("tbody tr[data-omni-row-index='1'] td[data-omni-col='Value']").TextContent.Replace("x", string.Empty, StringComparison.Ordinal).Trim());
        await grid.InvokeAsync(() => BridgeOf(grid).OnEditCellAsync(1, "Value"));

        Assert.Equal([2], edited);
        var row = grid.Find("tr.omni-data-grid__row--editing");
        Assert.Equal("1", row.GetAttribute("data-omni-row-index"));
        Assert.Equal("true", row.GetAttribute("data-omni-edit-focus"));
        // The script is asked to focus the field of the pressed column, not the first one of the row.
        grid.WaitForAssertion(() => Assert.Equal("Value", Assert.Single(module.Invocations["focusEditor"]).Arguments[1]));
    }

    [Fact]
    public async Task A_press_on_another_column_focuses_the_field_of_that_column()
    {
        var module = SetupGrid();
        var grid = RenderGrid(parameters => parameters.Add(component => component.EditTrigger, OmniDataGridEditTrigger.Cell));

        await grid.InvokeAsync(() => BridgeOf(grid).OnEditCellAsync(2, "note"));

        Assert.Equal("2", grid.Find("tr.omni-data-grid__row--editing").GetAttribute("data-omni-row-index"));
        grid.WaitForAssertion(() => Assert.Equal("note", Assert.Single(module.Invocations["focusEditor"]).Arguments[1]));
    }

    [Fact]
    public async Task A_press_on_a_column_without_edit_template_or_a_row_that_is_not_there_does_nothing()
    {
        SetupGrid();
        var edited = new List<int>();
        var grid = RenderGrid(parameters => parameters
            .Add(component => component.EditTrigger, OmniDataGridEditTrigger.Cell)
            .Add(component => component.OnRowEdit, row => edited.Add(row.Id)));
        var bridge = BridgeOf(grid);

        await grid.InvokeAsync(() => bridge.OnEditCellAsync(0, "Name"));
        await grid.InvokeAsync(() => bridge.OnEditCellAsync(0, "absent"));
        await grid.InvokeAsync(() => bridge.OnEditCellAsync(42, "Value"));

        Assert.Empty(edited);
        Assert.Empty(grid.FindAll("tr.omni-data-grid__row--editing"));
    }

    [Fact]
    public async Task A_press_on_a_row_already_in_edit_mode_changes_nothing_and_another_row_replaces_it_in_single_mode()
    {
        SetupGrid();
        var edited = new List<int>();
        var grid = RenderGrid(parameters => parameters
            .Add(component => component.EditTrigger, OmniDataGridEditTrigger.Cell)
            .Add(component => component.OnRowEdit, row => edited.Add(row.Id)));
        var bridge = BridgeOf(grid);

        await grid.InvokeAsync(() => bridge.OnEditCellAsync(0, "Value"));
        await grid.InvokeAsync(() => bridge.OnEditCellAsync(0, "note"));
        Assert.Equal([1], edited);

        await grid.InvokeAsync(() => bridge.OnEditCellAsync(2, "Value"));
        Assert.Equal([1, 3], edited);
        Assert.Equal("2", Assert.Single(grid.FindAll("tr.omni-data-grid__row--editing")).GetAttribute("data-omni-row-index"));
    }

    [Fact]
    public async Task Several_rows_stay_in_edit_mode_in_Multiple_edit_mode()
    {
        SetupGrid();
        var grid = RenderGrid(parameters => parameters
            .Add(component => component.EditTrigger, OmniDataGridEditTrigger.Cell)
            .Add(component => component.EditMode, OmniDataGridRowMode.Multiple));
        var bridge = BridgeOf(grid);

        await grid.InvokeAsync(() => bridge.OnEditCellAsync(0, "Value"));
        await grid.InvokeAsync(() => bridge.OnEditCellAsync(2, "Value"));

        Assert.Equal(2, grid.FindAll("tr.omni-data-grid__row--editing").Count);
    }

    [Fact]
    public void Enter_or_Space_on_the_button_of_a_cell_starts_the_edit_on_that_cell_without_a_click_on_the_row()
    {
        var module = SetupGrid();
        var edited = new List<int>();
        var rowClicks = new List<int>();
        var cellClicks = new List<string>();
        var grid = RenderGrid(parameters => parameters
            .Add(component => component.EditTrigger, OmniDataGridEditTrigger.Cell)
            .Add(component => component.OnRowEdit, row => edited.Add(row.Id))
            .Add(component => component.OnRowClick, args => rowClicks.Add(args.Item.Id))
            .Add(component => component.OnCellClick, args => cellClicks.Add(args.ColumnKey)));

        // A native button: Enter (on press) and Space (on release) both end in its click, which is all a
        // browser delivers to the grid, so the click stands for both keys. The click and the keys stay with the
        // button, as the edit column's do: neither is a click or a key of the row.
        var button = grid.Find("tbody tr[data-omni-row-index='2'] td[data-omni-col='note'] button.omni-data-grid__edit-cell");
        Assert.Contains(button.Attributes, attribute => attribute.Name.EndsWith("onclick:stoppropagation", StringComparison.Ordinal));
        Assert.Contains(button.Attributes, attribute => attribute.Name.EndsWith("onkeydown:stoppropagation", StringComparison.Ordinal));
        button.Click();

        Assert.Equal([3], edited);
        Assert.Empty(rowClicks);
        Assert.Empty(cellClicks);
        Assert.Equal("2", grid.Find("tr.omni-data-grid__row--editing").GetAttribute("data-omni-row-index"));
        grid.WaitForAssertion(() => Assert.Equal("note", Assert.Single(module.Invocations["focusEditor"]).Arguments[1]));
    }

    [Fact]
    public void The_script_leaves_the_clicks_of_the_controls_of_a_cell_alone_and_only_reports_a_plain_click_on_an_editable_cell()
    {
        var script = File.ReadAllText(Path.Combine(ShippedLookTests.RepositoryRoot(), "src", "OmniEurope.Blazor", "wwwroot", "grid", "edit-cell.js"));
        var grid = File.ReadAllText(Path.Combine(ShippedLookTests.RepositoryRoot(), "src", "OmniEurope.Blazor", "wwwroot", "omni-grid.js"));
        var focus = File.ReadAllText(Path.Combine(ShippedLookTests.RepositoryRoot(), "src", "OmniEurope.Blazor", "wwwroot", "grid", "edit-focus.js"));

        Assert.Contains("from './grid/edit-cell.js'", grid, StringComparison.Ordinal);
        Assert.Contains("viewport.addEventListener('click'", script, StringComparison.Ordinal);
        // The controls whose clicks are theirs: button, link, field, and the roles of a button and a link.
        foreach (var control in new[] { "'a[href]'", "'button'", "'input:not([type=\"hidden\"])'", "'select'", "'textarea'", "'[role=\"button\"]'", "'[role=\"link\"]'" })
        {
            Assert.Contains(control, script, StringComparison.Ordinal);
        }

        Assert.Contains("omni-data-grid__cell--edit-trigger", script, StringComparison.Ordinal);
        Assert.Contains("'OnEditCellAsync'", script, StringComparison.Ordinal);
        // On a row already in edit mode the click only moves the focus.
        Assert.Contains("omni-data-grid__row--editing", script, StringComparison.Ordinal);
        Assert.Contains("focusCellEditor", script, StringComparison.Ordinal);
        Assert.Contains("export function focusCellEditor", focus, StringComparison.Ordinal);
    }
}
