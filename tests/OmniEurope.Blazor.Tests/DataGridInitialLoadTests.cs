using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

    [Theory]
    [InlineData(OmniDataGridScrollMode.Paged)]
    [InlineData(OmniDataGridScrollMode.Virtual)]
    public void ColumnRenderedAfterTheFirstRequest_LoadsTheRowsAgainWithItsDefault(OmniDataGridScrollMode mode)
    {
        // The column that declares the default filter is under a condition and appears once the grid
        // has loaded: the rows on screen were loaded without its filter, so they are loaded again.
        var requests = new List<OmniDataGridLoadRequest>();
        var grid = Render<DataGridInitialLoadTestHost>(parameters => parameters
            .Add(component => component.ScrollMode, mode)
            .Add(component => component.ShowStatus, false)
            .Add(component => component.Load, request => Answer(requests, request, slow: false)));
        grid.WaitForAssertion(() => Assert.Equal(3, grid.FindAll("tbody tr[data-omni-row-index]").Count));
        Assert.Empty(Assert.Single(requests).Filters);

        grid.Render(parameters => parameters.Add(component => component.ShowStatus, true));

        grid.WaitForAssertion(() => Assert.Equal(2, grid.FindAll("tbody tr[data-omni-row-index]").Count));
        Assert.Equal(2, requests.Count);
        var filter = Assert.Single(requests[1].Filters);
        Assert.Equal("Status", filter.Key);
        Assert.Equal("Open", filter.Value);
        Assert.DoesNotContain("Bravo", grid.Find("tbody").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void SavedSortReadAtOnce_WinsOverTheColumnDefault()
    {
        // The store answers before the columns register: the Name column finds its key already sorted
        // (ascending, saved) and does not add its own descending default.
        const string saved = "{\"Filters\":{},\"Sorts\":[{\"Key\":\"Name\",\"Descending\":false}],\"ColumnWidths\":{}}";
        var requests = new List<OmniDataGridLoadRequest>();
        var grid = Render<DataGridInitialLoadTestHost>(parameters => parameters
            .Add(component => component.StateKey, "tickets")
            .Add(component => component.StateStore, new ReadyStore(saved))
            .Add(component => component.Load, request => Answer(requests, request, slow: false)));

        grid.WaitForAssertion(() => Assert.Equal(2, grid.FindAll("tbody tr[data-omni-row-index]").Count));
        var sort = Assert.Single(Assert.Single(requests).Sorts);
        Assert.Equal("Name", sort.Key);
        Assert.False(sort.Descending);
    }

    [Fact]
    public void ShiftClick_AddsAColumnToTheSorts_AndDropsOneThatWasDescending()
    {
        var requests = new List<OmniDataGridLoadRequest>();
        var grid = Render<DataGridInitialLoadTestHost>(parameters => parameters
            .Add(component => component.Load, request => Answer(requests, request, slow: false)));
        grid.WaitForAssertion(() => Assert.Single(requests));
        string[] Sorts() => [.. requests[^1].Sorts.Select(sort => $"{sort.Key}{(sort.Descending ? "-" : "+")}")];
        var shift = new MouseEventArgs { ShiftKey = true };

        grid.Find("th[data-omni-col=\"Id\"]").Click(shift);
        Assert.Equal(["Name-", "Id+"], Sorts());

        // Name was descending: a third click on it removes it and leaves the others in place.
        grid.Find("th[data-omni-col=\"Name\"]").Click(shift);
        Assert.Equal(["Id+"], Sorts());

        // A plain click replaces every sort with the clicked column's next state.
        grid.Find("th[data-omni-col=\"Id\"]").Click();
        Assert.Equal(["Id-"], Sorts());
    }

    [Fact]
    public void RemoteDateRange_WithOnlyAnEnd_SendsTheUpperBound_AndUnreadableSidesSendNothing()
    {
        var requests = new List<OmniDataGridLoadRequest>();
        var grid = Render<DataGridFilterKindsTestHost>(parameters => parameters
            .Add(component => component.Load, request =>
            {
                requests.Add(request);
                return Task.FromResult(new OmniDataGridResult<DataGridFilterKindsTestHost.Ticket>([], 0));
            }));

        grid.Find("th[data-omni-col=\"Opened\"] .omni-data-grid__date-range-end").Change("2026-08-30");

        var filter = Assert.Single(requests[^1].Filters);
        Assert.Equal(OmniDataGridFilterOperator.LessThan, filter.Operator);
        Assert.Equal("2026-08-31T00:00:00", filter.Value);
        Assert.Null(filter.SecondOperator);

        grid.Find("th[data-omni-col=\"Opened\"] .omni-data-grid__date-range-end").Change("garbage");

        Assert.Empty(requests[^1].Filters);
    }

    [Fact]
    public async Task StaticRender_ShowsTheLoadingState_AndSendsNoRequest()
    {
        // A prerender or a static server render never runs OnAfterRender, where the columns are known
        // to have rendered: the grid is busy under its loading bar and asks its loader nothing.
        var requests = new List<OmniDataGridLoadRequest>();
        Func<OmniDataGridLoadRequest, Task<OmniDataGridResult<Row>>> load = request => Answer(requests, request, slow: false);
        await using var renderer = new HtmlRenderer(Services, Services.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(DataGridInitialLoadTestHost.Load)] = load });
            var output = await renderer.RenderComponentAsync<DataGridInitialLoadTestHost>(parameters);
            return output.ToHtmlString();
        });

        Assert.Empty(requests);
        Assert.Contains("aria-busy=\"true\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-omni-row-index", html, StringComparison.Ordinal);
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

    private sealed class ReadyStore(string state) : IOmniDataGridStateStore
    {
        public Task<string?> LoadAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult<string?>(state);

        public Task SaveAsync(string key, string state, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
