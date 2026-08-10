using System.Diagnostics;
using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class PerformanceBudgetTests : BunitContext
{
    [Fact]
    public void OneThousandSimpleComponents_StayWithinRegressionBudget()
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        var watch = Stopwatch.StartNew();
        for (var index = 0; index < 1_000; index++)
        {
            using var button = Render<OmniButton>(parameters => parameters.AddChildContent($"Bouton {index}"));
        }
        watch.Stop();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(5), $"Temps: {watch.Elapsed}");
        Assert.True(allocated < 160 * 1024 * 1024, $"Allocations: {allocated} octets");
    }

    [Fact]
    public void TenThousandRowGrid_RendersOnlyItsPageWithinBudget()
    {
        var items = Enumerable.Range(1, 10_000).ToArray();
        var before = GC.GetAllocatedBytesForCurrentThread();
        var watch = Stopwatch.StartNew();
        using var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, items)
            .Add(component => component.PageSize, 50));
        watch.Stop();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(50, grid.FindAll("tbody tr").Count);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(3), $"Temps: {watch.Elapsed}");
        Assert.True(allocated < 160 * 1024 * 1024, $"Allocations: {allocated} octets");
    }

    [Fact]
    public void ThousandPointSvg_StaysWithinRegressionBudget()
    {
        var points = Enumerable.Range(0, 1_000)
            .Select(index => new OmniChartPoint(index / 10d, index % 100))
            .ToArray();
        var before = GC.GetAllocatedBytesForCurrentThread();
        var watch = Stopwatch.StartNew();
        using var series = Render<OmniLineSeries>(parameters => parameters.Add(component => component.Data, points));
        watch.Stop();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(1_000, series.Find("polyline").GetAttribute("points")!.Split(' ').Length);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(3), $"Temps: {watch.Elapsed}");
        Assert.True(allocated < 80 * 1024 * 1024, $"Allocations: {allocated} octets");
    }
}
