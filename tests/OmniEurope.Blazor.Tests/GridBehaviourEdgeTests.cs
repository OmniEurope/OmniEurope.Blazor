using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// OmniDataGrid behaviours its other suites leave: a saved state that cannot be read or saved, widths in
/// every unit, keyboard resizing from widths the grid has to guess, filters set from code for keys with
/// and without a column, single expansion and selection, rows that cannot be selected, a loader taken
/// away, a reload without a loader, and new rows highlighted after a refresh.
/// </summary>
public sealed class GridBehaviourEdgeTests : OmniBunitContext
{
    public sealed record Row(string Name, int Amount);

    private static readonly IReadOnlyList<Row> Rows = [new("a", 1), new("b", 2), new("c", 3)];

    private sealed class Store(Func<string?> load, Exception? saveFailure = null) : IOmniDataGridStateStore
    {
        public List<string> Saved { get; } = [];

        public Task<string?> LoadAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(load());

        public Task SaveAsync(string key, string state, CancellationToken cancellationToken = default)
        {
            if (saveFailure is not null)
            {
                return Task.FromException(saveFailure);
            }

            Saved.Add(state);
            return Task.CompletedTask;
        }
    }

    private static RenderFragment Columns(params (string Property, string? Width)[] columns) => builder =>
    {
        for (var index = 0; index < columns.Length; index++)
        {
            builder.OpenComponent<OmniDataGridColumn<Row>>(index * 10);
            builder.AddComponentParameter(index * 10 + 1, nameof(OmniDataGridColumn<Row>.Property), columns[index].Property);
            builder.AddComponentParameter(index * 10 + 2, nameof(OmniDataGridColumn<Row>.Width), columns[index].Width);
            builder.CloseComponent();
        }
    };

    private IRenderedComponent<OmniDataGrid<Row>> RenderGrid(Action<ComponentParameterCollectionBuilder<OmniDataGrid<Row>>>? extra = null, RenderFragment? columns = null) =>
        Render<OmniDataGrid<Row>>(parameters =>
        {
            parameters
                .Add(component => component.Items, Rows)
                .Add(component => component.KeyOf, row => row.Name)
                .Add(component => component.Columns, columns ?? Columns(("Name", null), ("Amount", null)));
            extra?.Invoke(parameters);
        });

    [Theory]
    [InlineData("pas du json")]
    [InlineData("null")]
    public void SavedStateThatCannotBeRead_IsIgnored(string json)
    {
        var grid = RenderGrid(parameters => parameters
            .Add(component => component.StateKey, "factures")
            .Add(component => component.StateStore, new Store(() => json)));

        Assert.Equal(3, grid.FindAll("tbody tr").Count);
    }

    [Fact]
    public void SavedState_RestoresWidths_AndASaveOnALostCircuitIsQuiet()
    {
        var module = JSInterop.SetupModule(OmniModules.Grid);
        module.Mode = JSRuntimeMode.Loose;
        var store = new Store(() => """{"ColumnWidths":{"Name":"150px"}}""", new JSDisconnectedException("perdu"));
        var grid = RenderGrid(parameters => parameters
            .Add(component => component.StateKey, "factures")
            .Add(component => component.StateStore, store));

        grid.Find("th[data-omni-col='Amount']").Click();

        Assert.Contains("150px", string.Join(' ', module.Invocations["applyColumns"].Select(call => call.Arguments[2]?.ToString())), StringComparison.Ordinal);
    }

    [Fact]
    public void SavedState_LostCircuitWhileLoading_LeavesTheGridAsDeclared()
    {
        var grid = RenderGrid(parameters => parameters
            .Add(component => component.StateKey, "factures")
            .Add(component => component.StateStore, new Store(() => throw new JSDisconnectedException("perdu"))));

        Assert.Equal(3, grid.FindAll("tbody tr").Count);
    }

    [Theory]
    [InlineData("10rem", "calc(10rem)")]
    [InlineData("12ch", "calc(12ch)")]
    [InlineData("5vw", "calc(5vw)")]
    [InlineData("10px", "calc(10px)")]
    [InlineData("2em", "calc(2em)")]
    [InlineData("10pt", "calc(10pt)")]
    [InlineData("50%", "calc(var(--omni-data-grid-column-min-width))")]
    [InlineData("12xx", "calc(var(--omni-data-grid-column-min-width))")]
    [InlineData("px", "calc(var(--omni-data-grid-column-min-width))")]
    public void TableMinimumWidth_AddsOnlyAbsoluteWidths(string width, string expected)
    {
        var module = JSInterop.SetupModule(OmniModules.Grid);
        module.Mode = JSRuntimeMode.Loose;
        RenderGrid(columns: Columns(("Name", width)));

        Assert.Equal(expected, module.Invocations["applyColumns"][^1].Arguments[2]);
    }

