using System.Globalization;
using Bunit;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

public sealed class DataGridVirtualizationTests : OmniBunitContext
{
    [Fact]
    public void VirtualWindow_PlacesRowsOnTheEstimateUntilTheyAreMeasured()
    {
        var window = new GridVirtualWindow();
        window.Configure(1_000, 40d);

        Assert.Equal(40_000d, window.TotalHeight);
        Assert.Equal(400d, window.OffsetOf(10));
        Assert.Equal(10, window.IndexAt(400d));
        Assert.Equal(10, window.IndexAt(439d));
        Assert.Equal(11, window.IndexAt(440d));
    }

    [Fact]
    public void VirtualWindow_ShiftsEveryLaterRowWhenOneRowIsMeasuredTaller()
    {
        var window = new GridVirtualWindow();
        window.Configure(100, 40d);

        Assert.True(window.Measure(2, 140d));

        Assert.Equal(4_100d, window.TotalHeight);
        Assert.Equal(80d, window.OffsetOf(2));
        Assert.Equal(220d, window.OffsetOf(3));
        Assert.Equal(140d, window.HeightOf(2));
        Assert.Equal(40d, window.HeightOf(3));
        Assert.Equal(2, window.IndexAt(219d));
        Assert.Equal(3, window.IndexAt(220d));
    }

    [Fact]
    public void VirtualWindow_AccumulatesSeveralDifferentRowHeights()
    {
        var window = new GridVirtualWindow();
        window.Configure(6, 20d);
        window.Measure(0, 10d);
        window.Measure(1, 50d);
        window.Measure(4, 30d);

        Assert.Equal(10d + 50d + 20d + 20d + 30d + 20d, window.TotalHeight);
        Assert.Equal(60d, window.OffsetOf(2));
        Assert.Equal(100d, window.OffsetOf(4));
        Assert.Equal(4, window.IndexAt(100d));
    }

    [Fact]
    public void VirtualWindow_IgnoresAMeasurementThatDoesNotMoveTheLayout()
    {
        var window = new GridVirtualWindow();
        window.Configure(10, 40d);

        Assert.True(window.Measure(3, 55d));
        Assert.False(window.Measure(3, 55.2d));
        Assert.False(window.Measure(-1, 80d));
        Assert.False(window.Measure(3, 0d));
        Assert.False(window.Measure(3, double.NaN));
        Assert.Equal(55d, window.HeightOf(3));
    }

    [Fact]
    public void VirtualWindow_ForgetsMeasurementsOnReset()
    {
        var window = new GridVirtualWindow();
        window.Configure(10, 40d);
        window.Measure(0, 120d);

        window.ResetMeasurements();

        Assert.Equal(400d, window.TotalHeight);
        Assert.False(window.IsMeasured(0));
    }

    [Fact]
    public void VirtualWindow_ComputesTheVisibleRangeWithOverscanAndSpacers()
    {
        var window = new GridVirtualWindow();
        window.Configure(1_000, 40d);

        var range = window.Compute(scrollTop: 4_000d, viewportHeight: 400d, overscan: 2);

        Assert.Equal(98, range.StartIndex);
        Assert.Equal(3_920d, range.TopSpacer);
        Assert.True(range.Count >= 10, $"Range too small: {range.Count}");
        Assert.Equal(window.TotalHeight - window.OffsetOf(range.EndIndex), range.BottomSpacer);
        Assert.Equal(window.TotalHeight, range.TopSpacer + SpannedHeight(window, range) + range.BottomSpacer);
    }

    [Fact]
    public void VirtualWindow_ClampsAnOverscrolledPosition()
    {
        var window = new GridVirtualWindow();
        window.Configure(20, 40d);

        var range = window.Compute(scrollTop: 100_000d, viewportHeight: 200d, overscan: 0);

        Assert.True(range.EndIndex <= 20);
        Assert.Equal(0d, range.BottomSpacer);
    }

