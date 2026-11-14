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

    [Fact]
    public void ThousandPointLineInAChart_WithItsHoverZones_StaysWithinRegressionBudget()
    {
        var points = Enumerable.Range(0, 1_000)
            .Select(index => new OmniChartPoint(index / 10d, index % 100))
            .ToArray();
        IRenderedComponent<OmniChart> RenderChart() => Render<OmniChart>(parameters => parameters
            .Add(component => component.Label, "Points")
            .AddChildContent<OmniLineSeries>(series => series.Add(component => component.Data, points)));
        using var functionalChart = RenderChart();
        functionalChart.WaitForAssertion(() => Assert.Equal(1_000, functionalChart.FindAll(".omni-chart__point-zone").Count));
        var (elapsed, allocated) = MeasureMedian(() =>
        {
            using var chart = RenderChart();
        });

        Assert.True(elapsed < TimeSpan.FromSeconds(3), $"Temps médian: {elapsed}");
        // Measured at 17 MB on 2026-11-02, with the three elements of each hover zone.
        Assert.True(allocated < 40 * 1024 * 1024, $"Allocations: {allocated} octets");
    }

    [Fact]
    public void TenThousandCellSpreadsheet_DrawnWhole_StaysWithinRegressionBudget()
    {
        // 1 000 rows of 10 columns, the last one a formula: the sheet draws every cell, without
        // virtualization, so this is the practical size docs/performance-budgets.md announces.
        var rows = Enumerable.Range(1, 1_000)
            .Select(row => (IReadOnlyList<string>)[
                $"Poste {row}", .. Enumerable.Range(1, 8).Select(column => (row * column).ToString(System.Globalization.CultureInfo.InvariantCulture)),
                $"=SUM(B{row}:I{row})"])
            .ToArray();
        var sheet = OmniSpreadsheetData.FromRows(rows);
        IRenderedComponent<OmniSpreadsheet> RenderSheet() => Render<OmniSpreadsheet>(parameters => parameters
            .Add(component => component.Value, sheet)
            .Add(component => component.Label, "Budget"));
        using var functionalSheet = RenderSheet();
        Assert.Equal(10_000, functionalSheet.FindAll("[role=gridcell]").Count);
        var (elapsed, allocated) = MeasureMedian(() =>
        {
            using var rendered = RenderSheet();
        });

        Assert.True(elapsed < TimeSpan.FromSeconds(3), $"Temps médian: {elapsed}");
        // Measured at 45 MB and 75 ms on 2026-11-14 (net11.0, Release).
        Assert.True(allocated < 100 * 1024 * 1024, $"Allocations: {allocated} octets");
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
