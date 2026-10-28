using Microsoft.AspNetCore.Components;
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

    [Fact]
    public Task A_window_that_moved_on_while_a_block_loaded_does_not_ask_for_the_next_one() =>
        // The grid drives its source from the renderer's dispatcher, one continuation at a time. Run there
        // too: on the thread pool the two waiting windows resumed at once and wrote the cache together.
        Dispatcher.CreateDefault().InvokeAsync(AWindowThatMovedOnDoesNotAskForTheNextOneAsync);

    private static async Task AWindowThatMovedOnDoesNotAskForTheNextOneAsync()
    {
        await using var source = new GridVirtualDataSource<int>();
        var asked = new List<int>();
        var answers = new Dictionary<int, TaskCompletionSource<OmniDataGridResult<int>>>();
        Task<OmniDataGridResult<int>> Loader(int skip, int take, CancellationToken token)
        {
            asked.Add(skip);
            var answer = new TaskCompletionSource<OmniDataGridResult<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
            answers[skip] = answer;
            return answer.Task;
        }

        var wide = source.EnsureRangeAsync(0, 100, 50, Loader);
        var narrow = source.EnsureRangeAsync(0, 10, 50, Loader);
        answers[0].SetResult(Block(0, 50));
        await Task.WhenAll(wide, narrow);

        Assert.Equal([0], asked);
        Assert.True(source.TryGet(5, out _));
        Assert.Null(source.Error);
    }

    [Fact]
    public async Task Scrolling_back_up_cancels_the_blocks_below_the_window()
    {
        await using var source = new GridVirtualDataSource<int>();
        var tokens = new Dictionary<int, CancellationToken>();
        Task<OmniDataGridResult<int>> Loader(int skip, int take, CancellationToken token)
        {
            tokens[skip] = token;
            return skip == 0 ? Task.FromResult(Block(skip, take)) : new TaskCompletionSource<OmniDataGridResult<int>>().Task;
        }

        var below = source.EnsureRangeAsync(5_000, 10, 50, Loader);
        await source.EnsureRangeAsync(0, 10, 50, Loader);

        Assert.True(tokens[5_000].IsCancellationRequested);
        Assert.False(await below);
        Assert.True(source.TryGet(5, out _));
    }

    [Fact]
    public async Task A_loader_that_throws_before_returning_a_task_is_reported_as_an_error_and_leaves_nothing_loading()
    {
        await using var source = new GridVirtualDataSource<int>();

        var changed = await source.EnsureRangeAsync(0, 10, 50,
            (_, _, _) => throw new InvalidOperationException("loader failed"));

        Assert.True(changed);
        Assert.IsType<InvalidOperationException>(source.Error);
        Assert.False(source.Loading);
    }
}
