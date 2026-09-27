using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// RowMouseClick and RowContextMenu: a host that selects with Ctrl or Shift and opens its own menu on
/// a right-click needs the modifier keys and the row, which RowClick alone does not carry.
/// </summary>
public sealed class DataGridRowMouseTests : OmniBunitContext
{
    private sealed record Row(int Id, string Name);

    private static readonly IReadOnlyList<Row> Rows = [new(1, "Un"), new(2, "Deux"), new(3, "Trois")];

    private IRenderedComponent<OmniDataGrid<Row>> RenderGrid(
        Action<OmniDataGridRowMouseEventArgs<Row>>? onClick = null,
        Action<OmniDataGridRowMouseEventArgs<Row>>? onContextMenu = null)
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<OmniDataGridColumn<Row>>(0);
            builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Name));
            builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Title), "Nom");
            builder.CloseComponent();
        };

        return Render<OmniDataGrid<Row>>(parameters =>
        {
            parameters.Add(grid => grid.Items, Rows).Add(grid => grid.Columns, columns);
            if (onClick is not null)
                parameters.Add(grid => grid.RowMouseClick, onClick);
            if (onContextMenu is not null)
                parameters.Add(grid => grid.RowContextMenu, onContextMenu);
        });
    }

    [Fact]
    public void RowMouseClick_CarriesTheRowAndTheModifierKeys()
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
    public void RowMouseClick_FromTheKeyboard_ReportsNoModifier()
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
    public void RowContextMenu_ReportsTheRightClickedRow()
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
    public void WithoutHandlers_RowsKeepNoClickOrContextMenuHandler()
    {
        var grid = RenderGrid();
        var row = grid.FindAll("tbody tr[data-omni-row-index]")[0];

        Assert.Throws<MissingEventHandlerException>(() => row.Click());
        Assert.Throws<MissingEventHandlerException>(() => row.ContextMenu());
    }
}
