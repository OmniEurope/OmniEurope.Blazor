using Bunit;
using OmniEurope.Blazor.Components;
using Row = OmniEurope.Blazor.Tests.DataGridInitialLoadTestHost.Row;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The first request of a <see cref="OmniDataGrid{TItem}.Load"/> grid carries the defaults its columns
/// declare (a <c>DefaultFilterValue</c>, a <c>SortOrder</c>): the columns register while the grid
/// renders, and a request sent before that opened the grid on rows that did not match the filter its
/// header showed as active.
/// </summary>
public sealed class DataGridInitialLoadTests : OmniBunitContext
{
    private static readonly Row[] Rows = [new(1, "Alpha", "Open"), new(2, "Bravo", "Closed"), new(3, "Charlie", "Open")];

    [Theory]
    [InlineData(OmniDataGridScrollMode.Paged, false)]
    [InlineData(OmniDataGridScrollMode.Paged, true)]
    [InlineData(OmniDataGridScrollMode.Virtual, false)]
    [InlineData(OmniDataGridScrollMode.Virtual, true)]
    public void FirstRequest_CarriesTheColumnDefaults_AndIsTheOnlyOne(OmniDataGridScrollMode mode, bool slowLoader)
    {
        var requests = new List<OmniDataGridLoadRequest>();
        var grid = Render<DataGridInitialLoadTestHost>(parameters => parameters
            .Add(component => component.ScrollMode, mode)
            .Add(component => component.Load, request => Answer(requests, request, slowLoader)));

        grid.WaitForAssertion(() => Assert.Equal(2, grid.FindAll("tbody tr[data-omni-row-index]").Count));
        var request = Assert.Single(requests);
        var filter = Assert.Single(request.Filters);
        Assert.Equal("Status", filter.Key);
        Assert.Equal(OmniDataGridFilterOperator.Equals, filter.Operator);
        Assert.Equal("Open", filter.Value);
        var sort = Assert.Single(request.Sorts);
        Assert.Equal("Name", sort.Key);
        Assert.True(sort.Descending);
        Assert.DoesNotContain("Bravo", grid.Find("tbody").TextContent, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(OmniDataGridScrollMode.Paged)]
    [InlineData(OmniDataGridScrollMode.Virtual)]
    public void FirstRequest_WaitsForTheSavedStateStillBeingRead_AndCarriesIt(OmniDataGridScrollMode mode)
    {
        // The store answers after the grid first rendered: the columns registered their defaults, but
        // the saved filter (Closed) wins once read, and only then is the request sent.
        const string saved = "{\"Filters\":{\"Status\":{\"Operator\":1,\"Value\":\"Closed\",\"LogicalOperator\":0,\"SecondOperator\":1,\"SecondValue\":\"\"}},\"Sorts\":[],\"ColumnWidths\":{}}";
        var requests = new List<OmniDataGridLoadRequest>();
        var grid = Render<DataGridInitialLoadTestHost>(parameters => parameters
            .Add(component => component.ScrollMode, mode)
            .Add(component => component.StateKey, "tickets")
            .Add(component => component.StateStore, new SlowStore(saved))
            .Add(component => component.Load, request => Answer(requests, request, slow: false)));

        grid.WaitForAssertion(() => Assert.Single(grid.FindAll("tbody tr[data-omni-row-index]")));
        var request = Assert.Single(requests);
        var filter = Assert.Single(request.Filters);
        Assert.Equal("Status", filter.Key);
        Assert.Equal("Closed", filter.Value);
    }

    private static async Task<OmniDataGridResult<Row>> Answer(List<OmniDataGridLoadRequest> requests, OmniDataGridLoadRequest request, bool slow)
    {
        requests.Add(request);
        if (slow)
        {
            await Task.Delay(20, request.CancellationToken);
        }

        var matching = Rows
            .Where(row => request.Filters.All(filter => filter.Key != "Status" || row.Status == filter.Value))
            .ToArray();
        return new OmniDataGridResult<Row>(matching, matching.Length);
    }

    private sealed class SlowStore(string state) : IOmniDataGridStateStore
    {
        public async Task<string?> LoadAsync(string key, CancellationToken cancellationToken = default)
        {
            await Task.Delay(20, cancellationToken);
            return state;
        }

        public Task SaveAsync(string key, string state, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