    [Fact]
    public void VirtualWindow_HandlesAnEmptyRowSet()
    {
        var window = new GridVirtualWindow();
        window.Configure(0, 40d);

        var range = window.Compute(0d, 400d, 4);

        Assert.Equal(0, range.Count);
        Assert.Equal(0d, range.TopSpacer);
        Assert.Equal(0d, range.BottomSpacer);
    }

    [Fact]
    public async Task VirtualDataSource_FetchesOnlyTheBlocksCoveringTheRequestedRange()
    {
        var requested = new List<(int Skip, int Take)>();
        await using var source = new GridVirtualDataSource<int>();

        var changed = await source.EnsureRangeAsync(120, 10, 50, (skip, take, _) =>
        {
            requested.Add((skip, take));
            return Task.FromResult(new OmniDataGridResult<int>(Enumerable.Range(skip, take).ToArray(), 10_000));
        });

        Assert.True(changed);
        Assert.Equal([(100, 50)], requested);
        Assert.Equal(10_000, source.TotalCount);
        Assert.True(source.TryGet(120, out var item));
        Assert.Equal(120, item);
        Assert.False(source.TryGet(200, out _));
    }

    [Fact]
    public async Task VirtualDataSource_DoesNotRefetchACachedBlock()
    {
        var calls = 0;
        await using var source = new GridVirtualDataSource<int>();
        Task<OmniDataGridResult<int>> Loader(int skip, int take, CancellationToken _)
        {
            calls++;
            return Task.FromResult(new OmniDataGridResult<int>(Enumerable.Range(skip, take).ToArray(), 500));
        }

        await source.EnsureRangeAsync(0, 10, 50, Loader);
        var changed = await source.EnsureRangeAsync(5, 10, 50, Loader);

        Assert.Equal(1, calls);
        Assert.False(changed);
    }

    [Fact]
    public async Task VirtualDataSource_CapturesALoaderFailureAndForgetsItOnReset()
    {
        await using var source = new GridVirtualDataSource<int>();

        await source.EnsureRangeAsync(0, 10, 50, (_, _, _) =>
            Task.FromException<OmniDataGridResult<int>>(new InvalidOperationException("remote refused")));

        Assert.NotNull(source.Error);

        source.Reset();

        Assert.Null(source.Error);
        Assert.Equal(0, source.CachedItemCount);
    }

    [Fact]
    public async Task VirtualDataSource_KeepsTheCacheBoundedWhileScrollingForward()
    {
        await using var source = new GridVirtualDataSource<int>();
        Task<OmniDataGridResult<int>> Loader(int skip, int take, CancellationToken _) =>
            Task.FromResult(new OmniDataGridResult<int>(Enumerable.Range(skip, take).ToArray(), 1_000_000));

        for (var block = 0; block < 200; block++)
        {
            await source.EnsureRangeAsync(block * 50, 50, 50, Loader);
        }

        Assert.True(source.CachedItemCount <= 24 * 50, $"Cache grew to {source.CachedItemCount} items.");
        Assert.True(source.TryGet(199 * 50, out _));
    }

