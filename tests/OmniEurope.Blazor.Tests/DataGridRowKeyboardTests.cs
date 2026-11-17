using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The keyboard between the rows of a grid whose rows the host listens to: a roving tabindex (one tab
/// stop, the row that last took the focus), Enter and Space and the tree arrows unchanged. The move itself
/// (Up, Down, Home, End, a key in a control left alone, the page not scrolling) is made by the page script
/// and is proved in a real browser by the showcase probe, not here: bUnit runs no script.
/// </summary>
public sealed class DataGridRowKeyboardTests : OmniBunitContext
{
    private const string FocusModule = "./_content/OmniEurope.Blazor/omni-focus.js";

    private sealed record Row(int Id, string Name, IReadOnlyList<Row>? Children = null);

    private static readonly IReadOnlyList<Row> Rows = [new(1, "Un"), new(2, "Deux"), new(3, "Trois")];

    private static RenderFragment Columns() => builder =>
    {
        builder.OpenComponent<OmniDataGridColumn<Row>>(0);
        builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Name));
        builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Title), "Nom");
        builder.AddComponentParameter(3, nameof(OmniDataGridColumn<Row>.Template), (RenderFragment<Row>)(row => cell =>
        {
            cell.OpenElement(0, "button");
            cell.AddAttribute(1, "type", "button");
            cell.AddContent(2, row.Name);
            cell.CloseElement();
        }));
        builder.CloseComponent();
    };

    private IRenderedComponent<OmniDataGrid<Row>> RenderGrid(
        Action<OmniDataGridRowMouseEventArgs<Row>>? onClick = null,
        IReadOnlyList<Row>? items = null,
        Action<ComponentParameterCollectionBuilder<OmniDataGrid<Row>>>? configure = null) =>
        Render<OmniDataGrid<Row>>(parameters =>
        {
            parameters.Add(grid => grid.Items, items ?? Rows).Add(grid => grid.Columns, Columns()).Add(grid => grid.KeyOf, row => row.Id);
            if (onClick is not null)
            {
                parameters.Add(grid => grid.OnRowClick, onClick);
            }

            configure?.Invoke(parameters);
        });

    private static IReadOnlyList<AngleSharp.Dom.IElement> RowElements(IRenderedComponent<OmniDataGrid<Row>> grid) =>
        grid.FindAll("tbody tr[data-omni-row-index]");

    private static string?[] TabIndexes(IRenderedComponent<OmniDataGrid<Row>> grid) =>
        RowElements(grid).Select(row => row.GetAttribute("tabindex")).ToArray();

    [Fact]
    public void InteractiveRows_AreOneTabStop_TheFirstRowAtFirst()
    {
        var grid = RenderGrid(onClick: _ => { });

        Assert.Equal(["0", "-1", "-1"], TabIndexes(grid));
        Assert.All(RowElements(grid), row => Assert.Equal("true", row.GetAttribute("data-omni-row-nav")));
    }

    [Fact]
    public void AGridWhoseRowsAreNotInteractive_HasNoTabStopAndNoRowKeys()
    {
        var grid = RenderGrid();

        Assert.Equal([null, null, null], TabIndexes(grid));
        Assert.All(RowElements(grid), row => Assert.False(row.HasAttribute("data-omni-row-nav")));
        Assert.Empty(grid.FindAll("[data-omni-row-nav]"));
    }

    [Fact]
    public void TheFocusEnteringARow_MakesItTheTabStop_AndTheOthersLeaveTheTabOrder()
    {
        var grid = RenderGrid(onClick: _ => { });

        RowElements(grid)[1].FocusIn();
        Assert.Equal(["-1", "0", "-1"], TabIndexes(grid));

        RowElements(grid)[2].FocusIn();
        Assert.Equal(["-1", "-1", "0"], TabIndexes(grid));

        // Home takes the focus back to the first row: it becomes the tab stop again.
        RowElements(grid)[0].FocusIn();
        Assert.Equal(["0", "-1", "-1"], TabIndexes(grid));
    }

    [Fact]
    public void TheFocusEnteringAControlOfARow_MakesThatRowTheTabStop()
    {
        var grid = RenderGrid(onClick: _ => { });

        RowElements(grid)[2].QuerySelector("button")!.FocusIn();

        Assert.Equal(["-1", "-1", "0"], TabIndexes(grid));
        // The cell's own control is not a row to move to: the script moves between the marked rows only.
        Assert.Empty(grid.FindAll("tbody td [data-omni-row-nav]"));
    }

    [Fact]
    public void TheFocusOnTheTabStopAgain_ChangesNothing()
    {
        var grid = RenderGrid(onClick: _ => { });

        RowElements(grid)[0].FocusIn();
        RowElements(grid)[0].FocusIn();

        Assert.Equal(["0", "-1", "-1"], TabIndexes(grid));
    }

    [Fact]
    public void ARowTheHostMadeNotSelectable_IsNeitherTheFirstTabStopNorAStopAtAll()
    {
        var grid = RenderGrid(onClick: _ => { }, configure: parameters => parameters.Add(g => g.RowRender, args => args.Selectable = args.Item.Id != 1));

        Assert.Equal([null, "0", "-1"], TabIndexes(grid));
        Assert.False(RowElements(grid)[0].HasAttribute("data-omni-row-nav"));

        // Nothing listens to the focus entering it: the tab stop cannot move there.
        Assert.Throws<Bunit.MissingEventHandlerException>(() => RowElements(grid)[0].FocusIn());
        Assert.Equal([null, "0", "-1"], TabIndexes(grid));
    }

    [Fact]
    public void TheTabStopRowLeavingTheScreen_GivesTheStopBackToTheFirstRow()
    {
        var grid = RenderGrid(onClick: _ => { });
        RowElements(grid)[2].FocusIn();
        Assert.Equal(["-1", "-1", "0"], TabIndexes(grid));

        grid.Render(parameters => parameters.Add(g => g.Items, Rows.Take(2).ToArray()));

        Assert.Equal(["0", "-1"], TabIndexes(grid));
    }

    [Fact]
    public void TheFirstFocusOfARow_TurnsOnThePageKeysOnce()
    {
        var module = JSInterop.SetupModule(FocusModule);
        module.SetupVoid("enablePageKeys");
        var grid = RenderGrid(onClick: _ => { });

        Assert.Empty(module.Invocations["enablePageKeys"]);

        RowElements(grid)[0].FocusIn();
        RowElements(grid)[1].FocusIn();

        Assert.Single(module.Invocations["enablePageKeys"]);
    }

    [Fact]
    public void EnterAndSpace_StillRaiseOnRowClick_AndTheVerticalKeysDoNotClick()
    {
        var clicked = new List<int>();
        var grid = RenderGrid(onClick: args => clicked.Add(args.Item.Id));

        RowElements(grid)[1].KeyDown("Enter");
        RowElements(grid)[2].KeyDown(" ");
        foreach (var key in new[] { "ArrowDown", "ArrowUp", "Home", "End" })
        {
            RowElements(grid)[1].KeyDown(key);
        }

        Assert.Equal([2, 3], clicked);
        Assert.Equal(["0", "-1", "-1"], TabIndexes(grid));
    }

    [Fact]
    public void TreeRows_KeepTheirArrowsAndTheirTabStop()
    {
        IReadOnlyList<Row> tree = [new(1, "Un", [new(11, "Un-un")]), new(2, "Deux")];
        var grid = RenderGrid(items: tree, configure: parameters => parameters
            .Add(g => g.ChildrenOf, row => row.Children)
            .Add(g => g.ToggleTreeOnRowClick, true));

        Assert.Equal(["0", "-1"], TabIndexes(grid));

        RowElements(grid)[0].KeyDown("ArrowRight");
        Assert.Equal(3, RowElements(grid).Count);
        Assert.Equal(["0", "-1", "-1"], TabIndexes(grid));

        RowElements(grid)[1].FocusIn();
        RowElements(grid)[0].KeyDown("ArrowLeft");
        Assert.Equal(2, RowElements(grid).Count);
        // The focused row left with the closed branch: the stop goes back to the first row.
        Assert.Equal(["0", "-1"], TabIndexes(grid));
    }
}
