using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Disposing a remote grid: a <see cref="OmniDataGrid{TItem}.Load"/> that never completes must not
/// keep the grid from being disposed, whether it loads pages or virtual blocks.
/// </summary>
public sealed class DataGridLoadLifecycleTests : OmniBunitContext
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Dispose_WithALoadThatNeverCompletes_CancelsItAndReturns(bool virtualized)
    {
        var pending = new TaskCompletionSource<OmniDataGridResult<int>>();
        CancellationToken observed = default;
        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.AllowVirtualization, virtualized)
            .Add(component => component.EstimatedRowHeight, 40d)
            .Add(component => component.Load, request =>
            {
                observed = request.CancellationToken;
                // Ignores its token on purpose: the grid must not depend on the loader honouring it.
                return pending.Task;
            }));

        var dispose = grid.InvokeAsync(() => grid.Instance.DisposeAsync().AsTask());
        var finished = await Task.WhenAny(dispose, Task.Delay(Patience, Xunit.TestContext.Current.CancellationToken));

        Assert.Same(dispose, finished);
        await dispose;
        Assert.True(observed.IsCancellationRequested);
    }
}