    [Fact]
    public void VirtualizedGrid_RendersOnlyAWindowOfAVeryLargeLocalCollection()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var items = Enumerable.Range(0, 100_000).ToArray();

        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, items)
            .Add(component => component.AllowVirtualization, true)
            .Add(component => component.EstimatedRowHeight, 40d)
            .Add(component => component.VirtualizationOverscanCount, 2));

        var rows = grid.FindAll("tbody tr:not(.omni-data-grid__spacer)");
        Assert.InRange(rows.Count, 1, 64);
        Assert.Equal("100000", grid.Find("table").GetAttribute("aria-rowcount"));
        Assert.Equal(2, grid.FindAll(".omni-data-grid__spacer").Count);
        Assert.Contains("omni-data-grid__viewport--virtual", grid.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("style=", grid.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VirtualizedGrid_MovesItsWindowWhenTheViewportScrolls()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var items = Enumerable.Range(0, 10_000).ToArray();
        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, items)
            .Add(component => component.AllowVirtualization, true)
            .Add(component => component.EstimatedRowHeight, 40d));

        Assert.Contains(">0</td>", grid.Markup, StringComparison.Ordinal);

        await grid.InvokeAsync(() => grid.Instance.OnViewportChangedAsync(40_000d, 400d));

        Assert.DoesNotContain(">0</td>", grid.Markup, StringComparison.Ordinal);
        Assert.Contains(">1000</td>", grid.Markup, StringComparison.Ordinal);
        // The default overscan of 4 keeps four rows above the viewport rendered.
        Assert.Equal("997", grid.FindAll("tbody tr[data-omni-row-index]")[0].GetAttribute("aria-rowindex"));
    }

    [Fact]
    public async Task VirtualizedGrid_LoadsFurtherWindowsFromTheRemoteLoaderWhileScrolling()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var requests = new List<OmniDataGridLoadRequest>();

        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.AllowVirtualization, true)
            .Add(component => component.EstimatedRowHeight, 40d)
            .Add(component => component.VirtualBlockSize, 100)
            .Add(component => component.Load, request =>
            {
                requests.Add(request);
                return Task.FromResult(new OmniDataGridResult<int>(
                    Enumerable.Range(request.Skip, request.Top).ToArray(),
                    50_000));
            }));

        Assert.Single(requests);
        Assert.Equal(0, requests[0].Skip);
        Assert.Equal(100, requests[0].Top);
        Assert.Equal("50000", grid.Find("table").GetAttribute("aria-rowcount"));

        await grid.InvokeAsync(() => grid.Instance.OnViewportChangedAsync(400_000d, 400d));

        Assert.Contains(requests, request => request.Skip == 10_000);
        Assert.Contains(">10000</td>", grid.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Grid_SendsTheTableHeightToTheLayoutInteropAsACssLength()
    {
        var module = JSInterop.SetupModule("./_content/OmniEurope.Blazor/omni-grid.js");

        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, new[] { 1, 2, 3 })
            .Add(component => component.Height, "480px"));

        Assert.Contains("omni-data-grid__viewport--sized", grid.Markup, StringComparison.Ordinal);
        var invocation = Assert.Single(module.Invocations["applyLayout"]);
        Assert.Equal("480px", invocation.Arguments[3]);
        Assert.DoesNotContain("style=", grid.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VirtualizedGrid_SendsTheTableHeightAndTheSpacersTogether()
    {
        var module = JSInterop.SetupModule("./_content/OmniEurope.Blazor/omni-grid.js");

        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, Enumerable.Range(0, 5_000).ToArray())
            .Add(component => component.AllowVirtualization, true)
            .Add(component => component.EstimatedRowHeight, 40d)
            .Add(component => component.Height, "50vh"));

        grid.WaitForAssertion(() =>
        {
            var invocation = module.Invocations["applyLayout"][0];
            Assert.Equal(0d, invocation.Arguments[1]);
            Assert.Equal("50vh", invocation.Arguments[3]);
            Assert.True(
                Convert.ToDouble(invocation.Arguments[2], CultureInfo.InvariantCulture) > 0d,
                "The bottom spacer must stand in for the rows below the window.");
        });
    }

    [Fact]
    public void VirtualizedGrid_RefusesGroupingAndDetailRows()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var grouping = Assert.Throws<InvalidOperationException>(() => Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, new[] { 1, 2 })
            .Add(component => component.AllowVirtualization, true)
            .Add(component => component.GroupBy, item => (object?)item)));

        Assert.Contains("GroupBy", grouping.Message, StringComparison.Ordinal);
    }

    private static double SpannedHeight(GridVirtualWindow window, GridVirtualRange range) =>
        window.OffsetOf(range.EndIndex) - window.OffsetOf(range.StartIndex);
}
