using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A virtualized grid with content sizing that fetches blocks (<c>Load</c>): each block hands the script only
/// the texts it does not hold yet, to be added to its own, instead of the texts of every block fetched so far
/// again. Measured before the change with 20 000 rows, 3 columns of distinct texts and blocks of 100 rows: a
/// payload growing from 6 KB to 130 KB per block and 1.8 MB over the scroll, now 6 KB per block.
/// </summary>
public sealed class DataGridContentTextsBlocksTests : OmniBunitContext
{
    private const int BlockSize = 100;
    private const int RowHeight = 40;

    private BunitJSModuleInterop? _module;

    private BunitJSModuleInterop Module
    {
        get
        {
            if (_module is null)
            {
                _module = JSInterop.SetupModule(OmniModules.Grid);
                _module.Mode = JSRuntimeMode.Loose;
            }

            return _module;
        }
    }

    private IReadOnlyList<JSRuntimeInvocation> Pushes => Module.Invocations["setContentTexts"];

    [Fact]
    public async Task ABlockHandsOverOnlyItsNewTexts_ToBeAddedToTheOnesTheScriptHolds()
    {
        var grid = RenderBlocks(nameof(Row.A), nameof(Row.Fixed), nameof(Row.C));
        var first = LastPush();
        Assert.False(first.Append);
        Assert.Equal(BlockSize, first.Texts[nameof(Row.A)].Length);

        await ScrollToBlockAsync(grid, 1);

        var push = LastPush();
        Assert.True(push.Append);
        // The 100 rows of the second block, nothing of the first one, and nothing for the column whose text never changes.
        Assert.Equal(Enumerable.Range(BlockSize, BlockSize).Select(id => $"A{id:D6}"), push.Texts[nameof(Row.A)]);
        Assert.Equal(Enumerable.Range(BlockSize, BlockSize).Select(id => $"C{id:D6}"), push.Texts[nameof(Row.C)]);
        Assert.DoesNotContain(nameof(Row.Fixed), push.Texts.Keys);
    }

    [Fact]
    public async Task ABlockWithNothingNewHandsOverNothing()
    {
        var grid = RenderBlocks(nameof(Row.Fixed));
        var pushes = Pushes.Count;

        await ScrollToBlockAsync(grid, 1);
        await ScrollToBlockAsync(grid, 2);

        Assert.Equal(pushes, Pushes.Count);
    }

    [Fact]
    public async Task ARenderOfTheSameRows_HandsOverNothing()
    {
        var grid = RenderBlocks(nameof(Row.A));
        await ScrollToBlockAsync(grid, 1);
        var pushes = Pushes.Count;

        grid.Render();
        await ScrollToBlockAsync(grid, 1);

        Assert.Equal(pushes, Pushes.Count);
    }

    [Fact]
    public async Task AColumnTheReaderSizes_HandsEveryTextOfTheOthersOverAgain_NotAnAddition()
    {
        var grid = RenderBlocks(nameof(Row.A), nameof(Row.Fixed), nameof(Row.C));
        await ScrollToBlockAsync(grid, 1);
        Assert.True(LastPush().Append);

        await grid.InvokeAsync(() => grid.Instance.OnColumnResizedAsync(nameof(Row.C), 123d));

        var push = LastPush();
        Assert.False(push.Append);
        Assert.DoesNotContain(nameof(Row.C), push.Texts.Keys);
        // Both blocks, the fixed text once: the script replaces what it held.
        Assert.Equal(2 * BlockSize, push.Texts[nameof(Row.A)].Length);
        Assert.Single(push.Texts[nameof(Row.Fixed)]);
    }

