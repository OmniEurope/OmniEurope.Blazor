using Bunit;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// What the virtualized grid takes from the snapshots its script sends: the row height of the stylesheet,
/// the scroll, the measured rows (ignored when every row has the fixed height), and a scroll request
/// to a row.
/// </summary>
public sealed class DataGridViewportTests : OmniBunitContext
{
    private static readonly int[] Many = [.. Enumerable.Range(0, 10_000)];

    private (IRenderedComponent<OmniDataGrid<int>> Grid, BunitJSModuleInterop Module) RenderGrid(GridViewportSnapshot snapshot, bool fixedHeight = false)
    {
        var module = JSInterop.SetupModule(OmniModules.Grid);
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<GridViewportSnapshot?>("sync", _ => true).SetResult(snapshot);
        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, Many)
            .Add(component => component.ScrollMode, OmniDataGridScrollMode.Virtual)
            .Add(component => component.FixedRowHeight, fixedHeight)
            .Add(component => component.EstimatedRowHeight, 40d));
        return (grid, module);
    }

    private static string FirstRow(IRenderedComponent<OmniDataGrid<int>> grid) =>
        grid.FindAll("tbody tr[data-omni-row-index]")[0].GetAttribute("aria-rowindex")!;

    [Fact]
    public void ScrollOfTheSnapshot_MovesTheWindow()
    {
        var (grid, _) = RenderGrid(new GridViewportSnapshot { ScrollTop = 40_000, ViewportHeight = 400 });

        grid.WaitForAssertion(() => Assert.Contains(">1000</td>", grid.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void MeasuredRows_ChangeTheWindow_UnlessEveryRowHasTheFixedHeight()
    {
        var measured = new GridViewportSnapshot
        {
            ScrollTop = 4_000,
            ViewportHeight = 400,
            Rows = [.. Enumerable.Range(0, 100).Select(index => new GridRowMeasurement { Index = index, Height = 80 })]
        };

        var (free, _) = RenderGrid(measured);
        // Rows of 80 instead of 40: 4 000 px down reaches row 50, not row 100.
        free.WaitForAssertion(() => Assert.Contains(">50</td>", free.Markup, StringComparison.Ordinal));

        var (fixedGrid, _) = RenderGrid(measured, fixedHeight: true);
        fixedGrid.WaitForAssertion(() => Assert.Contains(">100</td>", fixedGrid.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void RowEstimate_IsTakenOnce_AndAnEmptyOneIsIgnored()
    {
        var (grid, module) = RenderGrid(new GridViewportSnapshot { ViewportHeight = 400, RowEstimate = 0 });
        var renders = grid.RenderCount;

        module.Setup<GridViewportSnapshot?>("sync", _ => true).SetResult(new GridViewportSnapshot { ViewportHeight = 400, RowEstimate = 32 });
        grid.Render();
        grid.Render();

        Assert.True(grid.RenderCount > renders);
        Assert.Equal("1", FirstRow(grid));
    }

    [Fact]
    public async Task ViewportReportInsideTheSameWindow_LoadsAndRendersNothing()
    {
        var (grid, _) = RenderGrid(new GridViewportSnapshot { ViewportHeight = 400 });
        var renders = grid.RenderCount;

        await grid.InvokeAsync(() => grid.Instance.OnViewportChangedAsync(1, 400));

        Assert.Equal(renders, grid.RenderCount);
    }

    [Fact]
    public async Task ScrollToIndex_AsksTheScriptForTheOffsetOfTheRow()
    {
        var (grid, module) = RenderGrid(new GridViewportSnapshot { ViewportHeight = 400 });

        await grid.InvokeAsync(() => grid.Instance.ScrollToIndexAsync(25));

        Assert.Equal(1_000d, Assert.Single(module.Invocations["scrollToOffset"]).Arguments[1]);
    }
}
