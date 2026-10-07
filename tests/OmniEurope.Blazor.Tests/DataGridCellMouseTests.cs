using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// OnCellClick and OnCellDoubleClick: a drill-down on the figure under the pointer needs the row, the
/// column and its value, while the row events keep firing.
/// </summary>
public sealed class DataGridCellMouseTests : OmniBunitContext
{
    private sealed record Row(int Id, string Name, decimal Amount);

    private static readonly IReadOnlyList<Row> Rows = [new(1, "Un", 10m), new(2, "Deux", 20m), new(3, "Trois", 30m)];

    private IRenderedComponent<OmniDataGrid<Row>> RenderGrid(Action<ComponentParameterCollectionBuilder<OmniDataGrid<Row>>> configure, bool editable = false)
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<OmniDataGridColumn<Row>>(0);
            builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Name));
            builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Title), "Nom");
            if (editable)
            {
                builder.AddComponentParameter(3, nameof(OmniDataGridColumn<Row>.EditTemplate), (RenderFragment<Row>)(row => inner => inner.AddContent(0, row.Name)));
            }
            builder.CloseComponent();
            builder.OpenComponent<OmniDataGridColumn<Row>>(4);
            builder.AddComponentParameter(5, nameof(OmniDataGridColumn<Row>.Key), "montant");
            builder.AddComponentParameter(6, nameof(OmniDataGridColumn<Row>.Value), (Func<Row, object?>)(row => row.Amount));
            builder.AddComponentParameter(7, nameof(OmniDataGridColumn<Row>.Title), "Montant");
            builder.CloseComponent();
        };

        return Render<OmniDataGrid<Row>>(parameters =>
        {
            parameters.Add(grid => grid.Items, Rows).Add(grid => grid.Columns, columns).Add(grid => grid.KeyOf, row => row.Id);
            configure(parameters);
        });
    }

    [Fact]
    public void OnCellDoubleClick_CarriesTheRowTheColumnTheValueAndTheModifiers()
    {
        OmniDataGridCellMouseEventArgs<Row>? received = null;
        var grid = RenderGrid(parameters => parameters.Add(g => g.OnCellDoubleClick, args => received = args));

        grid.FindAll("tbody tr[data-omni-row-index]")[1].QuerySelector("td[data-omni-col='montant']")!
            .DoubleClick(new MouseEventArgs { CtrlKey = true, ClientX = 12, ClientY = 34 });

        Assert.NotNull(received);
        Assert.Equal(2, received!.Item.Id);
        Assert.Equal(1, received.RowIndex);
        Assert.Equal("montant", received.ColumnKey);
        Assert.Equal("Montant", received.ColumnTitle);
        Assert.Null(received.ColumnProperty);
        Assert.Equal(1, received.ColumnIndex);
        Assert.Equal(20m, received.Value);
        Assert.True(received.CtrlKey);
        Assert.False(received.ShiftKey);
        Assert.Equal(12, received.ClientX);
        Assert.Equal(34, received.ClientY);
    }

    [Fact]
    public void OnCellClick_ReportsThePropertyOfThePropertyColumn()
    {
        OmniDataGridCellMouseEventArgs<Row>? received = null;
        var grid = RenderGrid(parameters => parameters.Add(g => g.OnCellClick, args => received = args));

        grid.FindAll("tbody td[data-omni-col='Name']")[2].Click();

        Assert.NotNull(received);
        Assert.Equal(3, received!.Item.Id);
        Assert.Equal("Name", received.ColumnKey);
        Assert.Equal("Name", received.ColumnProperty);
        Assert.Equal(0, received.ColumnIndex);
        Assert.Equal("Trois", received.Value);
    }

    [Fact]
    public void CellEvents_LeaveTheRowEventsInPlace()
    {
        var order = new List<string>();
        var grid = RenderGrid(parameters => parameters
            .Add(g => g.OnCellDoubleClick, _ => order.Add("cell-dbl"))
            .Add(g => g.OnRowDoubleClick, _ => order.Add("row-dbl"))
            .Add(g => g.OnCellClick, _ => order.Add("cell"))
            .Add(g => g.OnRowClick, _ => order.Add("row")));

        // A data cell lets its events bubble to its row: it never stops their propagation. bUnit drops
        // the row handler once the cell handler re-rendered the grid (it reads every handler id before
        // dispatching), so the bubbling itself is checked in the browser; here the row keeps both.
        var cell = grid.FindAll("tbody td[data-omni-col='montant']")[0];
        Assert.False(cell.HasAttribute("blazor:onclick:stopPropagation"));
        Assert.False(cell.HasAttribute("blazor:ondblclick:stopPropagation"));
        cell.Click();
        grid.FindAll("tbody td[data-omni-col='montant']")[0].DoubleClick();
        grid.FindAll("tbody tr[data-omni-row-index]")[0].Click();
        grid.FindAll("tbody tr[data-omni-row-index]")[0].DoubleClick();

        Assert.Equal(["cell", "cell-dbl", "row", "row-dbl"], order);
    }

    [Fact]
    public void ClickOnACell_WithoutCellHandler_ReachesTheRow()
    {
        OmniDataGridRowMouseEventArgs<Row>? received = null;
        var grid = RenderGrid(parameters => parameters.Add(g => g.OnRowClick, args => received = args));

        grid.FindAll("tbody td[data-omni-col='montant']")[1].Click();

        Assert.Equal(2, received?.Item.Id);
    }

    [Fact]
    public void WithoutHandlers_CellsCarryNoPointerHandler()
    {
        var grid = RenderGrid(_ => { });
        var cell = grid.FindAll("tbody td[data-omni-col='montant']")[0];

        Assert.Throws<MissingEventHandlerException>(() => cell.Click());
        Assert.Throws<MissingEventHandlerException>(() => cell.DoubleClick());
    }

    [Fact]
    public async Task RowInEdit_DoesNotReportCellEvents()
    {
        var clicks = 0;
        var grid = RenderGrid(parameters => parameters.Add(g => g.OnCellClick, _ => clicks++), editable: true);

        await grid.InvokeAsync(() => grid.Instance.EditRowAsync(Rows[0]));
        var cell = grid.FindAll("tbody tr[data-omni-row-index]")[0].QuerySelector("td[data-omni-col='montant']")!;
        Assert.Throws<MissingEventHandlerException>(() => cell.Click());
        Assert.Equal(0, clicks);
    }

    [Fact]
    public async Task AnEditedRow_CarriesTheEditingClass_UntilTheEditEnds()
    {
        // recette R-017: the stylesheet keeps such a row at the height of a fixed-row grid.
        var grid = RenderGrid(_ => { }, editable: true);
        Assert.Empty(grid.FindAll("tr.omni-data-grid__row--editing"));

        await grid.InvokeAsync(() => grid.Instance.EditRowAsync(Rows[0]));

        Assert.Equal("0", grid.Find("tr.omni-data-grid__row--editing").GetAttribute("data-omni-row-index"));
        await grid.InvokeAsync(() => grid.Instance.CancelEditAsync(Rows[0]));
        grid.WaitForAssertion(() => Assert.Empty(grid.FindAll("tr.omni-data-grid__row--editing")));
    }
}
