using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A virtualized grid backed by a loader asks each block once, even when the window is asked again
/// while that block is still on its way (reported by a client application: two identical requests after every
/// header sort or filter).
/// </summary>
public sealed class DataGridVirtualLoadDedupTests : OmniBunitContext
{
    [Fact]
    public async Task A_block_asked_again_while_it_loads_is_fetched_once()
    {
        var calls = 0;
        var answer = new TaskCompletionSource<OmniDataGridResult<int>>();
        await using var source = new GridVirtualDataSource<int>();
        Task<OmniDataGridResult<int>> Loader(int skip, int take, CancellationToken _)
        {
            calls++;
            return answer.Task;
        }

        var first = source.EnsureRangeAsync(0, 10, 50, Loader);
        var second = source.EnsureRangeAsync(0, 10, 50, Loader);
        answer.SetResult(new OmniDataGridResult<int>(Enumerable.Range(0, 50).ToArray(), 500));
        await Task.WhenAll(first, second);

        Assert.Equal(1, calls);
        Assert.True(source.TryGet(5, out var item));
        Assert.Equal(5, item);
        Assert.False(source.Loading);
    }

    [Fact]
    public async Task A_reset_while_a_block_loads_lets_the_next_query_fetch_it_again()
    {
        var calls = 0;
        var stale = new TaskCompletionSource<OmniDataGridResult<int>>();
        await using var source = new GridVirtualDataSource<int>();

        var first = source.EnsureRangeAsync(0, 10, 50, (_, _, _) => { calls++; return stale.Task; });
        source.Reset();
        await source.EnsureRangeAsync(0, 10, 50, (skip, take, _) =>
        {
            calls++;
            return Task.FromResult(new OmniDataGridResult<int>(Enumerable.Range(100, take).ToArray(), 500));
        });
        stale.SetResult(new OmniDataGridResult<int>(Enumerable.Range(0, 50).ToArray(), 500));
        await first;

        Assert.Equal(2, calls);
        Assert.True(source.TryGet(0, out var item));
        Assert.Equal(100, item);
    }

    [Fact]
    public void A_header_sort_on_a_virtual_remote_grid_loads_once()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var requests = new List<OmniDataGridLoadRequest>();
        var pending = new List<TaskCompletionSource<OmniDataGridResult<Row>>>();

        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.ScrollMode, OmniDataGridScrollMode.Virtual)
            .Add(component => component.EstimatedRowHeight, 40d)
            .Add(component => component.FillAvailableHeight, true)
            .Add(component => component.Columns, (RenderFragment)(builder =>
            {
                builder.OpenComponent<OmniDataGridColumn<Row>>(0);
                builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Name));
                builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Title), "Nom");
                builder.AddComponentParameter(3, nameof(OmniDataGridColumn<Row>.Sortable), true);
                builder.CloseComponent();
            }))
            .Add(component => component.Load, request =>
            {
                requests.Add(request);
                var answer = new TaskCompletionSource<OmniDataGridResult<Row>>();
                pending.Add(answer);
                return answer.Task;
            }));

        grid.WaitForAssertion(() => Assert.Single(requests));
        grid.InvokeAsync(() => pending[0].SetResult(new OmniDataGridResult<Row>([new Row("Alice"), new Row("Bob")], 2)));
        grid.WaitForAssertion(() => Assert.Contains("Alice", grid.Find("tbody").TextContent, StringComparison.Ordinal));

        grid.Find(".omni-data-grid__sort").Click();

        grid.WaitForAssertion(() => Assert.Equal(2, requests.Count));
        Assert.Single(requests[1].Sorts);
        // Every render the sort causes (loading bar, after-render window sync) asks the window again
        // while the block is still on its way: none of them may send a second request.
        grid.Render();
        Assert.Equal(2, requests.Count);

        grid.InvokeAsync(() => pending[1].SetResult(new OmniDataGridResult<Row>([new Row("Bob"), new Row("Alice")], 2)));
        grid.WaitForAssertion(() =>
        {
            var body = grid.Find("tbody").TextContent;
            Assert.True(body.IndexOf("Bob", StringComparison.Ordinal) is >= 0 and var bob && bob < body.IndexOf("Alice", StringComparison.Ordinal), body);
        });
        Assert.Equal(2, requests.Count);
    }

    private sealed record Row(string Name);
}
