using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class DataGridComponentTests : BunitContext
{
    [Fact]
    public void DataGrid_SortsFiltersPagesAndSelectsByStableKey()
    {
        var grid = Render<DataGridTestHost>();
        grid.WaitForAssertion(() => Assert.Equal(2, grid.FindAll("tbody tr").Count));

        grid.FindAll(".omni-data-grid__sort")[1].Click();
        Assert.Contains("Alice", grid.FindAll("tbody tr")[0].TextContent, StringComparison.Ordinal);
        Assert.Equal("ascending", grid.FindAll("thead th")[2].GetAttribute("aria-sort"));

        grid.Find("tbody input[type=checkbox]").Change(true);
        Assert.Equal([2], grid.Instance.SelectedKeys);

        grid.Find(".omni-data-grid__filter").Change("bo");
        Assert.Single(grid.FindAll("tbody tr"));
        Assert.Contains("Bob", grid.Find("tbody tr").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("style=", grid.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DataGrid_UsesCancelableRemoteRequestsAndTotalCount()
    {
        OmniDataGridLoadRequest? received = null;
        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.PageSize, 10)
            .Add(component => component.Load, request =>
            {
                received = request;
                return Task.FromResult(new OmniDataGridResult<int>([1, 2], 25));
            }));

        Assert.NotNull(received);
        Assert.Equal(10, received.PageSize);
        Assert.False(received.CancellationToken.IsCancellationRequested);
        Assert.Contains("Page 1 sur 3", grid.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DataGrid_IgnoresAnOlderRemoteRequestThatCompletesLast()
    {
        var stale = new TaskCompletionSource<OmniDataGridResult<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var latest = new TaskCompletionSource<OmniDataGridResult<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callCount = 0;
        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Load, _ => ++callCount switch
            {
                1 => Task.FromResult(new OmniDataGridResult<int>([0], 1)),
                2 => stale.Task,
                _ => latest.Task
            }));

        var staleReload = grid.InvokeAsync(() => grid.Instance.ReloadAsync());
        Assert.Equal(2, callCount);
        var latestReload = grid.InvokeAsync(() => grid.Instance.ReloadAsync());
        Assert.Equal(3, callCount);

        latest.SetResult(new OmniDataGridResult<int>([2], 1));
        await latestReload;
        stale.SetResult(new OmniDataGridResult<int>([1, 1], 2));
        await staleReload;
        grid.Render(parameters => { });

        Assert.Single(grid.FindAll("tbody tr"));
    }
}
