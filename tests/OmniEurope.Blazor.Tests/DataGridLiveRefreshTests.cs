using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Covers <see cref="OmniDataGrid{TItem}.RefreshAsync"/>: a live refresh keeps the rows on screen
/// while it fetches (no loading state), swaps them when the new ones are in, and marks the rows
/// that were not held before for <see cref="OmniDataGrid{TItem}.NewRowHighlight"/>.
/// </summary>
public sealed class DataGridLiveRefreshTests : OmniBunitContext
{
    private const string NewRow = "omni-data-grid__row--new";

    public sealed record Row(int Id, string Name);

    [Fact]
    public async Task RemoteRefresh_KeepsTheRowsWhileFetching_ThenMarksOnlyTheNewOne()
    {
        var rows = new List<Row> { new(1, "alpha"), new(2, "beta") };
        TaskCompletionSource<OmniDataGridResult<Row>>? pending = null;
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.KeyProperty, nameof(Row.Id))
            .Add(component => component.NewRowHighlight, TimeSpan.FromSeconds(30))
            .Add(component => component.Load, _ => pending?.Task
                ?? Task.FromResult(new OmniDataGridResult<Row>([.. rows], rows.Count))));
        grid.WaitForAssertion(() => Assert.Equal(2, grid.FindAll("tbody tr").Count));

        rows.Insert(0, new Row(3, "gamma"));
        pending = new TaskCompletionSource<OmniDataGridResult<Row>>();
        var refresh = grid.InvokeAsync(() => grid.Instance.RefreshAsync());

        // While the fetch runs the table keeps its rows and never says it is loading.
        Assert.Equal(2, grid.FindAll("tbody tr").Count);
        Assert.Equal("false", grid.Find(".omni-data-grid").GetAttribute("aria-busy"));

        pending.SetResult(new OmniDataGridResult<Row>([.. rows], rows.Count));
        await refresh;

        var body = grid.FindAll("tbody tr");
        Assert.Equal(3, body.Count);
        Assert.Contains("gamma", body[0].TextContent, StringComparison.Ordinal);
        Assert.Contains(NewRow, body[0].ClassName, StringComparison.Ordinal);
        Assert.DoesNotContain(NewRow, body[1].ClassName, StringComparison.Ordinal);
        Assert.DoesNotContain(NewRow, body[2].ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NewRowMark_WearsOffAfterTheHighlightDuration()
    {
        var rows = new List<Row> { new(1, "alpha") };
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.KeyProperty, nameof(Row.Id))
            .Add(component => component.NewRowHighlight, TimeSpan.FromMilliseconds(80))
            .Add(component => component.Load, _ => Task.FromResult(new OmniDataGridResult<Row>([.. rows], rows.Count))));

        rows.Add(new Row(2, "beta"));
        await grid.InvokeAsync(() => grid.Instance.RefreshAsync());
        Assert.Single(grid.FindAll($"tbody tr.{NewRow}"));

        grid.WaitForAssertion(() => Assert.Empty(grid.FindAll($"tbody tr.{NewRow}")), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task HostSuppliedRows_AreComparedToTheRowsHeldWhenTheRefreshWasAsked()
    {
        Row[] before = [new(1, "alpha"), new(2, "beta")];
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.KeyProperty, nameof(Row.Id))
            .Add(component => component.NewRowHighlight, TimeSpan.FromSeconds(30))
            .Add(component => component.Items, before));

        await grid.InvokeAsync(() => grid.Instance.RefreshAsync());
        grid.Render(parameters => parameters.Add(component => component.Items, [new Row(4, "delta"), .. before]));

        Assert.Single(grid.FindAll($"tbody tr.{NewRow}"));
        Assert.Contains("delta", grid.Find($"tbody tr.{NewRow}").TextContent, StringComparison.Ordinal);

        // Rows the host swaps without asking for a refresh (a filter of its own) are not "new".
        grid.Render(parameters => parameters.Add(component => component.Items, [new Row(5, "epsilon")]));
        Assert.DoesNotContain("epsilon", string.Concat(grid.FindAll($"tbody tr.{NewRow}").Select(row => row.TextContent)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task WithoutARowKey_NothingIsMarked()
    {
        var rows = new List<Row> { new(1, "alpha") };
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.NewRowHighlight, TimeSpan.FromSeconds(30))
            .Add(component => component.Load, _ => Task.FromResult(new OmniDataGridResult<Row>([.. rows], rows.Count))));

        rows.Add(new Row(2, "beta"));
        await grid.InvokeAsync(() => grid.Instance.RefreshAsync());

        Assert.Equal(2, grid.FindAll("tbody tr").Count);
        Assert.Empty(grid.FindAll($"tbody tr.{NewRow}"));
    }

    [Fact]
    public async Task VirtualizedRemoteRefresh_ReplacesTheWindow_AndMarksTheNewRow()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var rows = Enumerable.Range(1, 30).Select(id => new Row(id, $"row {id}")).ToList();
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.AllowVirtualization, true)
            .Add(component => component.EstimatedRowHeight, 40d)
            .Add(component => component.VirtualBlockSize, 50)
            .Add(component => component.KeyProperty, nameof(Row.Id))
            .Add(component => component.NewRowHighlight, TimeSpan.FromSeconds(30))
            .Add(component => component.Load, request => Task.FromResult(new OmniDataGridResult<Row>(
                rows.Skip(request.Skip).Take(request.Top).ToArray(), rows.Count))));
        grid.WaitForAssertion(() => Assert.Contains("Name = row 1 }", grid.Markup, StringComparison.Ordinal));

        rows.Insert(0, new Row(99, "fresh"));
        await grid.InvokeAsync(() => grid.Instance.RefreshAsync());

        Assert.Equal("31", grid.Find("table").GetAttribute("aria-rowcount"));
        var marked = Assert.Single(grid.FindAll($"tbody tr.{NewRow}"));
        Assert.Contains("fresh", marked.TextContent, StringComparison.Ordinal);
    }
}
