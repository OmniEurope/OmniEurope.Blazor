using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A fast scroll over a remote virtual grid must not leave blocks that left the viewport loading:
/// they are cancelled, and only the blocks of the latest window stay in flight.
/// </summary>
public sealed class DataGridVirtualCancellationTests
{
    private static OmniDataGridResult<int> Block(int skip, int take) =>
        new(Enumerable.Range(skip, take).ToArray(), 100_000);

    [Fact]
    public async Task A_block_that_left_the_window_is_cancelled_when_the_window_moves()
    {
        await using var source = new GridVirtualDataSource<int>();
        var tokens = new Dictionary<int, CancellationToken>();
        var answers = new Dictionary<int, TaskCompletionSource<OmniDataGridResult<int>>>();
        Task<OmniDataGridResult<int>> Loader(int skip, int take, CancellationToken token)
        {
            tokens[skip] = token;
            var answer = new TaskCompletionSource<OmniDataGridResult<int>>();
            answers[skip] = answer;
            return answer.Task;
        }

        var first = source.EnsureRangeAsync(0, 10, 50, Loader);
        Assert.False(tokens[0].IsCancellationRequested);

        var second = source.EnsureRangeAsync(5_000, 10, 50, Loader);

        Assert.True(tokens[0].IsCancellationRequested);
        Assert.False(tokens[5_000].IsCancellationRequested);
        Assert.False(await first);
        answers[5_000].SetResult(Block(5_000, 50));
        Assert.True(await second);
        Assert.True(source.TryGet(5_005, out var item));
        Assert.Equal(5_005, item);
        Assert.False(source.TryGet(5, out _));
        Assert.Null(source.Error);
        Assert.False(source.Loading);
    }

    [Fact]
    public async Task A_cancelled_block_that_ignores_its_token_is_not_cached_nor_reported_as_an_error()
    {
        await using var source = new GridVirtualDataSource<int>();
        var stubborn = new TaskCompletionSource<OmniDataGridResult<int>>();
        Task<OmniDataGridResult<int>> Loader(int skip, int take, CancellationToken token) =>
            skip == 0 ? stubborn.Task : Task.FromResult(Block(skip, take));

        var first = source.EnsureRangeAsync(0, 10, 50, Loader);
        await source.EnsureRangeAsync(5_000, 10, 50, Loader);
        stubborn.SetResult(Block(0, 50));
        await first;

        Assert.False(source.TryGet(5, out _));
        Assert.Null(source.Error);
        Assert.False(source.Loading);
    }

    [Fact]
    public async Task A_window_wider_than_one_block_keeps_the_blocks_it_still_covers()
    {
        await using var source = new GridVirtualDataSource<int>();
        var tokens = new Dictionary<int, CancellationToken>();
        var answers = new Dictionary<int, TaskCompletionSource<OmniDataGridResult<int>>>();
        Task<OmniDataGridResult<int>> Loader(int skip, int take, CancellationToken token)
        {
            tokens[skip] = token;
            var answer = new TaskCompletionSource<OmniDataGridResult<int>>();
            answers[skip] = answer;
            return answer.Task;
        }

        var first = source.EnsureRangeAsync(0, 80, 50, Loader);
        var second = source.EnsureRangeAsync(40, 80, 50, Loader);

        Assert.False(tokens[0].IsCancellationRequested);
        answers[0].SetResult(Block(0, 50));
        await Task.Yield();
        answers[50].SetResult(Block(50, 50));
        answers[100].SetResult(Block(100, 50));
        await Task.WhenAll(first, second);

        Assert.True(source.TryGet(0, out _));
        Assert.True(source.TryGet(120, out _));
        Assert.Null(source.Error);
    }
}
