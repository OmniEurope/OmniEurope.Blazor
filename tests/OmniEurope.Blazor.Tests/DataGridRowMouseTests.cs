using Bunit;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

using Row = OmniEurope.Blazor.Tests.NamedRow;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// OnRowClick, OnRowDoubleClick and OnRowContextMenu: a host that selects with Ctrl or Shift and opens
/// its own menu on a right-click needs the modifier keys and the row.
/// </summary>
public sealed class DataGridRowMouseTests : OmniBunitContext
{
    private static readonly IReadOnlyList<Row> Rows = DataGridTestSetup.ThreeNamed;

    private IRenderedComponent<OmniDataGrid<Row>> RenderGrid(
        Action<OmniDataGridRowMouseEventArgs<Row>>? onClick = null,
        Action<OmniDataGridRowMouseEventArgs<Row>>? onContextMenu = null,
        Action<OmniDataGridRowMouseEventArgs<Row>>? onDoubleClick = null)
    {
        return this.RenderDataGrid(Rows, builder => DataGridTestSetup.PropertyColumn<Row>(builder, 0, nameof(Row.Name), "Nom"), parameters =>
        {
            if (onClick is not null)
                parameters.Add(grid => grid.OnRowClick, onClick);
            if (onContextMenu is not null)
                parameters.Add(grid => grid.OnRowContextMenu, onContextMenu);
            if (onDoubleClick is not null)
                parameters.Add(grid => grid.OnRowDoubleClick, onDoubleClick);
        });
    }

    [Fact]
    public void OnRowClick_CarriesTheRowAndTheModifierKeys()
    {
        OmniDataGridRowMouseEventArgs<Row>? received = null;
        var grid = RenderGrid(onClick: args => received = args);

        grid.FindAll("tbody tr[data-omni-row-index]")[1].Click(new MouseEventArgs { CtrlKey = true, ShiftKey = true });

        Assert.NotNull(received);
        Assert.Equal(2, received!.Item.Id);
        Assert.Equal(1, received.Index);
        Assert.True(received.CtrlKey);
        Assert.True(received.ShiftKey);
        Assert.False(received.AltKey);
    }

    [Fact]
    public void OnRowClick_FromTheKeyboard_ReportsNoModifier()
    {
        OmniDataGridRowMouseEventArgs<Row>? received = null;
        var grid = RenderGrid(onClick: args => received = args);

        grid.FindAll("tbody tr[data-omni-row-index]")[0].KeyDown("Enter");

        Assert.NotNull(received);
        Assert.Equal(1, received!.Item.Id);
        Assert.False(received.CtrlKey);
        Assert.False(received.ShiftKey);
    }

    [Fact]
    public void OnRowContextMenu_ReportsTheRightClickedRow()
    {
        OmniDataGridRowMouseEventArgs<Row>? received = null;
        var grid = RenderGrid(onContextMenu: args => received = args);

        grid.FindAll("tbody tr[data-omni-row-index]")[2].ContextMenu(new MouseEventArgs { ClientX = 40, ClientY = 60 });

        Assert.NotNull(received);
        Assert.Equal(3, received!.Item.Id);
        Assert.Equal(40, received.ClientX);
        Assert.Equal(60, received.ClientY);
    }

    [Fact]
    public void OnRowDoubleClick_CarriesTheRowAndTheModifierKeys()
    {
        OmniDataGridRowMouseEventArgs<Row>? received = null;
        var grid = RenderGrid(onDoubleClick: args => received = args);

        grid.FindAll("tbody tr[data-omni-row-index]")[2].DoubleClick(new MouseEventArgs { AltKey = true });

        Assert.NotNull(received);
        Assert.Equal(3, received!.Item.Id);
        Assert.Equal(2, received.Index);
        Assert.True(received.AltKey);
    }

    [Fact]
    public void WithoutHandlers_RowsKeepNoClickOrContextMenuHandler()
    {
        var grid = RenderGrid();
        var row = grid.FindAll("tbody tr[data-omni-row-index]")[0];

        Assert.Throws<MissingEventHandlerException>(() => row.Click());
        Assert.Throws<MissingEventHandlerException>(() => row.ContextMenu());
    }
}