    [Fact]
    public void TableMinimumWidth_WithEveryColumnHidden_IsNothing()
    {
        var module = JSInterop.SetupModule(OmniModules.Grid);
        module.Mode = JSRuntimeMode.Loose;
        var grid = RenderGrid(columns: Columns(("Name", "10rem")));

        grid.Render(parameters => parameters.Add(component => component.Columns, builder =>
        {
            builder.OpenComponent<OmniDataGridColumn<Row>>(0);
            builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Property), "Name");
            builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Visible), false);
            builder.CloseComponent();
        }));

        Assert.Equal("0px", module.Invocations["applyColumns"][^1].Arguments[2]);
    }

    [Theory]
    [InlineData("12pt", "ArrowLeft", "48px")]
    [InlineData("1vw", "ArrowRight", "192px")]
    [InlineData("abc", "ArrowLeft", "128px")]
    [InlineData("120", "ArrowLeft", "88px")]
    [InlineData("120px", "ArrowRight", "152px")]
    [InlineData(null, "ArrowLeft", "128px")]
    public void KeyboardResize_FromAWidthToGuess_StepsFromTheEstimate(string? width, string key, string expected)
    {
        var changes = new List<OmniDataGridColumnWidthChange>();
        var grid = RenderGrid(
            parameters => parameters.Add(component => component.OnColumnResize, change => changes.Add(change)),
            Columns(("Name", width)));

        grid.Find(".omni-data-grid__resize-handle").KeyDown(key);

        Assert.Equal(expected, Assert.Single(changes).Width);
    }

    [Fact]
    public async Task FiltersSetFromCode_RemoveWithAnEmptyValue_AndFilterAKeyWithoutAColumn()
    {
        var grid = RenderGrid();

        await grid.InvokeAsync(() => grid.Instance.SetFiltersAsync(new Dictionary<string, string?> { ["Name"] = "a", ["Ville"] = "Liège" }));
        Assert.Single(grid.FindAll("tbody tr"));
        await grid.InvokeAsync(() => grid.Instance.SetFiltersAsync(new Dictionary<string, string?> { ["Name"] = "" }));

        Assert.Equal(3, grid.FindAll("tbody tr").Count);
    }

    [Fact]
    public void SingleExpansionAndSelection_KeepOneRow_AndAnUnselectableRowStaysOut()
    {
        RenderFragment<Row> detail = row => builder => builder.AddContent(0, row.Name);
        var expanded = new List<IReadOnlyList<object>>();
        var selected = new List<IReadOnlyList<Row>>();
        var collapsed = new List<Row>();
        var grid = RenderGrid(parameters => parameters
            .Add(component => component.DetailTemplate, detail)
            .Add(component => component.ExpandMode, OmniDataGridRowMode.Single)
            .Add(component => component.ExpandedKeysChanged, keys => expanded.Add(keys))
            .Add(component => component.OnRowCollapse, row => collapsed.Add(row))
            .Add(component => component.SelectionMode, OmniDataGridSelectionMode.Single)
            .Add(component => component.AllowRowSelectOnRowClick, true)
            .Add(component => component.ValueChanged, rows => selected.Add(rows))
            .Add(component => component.RowRender, args => args.Selectable = args.Item.Name != "c"));

        var toggles = grid.FindAll("[data-omni-control=expand] button");
        toggles[0].Click();
        grid.Render(parameters => parameters.Add(component => component.ExpandedKeys, expanded[^1]));
        grid.FindAll("[data-omni-control=expand] button")[1].Click();
        Assert.Equal(["b"], expanded[^1]);
        grid.Render(parameters => parameters.Add(component => component.ExpandedKeys, expanded[^1]));
        grid.FindAll("[data-omni-control=expand] button")[1].Click();
        Assert.Equal(["b"], collapsed.Select(row => row.Name));

        grid.FindAll("tbody tr[data-omni-row-index]")[0].Click();
        grid.Render(parameters => parameters.Add(component => component.Value, selected[^1]));
        grid.FindAll("tbody tr[data-omni-row-index]")[1].Click();
        Assert.Equal(["b"], selected[^1].Select(row => row.Name));
        grid.FindAll("tbody tr[data-omni-row-index]")[2].Click();
        Assert.Equal(2, selected.Count);
    }

    [Fact]
    public async Task LoaderTakenAway_ShowsTheItems_AndAReloadWithoutLoaderDoesNothing()
    {
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Load, request => Task.FromResult(new OmniDataGridResult<Row>([new Row("distant", 9)], 1)))
            .Add(component => component.Columns, Columns(("Name", null))));
        grid.WaitForAssertion(() => Assert.Contains("distant", grid.Markup, StringComparison.Ordinal));

        grid.Render(parameters => parameters.Add(component => component.Load, null).Add(component => component.Items, Rows));
        await grid.InvokeAsync(grid.Instance.ReloadAsync);

        Assert.DoesNotContain("distant", grid.Markup, StringComparison.Ordinal);
        Assert.Equal(3, grid.FindAll("tbody tr").Count);
    }

    [Fact]
    public async Task RefreshOfHostRows_MarksTheNewOnesAsNew()
    {
        var grid = RenderGrid(parameters => parameters.Add(component => component.NewRowHighlight, TimeSpan.FromMinutes(1)));

        await grid.InvokeAsync(grid.Instance.RefreshAsync);
        grid.Render(parameters => parameters.Add(component => component.Items, [.. Rows, new Row("d", 4)]));

        Assert.Single(grid.FindAll("tr.omni-data-grid__row--new"));

        // A refresh that brings nothing new marks nothing more.
        await grid.InvokeAsync(grid.Instance.RefreshAsync);
        grid.Render(parameters => parameters.Add(component => component.Items, [.. Rows, new Row("d", 4)]));

        Assert.Single(grid.FindAll("tr.omni-data-grid__row--new"));
    }

    [Fact]
    public async Task VirtualRemoteRefresh_OvertakenByANewerOne_KeepsTheNewerRows()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        IReadOnlyList<Row> current = Rows;
        TaskCompletionSource<OmniDataGridResult<Row>>? hold = null;
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Load, _ =>
            {
                if (hold is { } held)
                {
                    hold = null;
                    return held.Task;
                }

                return Task.FromResult(new OmniDataGridResult<Row>(current, current.Count));
            })
            .Add(component => component.KeyOf, row => row.Name)
            .Add(component => component.ScrollMode, OmniDataGridScrollMode.Virtual)
            .Add(component => component.EstimatedRowHeight, 40d)
            .Add(component => component.Columns, Columns(("Name", null), ("Amount", null))));
        grid.WaitForAssertion(() => Assert.Equal(3, grid.FindAll("tbody tr[data-omni-row-index]").Count));

        var slow = new TaskCompletionSource<OmniDataGridResult<Row>>();
        hold = slow;
        var overtaken = grid.InvokeAsync(grid.Instance.RefreshAsync);
        current = [new Row("neuf", 9)];
        await grid.InvokeAsync(grid.Instance.RefreshAsync);
        slow.SetResult(new OmniDataGridResult<Row>([new Row("vieux", 0)], 1));
        await overtaken;

        Assert.Contains("neuf", grid.Find("tbody").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("vieux", grid.Find("tbody").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void PagingSummary_OfAnEmptyGrid_CountsFromNothing()
    {
        var grid = RenderGrid(parameters => parameters.Add(component => component.ShowPagingSummary, true));
        Assert.Equal("1 à 3 sur 3", grid.Find(".omni-data-grid__summary").TextContent.Trim());

        grid.Render(parameters => parameters.Add(component => component.Items, Array.Empty<Row>()));

        Assert.Equal("0 à 0 sur 0", grid.Find(".omni-data-grid__summary").TextContent.Trim());
    }

    [Fact]
    public async Task RefreshOfHostRows_WithAZeroHighlight_MarksNothing()
    {
        var grid = RenderGrid(parameters => parameters.Add(component => component.NewRowHighlight, TimeSpan.Zero));

        await grid.InvokeAsync(grid.Instance.RefreshAsync);
        grid.Render(parameters => parameters.Add(component => component.Items, [.. Rows, new Row("d", 4)]));

        Assert.Equal(4, grid.FindAll("tbody tr").Count);
        Assert.Empty(grid.FindAll("tr.omni-data-grid__row--new"));
    }
}
