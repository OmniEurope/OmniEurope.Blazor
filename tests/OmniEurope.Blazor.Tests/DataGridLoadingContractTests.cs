using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The loading and error contract of the grid (R-432 and the remote-data convention): by default every
/// load, the grid's own requests and a host load signalled by <see cref="OmniDataGrid{TItem}.Busy"/>,
/// shows the bar over the table only, the rows staying in place; a failure is shown with a retry and
/// reported through <see cref="OmniDataGrid{TItem}.OnLoadError"/>.
/// </summary>
public sealed class DataGridLoadingContractTests : OmniBunitContext
{
    private const string ProgressRow = "thead > tr.omni-data-grid__progress";

    [Fact]
    public void Busy_WithRows_ShowsTheBar_AndKeepsTheRowsInPlace()
    {
        var grid = Render<OmniDataGrid<string>>(parameters => parameters
            .Add(component => component.Items, ["alpha", "beta"])
            .Add(component => component.Busy, true));

        Assert.Single(grid.FindAll(ProgressRow));
        Assert.Equal(["alpha", "beta"], grid.FindAll("tbody tr[data-omni-row-index]").Select(row => row.TextContent.Trim()));
        Assert.Empty(grid.FindAll("tbody .omni-data-grid__state"));
        Assert.Equal("true", grid.Find(".omni-data-grid").GetAttribute("aria-busy"));
        Assert.DoesNotContain("omni-data-grid--preparing", grid.Find(".omni-data-grid").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Busy_OnAGridWithNoRowYet_ShowsTheBar_AndNeitherTheEmptyMessageNorAVisibleLoadingRow()
    {
        var grid = Render<OmniDataGrid<string>>(parameters => parameters
            .Add(component => component.Items, Array.Empty<string>())
            .Add(component => component.Busy, true));

        Assert.Single(grid.FindAll(ProgressRow));
        Assert.DoesNotContain("Aucune donnée", grid.Find("tbody").TextContent, StringComparison.Ordinal);
        var pending = grid.Find("tbody tr.omni-data-grid__pending td");
        Assert.Equal("status", pending.GetAttribute("role"));
        // Read to screen readers only: the bar is the visible indicator.
        Assert.Equal("Chargement…", pending.QuerySelector(".omni-visually-hidden")!.TextContent);
        Assert.Equal(pending.TextContent.Trim(), pending.QuerySelector(".omni-visually-hidden")!.TextContent);

        grid.Render(parameters => parameters.Add(component => component.Busy, false));
        Assert.Empty(grid.FindAll(ProgressRow));
        Assert.Contains("Aucune donnée", grid.Find("tbody").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void RemoteFirstLoad_ShowsOnlyTheBar_ThenTheRows()
    {
        var pending = new TaskCompletionSource<OmniDataGridResult<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Load, _ => pending.Task));

        grid.WaitForAssertion(() => Assert.Single(grid.FindAll(ProgressRow)));
        Assert.Single(grid.FindAll("tbody tr.omni-data-grid__pending"));
        Assert.Empty(grid.FindAll("tbody tr[data-omni-row-index]"));

        pending.SetResult(new OmniDataGridResult<int>([1, 2, 3], 3));

        grid.WaitForAssertion(() => Assert.Equal(3, grid.FindAll("tbody tr[data-omni-row-index]").Count));
        Assert.Empty(grid.FindAll(ProgressRow));
        Assert.Empty(grid.FindAll("tbody tr.omni-data-grid__pending"));
    }

    [Fact]
    public void RemoteReload_KeepsTheRowsOnScreenUnderTheBar()
    {
        TaskCompletionSource<OmniDataGridResult<int>>? pending = null;
        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Load, _ => pending?.Task ?? Task.FromResult(new OmniDataGridResult<int>([1, 2], 2))));
        grid.WaitForAssertion(() => Assert.Equal(2, grid.FindAll("tbody tr[data-omni-row-index]").Count));

        pending = new TaskCompletionSource<OmniDataGridResult<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = grid.InvokeAsync(grid.Instance.ReloadAsync);

        grid.WaitForAssertion(() => Assert.Single(grid.FindAll(ProgressRow)));
        Assert.Equal(2, grid.FindAll("tbody tr[data-omni-row-index]").Count);
        Assert.Empty(grid.FindAll("tbody .omni-data-grid__state"));

        pending.SetResult(new OmniDataGridResult<int>([7], 1));
        grid.WaitForAssertion(() => Assert.Equal("7", grid.Find("tbody tr[data-omni-row-index]").TextContent.Trim()));
        Assert.Empty(grid.FindAll(ProgressRow));
    }

    [Fact]
    public void ShowLoadingBarOff_Busy_ReplacesTheRowsWithTheLoadingRow()
    {
        var grid = Render<OmniDataGrid<string>>(parameters => parameters
            .Add(component => component.Items, ["alpha", "beta"])
            .Add(component => component.ShowLoadingBar, false)
            .Add(component => component.Busy, true));

        Assert.Empty(grid.FindAll(ProgressRow));
        Assert.Empty(grid.FindAll("tbody tr[data-omni-row-index]"));
        Assert.Equal("Chargement…", grid.Find("tbody .omni-data-grid__state--loading").TextContent.Trim());
    }

    [Fact]
    public void LoadFailure_IsReportedOnce_AndShownThroughErrorContent_WithTheRetry()
    {
        var failures = new List<Exception>();
        var boom = new InvalidOperationException("serveur indisponible");
        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Load, _ => Task.FromException<OmniDataGridResult<int>>(boom))
            .Add(component => component.OnLoadError, EventCallback.Factory.Create<Exception>(this, failures.Add))
            .Add(component => component.ErrorContent, (RenderFragment<Exception>)(exception => builder =>
                builder.AddMarkupContent(0, $"<span class=\"probe-error\">{exception.Message}</span>"))));

        grid.WaitForAssertion(() => Assert.Equal("serveur indisponible", grid.Find(".omni-data-grid__state .probe-error").TextContent));
        Assert.Equal("alert", grid.Find(".omni-data-grid__state").GetAttribute("role"));
        Assert.NotNull(grid.Find(".omni-data-grid__state button"));
        Assert.Same(boom, Assert.Single(failures));

        grid.Render(parameters => parameters.Add(component => component.Caption, "Relu"));
        Assert.Single(failures);
    }

    [Fact]
    public void LoadFailure_WithoutErrorContent_KeepsTheLocalizedMessage()
    {
        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Load, _ => Task.FromException<OmniDataGridResult<int>>(new InvalidOperationException("x"))));

        grid.WaitForAssertion(() => Assert.Contains("Le chargement de la grille a échoué.", grid.Find(".omni-data-grid__state").TextContent, StringComparison.Ordinal));
    }

    [Fact]
    public void DataList_LoadFailure_IsReportedThroughOnLoadError()
    {
        var failures = new List<Exception>();
        var boom = new InvalidOperationException("liste indisponible");
        var list = Render<OmniDataList<string>>(parameters => parameters
            .Add(component => component.ItemTemplate, item => builder => builder.AddContent(0, item))
            .Add(component => component.Load, _ => Task.FromException<IReadOnlyList<string>>(boom))
            .Add(component => component.OnLoadError, EventCallback.Factory.Create<Exception>(this, failures.Add)));

        list.WaitForAssertion(() => Assert.Single(list.FindAll(".omni-data-list__state--error")));
        Assert.Same(boom, Assert.Single(failures));
    }
}