    [Fact]
    public async Task TheTextsTheScriptHolds_AreBounded_ByHandingOverTheCurrentSetAgain()
    {
        const int blockSize = 1000;
        var grid = RenderBlocks(blockSize, nameof(Row.Growing));
        var appended = 0;
        var replaced = 0;

        // Every block brings texts longer than the ones before, so each enters the longest 5000 a fit receives.
        for (var block = 1; block <= 12; block++)
        {
            var before = Pushes.Count;
            await ScrollToBlockAsync(grid, block, blockSize);
            Assert.Equal(before + 1, Pushes.Count);
            var push = LastPush();
            Assert.InRange(push.Texts[nameof(Row.Growing)].Length, 1, GridColumnLayout<Row>.MaximumFitTexts);
            if (push.Append)
            {
                appended++;
            }
            else
            {
                replaced++;
                Assert.Equal(GridColumnLayout<Row>.MaximumFitTexts, push.Texts[nameof(Row.Growing)].Length);
            }
        }

        // Added to while the script holds at most twice a fit's texts, replaced once past it.
        Assert.True(appended >= 8, $"{appended} additions");
        Assert.True(replaced >= 1, $"{replaced} replacements");
    }

    [Fact]
    public async Task ASortOfTheSameRows_HandsOverNothing()
    {
        var module = Module;
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.ColumnSizing, OmniDataGridColumnSizing.Content)
            .Add(component => component.Items, Enumerable.Range(0, 30).Select(id => new Row(id)).ToArray())
            .Add(component => component.Columns, (RenderFragment)(builder =>
            {
                builder.OpenComponent<OmniDataGridColumn<Row>>(0);
                builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.A));
                builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Sortable), true);
                builder.CloseComponent();
            })));
        Assert.Single(module.Invocations["setContentTexts"]);

        await grid.InvokeAsync(() => grid.Find(".omni-data-grid__sort").Click());
        await grid.InvokeAsync(() => grid.Find(".omni-data-grid__sort").Click());

        Assert.Single(module.Invocations["setContentTexts"]);
    }

    private IRenderedComponent<OmniDataGrid<Row>> RenderBlocks(params string[] properties) => RenderBlocks(BlockSize, properties);

    private IRenderedComponent<OmniDataGrid<Row>> RenderBlocks(int blockSize, params string[] properties)
    {
        _ = Module;
        return Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.ScrollMode, OmniDataGridScrollMode.Virtual)
            .Add(component => component.ColumnSizing, OmniDataGridColumnSizing.Content)
            .Add(component => component.EstimatedRowHeight, (double)RowHeight)
            .Add(component => component.VirtualBlockSize, blockSize)
            .Add(component => component.Columns, (RenderFragment)(builder =>
            {
                var sequence = 0;
                foreach (var property in properties)
                {
                    builder.OpenComponent<OmniDataGridColumn<Row>>(sequence++);
                    builder.AddComponentParameter(sequence++, nameof(OmniDataGridColumn<Row>.Property), property);
                    builder.CloseComponent();
                }
            }))
            .Add(component => component.Load, request => Task.FromResult(new OmniDataGridResult<Row>(
                Enumerable.Range(request.Skip, Math.Min(request.Top, 20_000 - request.Skip)).Select(id => new Row(id)).ToArray(),
                20_000))));
    }

    private static Task ScrollToBlockAsync(IRenderedComponent<OmniDataGrid<Row>> grid, int block, int blockSize = BlockSize) =>
        grid.InvokeAsync(() => grid.Instance.OnViewportChangedAsync(block * blockSize * (double)RowHeight, 400d));

    private (IReadOnlyDictionary<string, string[]> Texts, bool Append) LastPush()
    {
        var call = Pushes[^1];
        return (Assert.IsAssignableFrom<IReadOnlyDictionary<string, string[]>>(call.Arguments[1]), Assert.IsType<bool>(call.Arguments[2]));
    }

    private sealed record Row(int Id)
    {
        public string A => $"A{Id:D6}";

        public string Fixed => "same everywhere";

        public string C => $"C{Id:D6}";

        public string Growing => $"{Id:D6}{new string('x', Id / 50)}";
    }
}
