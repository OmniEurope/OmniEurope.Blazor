using System.Diagnostics;
using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

[Collection("Performance")]
public sealed class PerformanceBudgetTests : OmniBunitContext
{
    [Fact]
    public void OneThousandSimpleComponents_StayWithinRegressionBudget()
    {
        void RenderBatch()
        {
            for (var index = 0; index < 1_000; index++)
            {
                using var button = Render<OmniButton>(parameters => parameters.AddChildContent($"Bouton {index}"));
            }
        }
        var (elapsed, allocated) = MeasureMedian(RenderBatch);

        Assert.True(elapsed < TimeSpan.FromSeconds(5), $"Temps médian: {elapsed}");
        Assert.True(allocated < 160 * 1024 * 1024, $"Allocations: {allocated} octets");
    }

    [Fact]
    public void TenThousandRowGrid_RendersOnlyItsPageWithinBudget()
    {
        var items = Enumerable.Range(1, 10_000).ToArray();
        using var functionalGrid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, items)
            .Add(component => component.PageSize, 50));
        Assert.Equal(50, functionalGrid.FindAll("tbody tr").Count);
        var (elapsed, allocated) = MeasureMedian(() =>
        {
            using var grid = Render<OmniDataGrid<int>>(parameters => parameters
                .Add(component => component.Items, items)
                .Add(component => component.PageSize, 50));
        });

        Assert.True(elapsed < TimeSpan.FromSeconds(3), $"Temps médian: {elapsed}");
        Assert.True(allocated < 160 * 1024 * 1024, $"Allocations: {allocated} octets");
    }

    [Fact]
    public void ThousandPointSvg_StaysWithinRegressionBudget()
    {
        var points = Enumerable.Range(0, 1_000)
            .Select(index => new OmniChartPoint(index / 10d, index % 100))
            .ToArray();
        using var functionalSeries = Render<OmniLineSeries>(parameters => parameters.Add(component => component.Data, points));
        Assert.Equal(1_000, functionalSeries.Find("polyline").GetAttribute("points")!.Split(' ').Length);
        var (elapsed, allocated) = MeasureMedian(() =>
        {
            using var series = Render<OmniLineSeries>(parameters => parameters.Add(component => component.Data, points));
        });

        Assert.True(elapsed < TimeSpan.FromSeconds(3), $"Temps médian: {elapsed}");
        Assert.True(allocated < 80 * 1024 * 1024, $"Allocations: {allocated} octets");
    }

    private static (TimeSpan Elapsed, long Allocated) MeasureMedian(Action action)
    {
        action();
        var samples = new (TimeSpan Elapsed, long Allocated)[5];
        for (var index = 0; index < samples.Length; index++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var before = GC.GetTotalAllocatedBytes(precise: true);
            var watch = Stopwatch.StartNew();
            action();
            watch.Stop();
            samples[index] = (watch.Elapsed, GC.GetTotalAllocatedBytes(precise: true) - before);
        }

        return (
            samples.Select(sample => sample.Elapsed).Order().ElementAt(samples.Length / 2),
            samples.Select(sample => sample.Allocated).Order().ElementAt(samples.Length / 2));
    }
}
