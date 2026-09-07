using Bunit;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

public sealed class DataGridComponentTests : OmniBunitContext
{
    [Fact]
    public void DataGridColumn_ReregistersWhenItsParametersChange()
    {
        var grid = Render<DataGridTestHost>();

        grid.InvokeAsync(grid.Instance.RenameColumn);

        grid.WaitForAssertion(() => Assert.Contains("Nom complet", grid.Find("thead").TextContent, StringComparison.Ordinal));
    }

    [Fact]
    public void DataGrid_SortsByTheRequestedColumn()
    {
        var grid = Render<DataGridTestHost>();
        grid.WaitForAssertion(() => Assert.Equal(2, grid.FindAll("tbody tr").Count));

        grid.FindAll(".omni-data-grid__sort")[1].Click();
        Assert.Contains("Alice", grid.FindAll("tbody tr")[0].TextContent, StringComparison.Ordinal);
        Assert.Equal("ascending", grid.FindAll("thead th")[2].GetAttribute("aria-sort"));

    }

    [Fact]
    public void DataGrid_CyclesAColumnThroughAscendingDescendingAndUnsorted()
    {
        var grid = Render<DataGridTestHost>();
        grid.WaitForAssertion(() => Assert.Equal(2, grid.FindAll("tbody tr").Count));

        grid.FindAll(".omni-data-grid__sort")[1].Click();
        Assert.Equal("ascending", grid.FindAll("thead th")[2].GetAttribute("aria-sort"));

        grid.FindAll(".omni-data-grid__sort")[1].Click();
        Assert.Equal("descending", grid.FindAll("thead th")[2].GetAttribute("aria-sort"));

        // Third click drops the sort entirely instead of looping back to ascending, so the grid can
        // be returned to its natural order.
        grid.FindAll(".omni-data-grid__sort")[1].Click();
        Assert.Null(grid.FindAll("thead th")[2].GetAttribute("aria-sort"));
        // The indicator keeps its slot on every sortable column; unsorted it is the hidden idle one.
        Assert.All(
            grid.FindAll(".omni-data-grid__sort-icon"),
            icon => Assert.Contains("omni-data-grid__sort-icon--idle", icon.ClassName, StringComparison.Ordinal));
    }

    [Fact]
    public void DataGrid_SelectsByStableKey()
    {
        var grid = Render<DataGridTestHost>();

        grid.Find("tbody input[type=checkbox]").Change(true);

        Assert.Equal([1], grid.Instance.SelectedKeys);
    }

