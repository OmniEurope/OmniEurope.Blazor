using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A tree grid (ChildrenOf) with ScrollMode Virtual: the virtual list is the flattened visible nodes, so
/// only the rows of the scroll window are in the DOM, the spacers stand in for the others, and opening
/// or closing a branch changes the total and the window.
/// </summary>
public sealed class DataGridTreeVirtualTests : OmniBunitContext
{
    private const int Roots = 50;
    private const int ChildrenPerRoot = 99;
    private const int Total = Roots * (1 + ChildrenPerRoot);
    private const double RowHeight = 40d;

    private sealed record Node(int Id, string Name, IReadOnlyList<Node>? Children = null);

    // 50 roots of 99 leaves: 5 000 nodes, the root of index r standing at row r * 100 once everything is open.
    private static readonly IReadOnlyList<Node> Forest =
    [
        .. Enumerable.Range(0, Roots).Select(root => new Node(
            root * 100,
            $"root-{root}",
            [.. Enumerable.Range(1, ChildrenPerRoot).Select(child => new Node((root * 100) + child, $"leaf-{root}-{child}"))]))
    ];

    private static RenderFragment Columns() => builder =>
    {
        DataGridTestSetup.PropertyColumn<Node>(builder, 0, nameof(Node.Name), "Nom");
    };