    [Fact]
    public void DataGrid_FiltersRows()
    {
        var grid = Render<DataGridTestHost>();

        grid.Find(".omni-data-grid__filter").Input("bo");
        Assert.Single(grid.FindAll("tbody tr"));
        Assert.Contains("Bob", grid.Find("tbody tr").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("style=", grid.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DataGrid_PaginatesToTheNextSubset()
    {
        var grid = Render<DataGridTestHost>();

        grid.Find(".omni-pager button[aria-label=\"Page suivante\"]").Click();

        Assert.Equal(2, grid.Instance.Page);
        Assert.Single(grid.FindAll("tbody tr"));
        Assert.Contains("Bob", grid.Find("tbody tr").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void DataGrid_GroupsRowsAndExpandsDetails()
    {
        var grid = Render<DataGridAdvancedTestHost>();

        Assert.Equal(2, grid.FindAll(".omni-data-grid__group").Count);
        Assert.Contains("A (2)", grid.Markup, StringComparison.Ordinal);
        grid.Find(".omni-data-grid__expand").Click();

        Assert.Equal([1], grid.Instance.ExpandedKeys);
        Assert.Contains("Détail Alice", grid.Find(".omni-data-grid__detail").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void DataGrid_TransitionsThroughEditAndResizeCallbacks()
    {
        var grid = Render<DataGridAdvancedTestHost>();

        grid.Find(".omni-data-grid__actions button").Click();
        Assert.Equal(1, grid.Instance.EditingId);
        Assert.Single(grid.FindAll(".advanced-edit"));
        grid.FindAll(".omni-data-grid__actions button")[0].Click();
        Assert.Equal("Alice", grid.Instance.UpdatedName);

        // The pointer drag itself lives in omni-grid.js, out of bUnit's reach; the arrow-key path on
        // the same handle goes through the identical .NET width-change code.
        grid.FindAll(".omni-data-grid__resize-handle")[0].KeyDown("ArrowRight");
        Assert.Equal("name", grid.Instance.WidthChange?.Key);
        Assert.Equal("192px", grid.Instance.WidthChange?.Width);
    }

    [Fact]
    public void DataGrid_ReprojectsFilteringSortingAndPagingAsOneConsistentView()
    {
        var grid = Render<DataGridTestHost>();
        grid.Instance.Page = 2;
        grid.Render();
        grid.WaitForAssertion(() => Assert.Contains("Bob", grid.Find("tbody").TextContent, StringComparison.Ordinal));

        grid.Find(".omni-data-grid__filter").Input("ali");

        Assert.Equal(1, grid.Instance.Page);
        Assert.Single(grid.FindAll("tbody tr"));
        Assert.Contains("Alice", grid.Find("tbody tr").TextContent, StringComparison.Ordinal);
        Assert.Empty(grid.FindAll(".omni-pager"));
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
    public void DataGrid_TreatsAnEmptyRemoteResultAsLoadedAndReloadsWhenTheDelegateChanges()
    {
        var firstCalls = 0;
        var secondCalls = 0;
        Func<OmniDataGridLoadRequest, Task<OmniDataGridResult<int>>> first = _ =>
        {
            firstCalls++;
            return Task.FromResult(new OmniDataGridResult<int>([], 0));
        };
        Func<OmniDataGridLoadRequest, Task<OmniDataGridResult<int>>> second = _ =>
        {
            secondCalls++;
            return Task.FromResult(new OmniDataGridResult<int>([], 0));
        };

        var grid = Render<OmniDataGrid<int>>(parameters => parameters.Add(component => component.Load, first));
        grid.Render(parameters => parameters.Add(component => component.Load, first));
        Assert.Equal(1, firstCalls);

        grid.Render(parameters => parameters.Add(component => component.Load, second));
        Assert.Equal(1, secondCalls);
        Assert.Contains("Aucune donnée.", grid.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void DataGrid_RemovingAColumnPurgesItsRemoteFilterAndSort()
    {
        var host = Render<DynamicDataGridTestHost>();
        host.Find(".omni-data-grid__filter").Input("ali");
        host.Find(".omni-data-grid__sort").Click();

        Assert.Contains(host.Instance.Requests[^1].Filters, filter => filter.Key == "name");
        Assert.Contains(host.Instance.Requests[^1].Sorts, sort => sort.Key == "name");

        host.InvokeAsync(host.Instance.RemoveNameColumn);

        host.WaitForAssertion(() =>
        {
            var request = host.Instance.Requests[^1];
            Assert.DoesNotContain(request.Filters, filter => filter.Key == "name");
            Assert.DoesNotContain(request.Sorts, sort => sort.Key == "name");
            Assert.DoesNotContain("Nom", host.Find("thead").TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task DataGrid_IgnoresAnOlderRemoteRequestThatCompletesLast()
    {
        var stale = new TaskCompletionSource<OmniDataGridResult<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var latest = new TaskCompletionSource<OmniDataGridResult<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callCount = 0;
        CancellationToken staleToken = default;
        CancellationToken latestToken = default;
        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Load, request =>
            {
                callCount++;
                if (callCount == 1) return Task.FromResult(new OmniDataGridResult<int>([0], 1));
                if (callCount == 2) { staleToken = request.CancellationToken; return stale.Task; }
                latestToken = request.CancellationToken;
                return latest.Task;
            }));

        var staleReload = grid.InvokeAsync(() => grid.Instance.ReloadAsync());
        Assert.Equal(2, callCount);
        var latestReload = grid.InvokeAsync(() => grid.Instance.ReloadAsync());
        Assert.Equal(3, callCount);
        Assert.True(staleToken.IsCancellationRequested);
        Assert.False(latestToken.IsCancellationRequested);

        latest.SetResult(new OmniDataGridResult<int>([2], 1));
        await latestReload;
        stale.SetResult(new OmniDataGridResult<int>([1, 1], 2));
        await staleReload;
        grid.Render(parameters => { });

        var row = Assert.Single(grid.FindAll("tbody tr"));
        Assert.Contains("2", row.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("0", row.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("1", row.TextContent, StringComparison.Ordinal);
        Assert.Equal(3, callCount);
    }

    [Fact]
    public void DataGrid_ReportsARecoverableLocalizedLoadFailureAndRetries()
    {
        var calls = 0;
        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Load, _ =>
            {
                calls++;
                return calls == 1
                    ? Task.FromException<OmniDataGridResult<int>>(new InvalidOperationException("sensitive details"))
                    : Task.FromResult(new OmniDataGridResult<int>([7], 1));
            }));

        grid.WaitForAssertion(() =>
        {
            Assert.Equal("alert", grid.Find(".omni-data-grid__state").GetAttribute("role"));
            Assert.Contains("Le chargement de la grille a échoué.", grid.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("sensitive details", grid.Markup, StringComparison.Ordinal);
        });

        grid.Find(".omni-data-grid__state button").Click();

        grid.WaitForAssertion(() =>
        {
            Assert.Equal(2, calls);
            Assert.Contains("7", Assert.Single(grid.FindAll("tbody tr")).TextContent, StringComparison.Ordinal);
            Assert.Empty(grid.FindAll("[role=alert]"));
        });
    }

    [Fact]
    public async Task GridRemoteState_IgnoresAStaleFailureAfterANewerSuccess()
    {
        await using var state = new GridRemoteState<int>();
        var stale = new TaskCompletionSource<OmniDataGridResult<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var staleLoad = state.LoadAsync(_ => stale.Task);

        await state.LoadAsync(_ => Task.FromResult(new OmniDataGridResult<int>([9], 1)));
        stale.SetException(new InvalidOperationException("obsolete"));
        await staleLoad;

        Assert.Equal([9], state.Items);
        Assert.Null(state.Error);
        Assert.False(state.Loading);
    }

    [Fact]
    public void GridProjection_FiltersSortsAndPagesWithoutRendering()
    {
        var columns = new[]
        {
            new OmniDataGridColumnDefinition<GridRow>
            {
                Key = "name",
                Title = "Name",
                Value = row => row.Name,
                Filterable = true,
                FilterOperator = OmniDataGridFilterOperator.Contains
            },
            new OmniDataGridColumnDefinition<GridRow>
            {
                Key = "score",
                Title = "Score",
                Value = row => row.Score
            }
        };

        var result = GridProjection<GridRow>.Create(
            [new("Alice", 20), new("Bob", 10), new("Aline", 30)],
            columns,
            new Dictionary<string, GridColumnFilter>
            {
                ["name"] = GridColumnFilter.Empty with { Operator = OmniDataGridFilterOperator.Contains, Value = "ali" }
            },
            [new OmniDataGridSort("score", true)],
            OmniDataGridFilterCaseSensitivity.Default,
            ignoreDiacritics: false,
            page: 1,
            pageSize: 1);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal("Aline", Assert.Single(result.Items).Name);
    }

    [Fact]
    public async Task GridRemoteState_KeepsOnlyTheLatestGeneration()
    {
        await using var state = new GridRemoteState<int>();
        var stale = new TaskCompletionSource<OmniDataGridResult<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var staleLoad = state.LoadAsync(_ => stale.Task);
        await state.LoadAsync(_ => Task.FromResult(new OmniDataGridResult<int>([2], 1)));
        stale.SetResult(new OmniDataGridResult<int>([1, 1], 2));
        await staleLoad;

        Assert.Equal([2], state.Items);
        Assert.Equal(1, state.TotalCount);
        Assert.False(state.Loading);
        Assert.Null(state.Error);
        Assert.True(state.HasLoaded);
    }

    private sealed record GridRow(string Name, int Score);
}