    private IRenderedComponent<OmniDataGrid<Node>> RenderGrid(
        OmniDataGridScrollMode scrollMode = OmniDataGridScrollMode.Virtual,
        Action<ComponentParameterCollectionBuilder<OmniDataGrid<Node>>>? configure = null)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        return Render<OmniDataGrid<Node>>(parameters =>
        {
            parameters.Add(grid => grid.Items, Forest)
                .Add(grid => grid.Columns, Columns())
                .Add(grid => grid.KeyOf, node => node.Id)
                .Add(grid => grid.ChildrenOf, node => node.Children)
                .Add(grid => grid.InitiallyExpanded, _ => true)
                .Add(grid => grid.ScrollMode, scrollMode)
                .Add(grid => grid.EstimatedRowHeight, RowHeight)
                .Add(grid => grid.FixedRowHeight, true);
            configure?.Invoke(parameters);
        });
    }

    private static IReadOnlyList<AngleSharp.Dom.IElement> Rows(IRenderedComponent<OmniDataGrid<Node>> grid) =>
        grid.FindAll("tbody tr[data-omni-row-index]");

    private static double Spacer(IRenderedComponent<OmniDataGrid<Node>> grid, string which) =>
        double.Parse(grid.Find($"[data-omni-spacer='{which}'] td").GetAttribute("height")!, CultureInfo.InvariantCulture);

    private static int RowCount(IRenderedComponent<OmniDataGrid<Node>> grid) =>
        int.Parse(grid.Find("table").GetAttribute("aria-rowcount")!, CultureInfo.InvariantCulture);

    private static string Name(AngleSharp.Dom.IElement row) => row.QuerySelector("td[data-omni-col='Name']")!.TextContent.Trim();

    private static Task ScrollTo(IRenderedComponent<OmniDataGrid<Node>> grid, double scrollTop) =>
        grid.InvokeAsync(() => grid.Instance.OnViewportChangedAsync(scrollTop, 400d));

    [Fact]
    public void FiveThousandOpenNodes_RenderOnlyTheWindow_AndTheSpacersStandInForTheRest()
    {
        var grid = RenderGrid();

        var rows = Rows(grid);
        Assert.InRange(rows.Count, 1, 60);
        Assert.Equal(Total, RowCount(grid));
        Assert.Equal("root-0", Name(rows[0]));
        Assert.Equal("leaf-0-1", Name(rows[1]));
        // Everything that is not rendered is in the spacers: the whole list is 5 000 rows of 40 px.
        Assert.Equal(0d, Spacer(grid, "top"));
        Assert.Equal((Total - rows.Count) * RowHeight, Spacer(grid, "bottom"));
        Assert.Equal(Total * RowHeight, (rows.Count * RowHeight) + Spacer(grid, "top") + Spacer(grid, "bottom"));
    }

    [Fact]
    public void RenderedRows_KeepTheirDepthAndExpanderState_AndTheRowIndexOfTheFlatList()
    {
        var grid = RenderGrid();

        var rows = Rows(grid);
        Assert.Equal("1", rows[0].GetAttribute("aria-level"));
        Assert.Equal("true", rows[0].GetAttribute("aria-expanded"));
        Assert.Equal("0", rows[0].GetAttribute("data-omni-tree-level"));
        Assert.Equal("2", rows[1].GetAttribute("aria-level"));
        Assert.Null(rows[1].GetAttribute("aria-expanded"));
        Assert.Equal("1", rows[1].GetAttribute("data-omni-tree-level"));
        Assert.Equal("treegrid", grid.Find("table").GetAttribute("role"));
        Assert.Equal(["1", "2", "3"], rows.Take(3).Select(row => row.GetAttribute("aria-rowindex")!));
        Assert.Equal(["0", "1", "2"], rows.Take(3).Select(row => row.GetAttribute("data-omni-row-index")!));
        Assert.NotNull(rows[0].QuerySelector(".omni-data-grid__tree-toggle"));
        Assert.NotNull(rows[1].QuerySelector(".omni-data-grid__tree-spacer"));
    }

    [Fact]
    public async Task Scrolling_MovesTheWindowOverTheVisibleNodes_WithTheirOwnLevels()
    {
        var grid = RenderGrid();

        // Row 2 500 is the root 25; the window starts a few rows above it (overscan).
        await ScrollTo(grid, 2_500 * RowHeight);

        var rows = Rows(grid);
        Assert.InRange(rows.Count, 1, 60);
        var root = rows.Single(row => Name(row) == "root-25");
        Assert.Equal("1", root.GetAttribute("aria-level"));
        Assert.Equal("2500", root.GetAttribute("data-omni-row-index"));
        Assert.Equal("2501", root.GetAttribute("aria-rowindex"));
        Assert.Equal("2", rows.Single(row => Name(row) == "leaf-25-1").GetAttribute("aria-level"));
        Assert.DoesNotContain(rows, row => Name(row) == "root-0");
        Assert.Empty(grid.FindAll("tr.omni-data-grid__row--placeholder"));
        Assert.True(Spacer(grid, "top") > 0d);
        Assert.Equal(Total * RowHeight, (rows.Count * RowHeight) + Spacer(grid, "top") + Spacer(grid, "bottom"));
    }

    [Fact]
    public async Task ScrollingToTheBottom_ShowsTheLastNode()
    {
        var grid = RenderGrid();

        await ScrollTo(grid, (Total * RowHeight) - 400d);

        var rows = Rows(grid);
        Assert.Equal("leaf-49-99", Name(rows[^1]));
        Assert.Equal(Total.ToString(CultureInfo.InvariantCulture), rows[^1].GetAttribute("aria-rowindex"));
        Assert.Equal(0d, Spacer(grid, "bottom"));
    }

    [Fact]
    public void ClosingABranch_ShrinksTheTotal_AndOpeningItGrowsItBack()
    {
        var grid = RenderGrid();

        grid.Find("tbody tr[data-omni-row-index='0'] .omni-data-grid__tree-toggle").Click();

        Assert.Equal(Total - ChildrenPerRoot, RowCount(grid));
        Assert.Equal(["root-0", "root-1", "leaf-1-1"], Rows(grid).Take(3).Select(Name));
        Assert.Equal("false", Rows(grid)[0].GetAttribute("aria-expanded"));
        Assert.Equal((RowCount(grid) - Rows(grid).Count) * RowHeight, Spacer(grid, "bottom"));

        grid.Find("tbody tr[data-omni-row-index='0'] .omni-data-grid__tree-toggle").Click();

        Assert.Equal(Total, RowCount(grid));
        Assert.Equal("leaf-0-1", Name(Rows(grid)[1]));
        Assert.Equal((Total - Rows(grid).Count) * RowHeight, Spacer(grid, "bottom"));
    }

    [Fact]
    public async Task CollapsingEverything_AtTheBottomOfTheList_KeepsTheWindowOverTheLastRoots()
    {
        var grid = RenderGrid();
        await ScrollTo(grid, (Total * RowHeight) - 400d);

        await grid.InvokeAsync(() => grid.Instance.CollapseAllTreeRowsAsync());

        // The window was far past the end of the 50 roots: it is clamped to the last ones, never empty.
        var rows = Rows(grid);
        Assert.Equal(Roots, RowCount(grid));
        Assert.InRange(rows.Count, 1, Roots - 1);
        Assert.Equal("root-49", Name(rows[^1]));
        Assert.Empty(grid.FindAll("tr.omni-data-grid__row--placeholder"));
        Assert.Equal(0d, Spacer(grid, "bottom"));
        Assert.Equal(Roots * RowHeight, (rows.Count * RowHeight) + Spacer(grid, "top") + Spacer(grid, "bottom"));

        await grid.InvokeAsync(() => grid.Instance.ExpandAllTreeRowsAsync());

        Assert.Equal(Total, RowCount(grid));
        Assert.InRange(Rows(grid).Count, 1, 60);
    }

    [Fact]
    public void RowClickAndKeys_OpenAndCloseABranchOfTheWindow()
    {
        var grid = RenderGrid(configure: parameters => parameters.Add(g => g.ToggleTreeOnRowClick, true));

        Rows(grid)[0].Click();
        Assert.Equal(Total - ChildrenPerRoot, RowCount(grid));

        Rows(grid)[0].KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.Equal(Total, RowCount(grid));

        Rows(grid)[0].KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.Equal(Total - ChildrenPerRoot, RowCount(grid));
    }

    [Fact]
    public void SelectingARow_ReportsTheNodeOfThatRow()
    {
        IReadOnlyList<Node>? selected = null;
        var grid = RenderGrid(configure: parameters => parameters
            .Add(g => g.SelectionMode, OmniDataGridSelectionMode.Multiple)
            .Add(g => g.ValueChanged, value => selected = value));

        Rows(grid)[1].QuerySelector("input[type='checkbox']")!.Change(true);

        Assert.Equal(["leaf-0-1"], selected!.Select(node => node.Name));
        Assert.Equal("true", Rows(grid)[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public void RowClick_ReportsTheIndexInTheFlatList()
    {
        OmniDataGridRowMouseEventArgs<Node>? clicked = null;
        var grid = RenderGrid(configure: parameters => parameters.Add(g => g.OnRowClick, args => clicked = args));

        Rows(grid)[2].Click();

        Assert.Equal("leaf-0-2", clicked!.Item.Name);
        Assert.Equal(2, clicked.Index);
    }

    [Fact]
    public async Task InteractiveRows_HaveOneTabStop_AmongTheRowsOfTheWindow()
    {
        var grid = RenderGrid(configure: parameters => parameters.Add(g => g.OnRowClick, _ => { }));

        Assert.Equal(1, Rows(grid).Count(row => row.GetAttribute("tabindex") == "0"));
        Assert.Equal("0", Rows(grid)[0].GetAttribute("tabindex"));

        await ScrollTo(grid, 2_500 * RowHeight);

        var rows = Rows(grid);
        Assert.Equal(1, rows.Count(row => row.GetAttribute("tabindex") == "0"));
        Assert.All(rows, row => Assert.NotNull(row.GetAttribute("data-omni-row-nav")));
    }

    [Fact]
    public async Task FilteringATree_KeepsTheMatchesWithTheirAncestors_InTheVirtualList()
    {
        var grid = RenderGrid();
        await ScrollTo(grid, 1_000 * RowHeight);

        await grid.InvokeAsync(() => grid.Instance.SetFiltersAsync(new Dictionary<string, string?> { ["Name"] = "leaf-7-99" }));

        // The matching leaf and its parent, nothing else, from the top of the list.
        Assert.Equal(2, RowCount(grid));
        Assert.Equal(["root-7", "leaf-7-99"], Rows(grid).Select(Name));
        Assert.Equal(["1", "2"], Rows(grid).Select(row => row.GetAttribute("aria-level")!));
    }

    [Fact]
    public async Task OpenDetailRows_InAVirtualTree_StandInTheSlotOfTheirNode()
    {
        var grid = RenderGrid(configure: parameters => parameters
            .Add(g => g.ExpandedKeys, new object[] { 2_501 })
            .Add(g => g.DetailTemplate, node => builder => builder.AddContent(0, $"Détail {node.Name}")));

        Assert.Equal(Total, RowCount(grid));
        Assert.DoesNotContain("Détail leaf-25-1", grid.Markup, StringComparison.Ordinal);

        await ScrollTo(grid, 2_500 * RowHeight);

        Assert.Contains("Détail leaf-25-1", grid.Markup, StringComparison.Ordinal);
        var rows = Rows(grid);
        Assert.Equal("2", rows.Single(row => Name(row) == "leaf-25-1").GetAttribute("aria-level"));
        Assert.Equal("1", rows.Single(row => Name(row) == "root-25").GetAttribute("aria-level"));

        // Closing a branch recomputes the slots over the shorter list.
        grid.Find("tbody tr[data-omni-row-index='2500'] .omni-data-grid__tree-toggle").Click();
        Assert.Equal(Total - ChildrenPerRoot, RowCount(grid));
        Assert.DoesNotContain("Détail leaf-25-1", grid.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void AnEmptyVirtualTree_RendersNoRow_AndAnEmptyRowCount()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var grid = this.RenderDataGrid(Array.Empty<Node>(), Columns(), parameters => parameters
            .Add(g => g.KeyOf, node => node.Id)
            .Add(g => g.ChildrenOf, node => node.Children)
            .Add(g => g.ScrollMode, OmniDataGridScrollMode.Virtual));

        Assert.Empty(Rows(grid));
        Assert.Equal("0", grid.Find("table").GetAttribute("aria-rowcount"));
    }

    [Fact]
    public void ANonVirtualTree_StillRendersEveryNode_WithoutSpacers()
    {
        var grid = RenderGrid(OmniDataGridScrollMode.All);

        Assert.Equal(Total, Rows(grid).Count);
        Assert.Empty(grid.FindAll(".omni-data-grid__spacer"));
        Assert.Null(grid.Find("table").GetAttribute("aria-rowcount"));
        Assert.Equal("leaf-49-99", Name(Rows(grid)[^1]));
    }
}
