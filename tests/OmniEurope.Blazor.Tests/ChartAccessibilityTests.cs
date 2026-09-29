using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// What a screen reader gets from a chart and a gauge, the numbers written in the current culture, one
/// series component for columns and bars, and a legend read from the series.
/// </summary>
public sealed class ChartAccessibilityTests : OmniBunitContext
{
    private static readonly IReadOnlyList<string> Quarters = ["T1", "T2"];

    [Fact]
    public void Gauge_AccessibleName_CarriesItsValue_EvenWhenTheValueIsNotWritten()
    {
        var gauge = Render<OmniArcGauge>(parameters => parameters
            .Add(component => component.Label, "Progression")
            .AddChildContent<OmniArcGaugeScale>(scale => scale
                .AddChildContent<OmniArcGaugeScaleValue>(value => value
                    .Add(component => component.Value, 75)
                    .Add(component => component.ShowValue, false)
                    .Add(component => component.FormatValue, number => $"{number} %"))));

        var name = gauge.Find(".omni-arc-gauge__svg").GetAttribute("aria-label");
        Assert.StartsWith("Progression", name, StringComparison.Ordinal);
        Assert.EndsWith("75 %", name, StringComparison.Ordinal);
    }

    [Fact]
    public void Gauge_AccessibleName_FollowsANewValue()
    {
        var value = 20d;
        var gauge = Render<OmniArcGauge>(parameters => parameters
            .Add(component => component.Label, "Charge")
            .AddChildContent<OmniArcGaugeScaleValue>(child => child.Add(component => component.Value, value)));
        Assert.EndsWith("20", gauge.Find("svg").GetAttribute("aria-label"), StringComparison.Ordinal);

        value = 64;
        gauge.Render(parameters => parameters
            .Add(component => component.Label, "Charge")
            .AddChildContent<OmniArcGaugeScaleValue>(child => child.Add(component => component.Value, value)));
        gauge.WaitForAssertion(() => Assert.EndsWith("64", gauge.Find("svg").GetAttribute("aria-label"), StringComparison.Ordinal));
    }

    [Fact]
    public void Chart_WithoutATableOfTheHost_WritesAHiddenTableOfItsSeries()
    {
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Dossiers")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<OmniCategoryAxis>(0);
                builder.AddAttribute(1, nameof(OmniCategoryAxis.Labels), Quarters);
                builder.CloseComponent();
                builder.OpenComponent<OmniColumnSeries>(2);
                builder.AddAttribute(3, nameof(OmniColumnSeries.Title), "Reçus");
                builder.AddAttribute(4, nameof(OmniColumnSeries.Data), (IReadOnlyList<OmniChartPoint>)[new(1, 12), new(2, 18)]);
                builder.CloseComponent();
                builder.OpenComponent<OmniLineSeries>(5);
                builder.AddAttribute(6, nameof(OmniLineSeries.Data), (IReadOnlyList<OmniChartPoint>)[new(1, 7)]);
                builder.CloseComponent();
                builder.OpenComponent<OmniMarkers>(7);
                builder.AddAttribute(8, nameof(OmniMarkers.Data), (IReadOnlyList<OmniChartPoint>)[new(1, 7)]);
                builder.CloseComponent();
            }));

        chart.WaitForAssertion(() =>
        {
            var table = chart.Find("figure > table.omni-chart__table");
            Assert.Contains("omni-visually-hidden", table.ClassList);
            Assert.Equal("Dossiers", table.QuerySelector("caption")!.TextContent);
            // One column per drawn series (the markers only decorate one), an untitled one numbered.
            Assert.Equal(["Catégorie", "Reçus", "Série 2"], table.QuerySelectorAll("thead th").Select(cell => cell.TextContent));
            var rows = table.QuerySelectorAll("tbody tr");
            Assert.Equal(["T1", "12", "7"], rows[0].Children.Select(cell => cell.TextContent));
            Assert.Equal(["T2", "18", ""], rows[1].Children.Select(cell => cell.TextContent));
            Assert.Equal("row", rows[0].Children[0].GetAttribute("scope"));
        });
    }

    [Fact]
    public void Chart_WithATableOfTheHost_ShowsThatOneAndWritesNone()
    {
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Dossiers")
            .Add(component => component.DataTableContent, (RenderFragment)(builder => builder.AddMarkupContent(0, "<table class=\"host\"></table>")))
            .AddChildContent<OmniColumnSeries>(series => series.Add(component => component.Data, [new OmniChartPoint(1, 12)])));

        Assert.Single(chart.FindAll(".omni-chart__data table.host"));
        Assert.Empty(chart.FindAll(".omni-chart__table"));
    }

    [Fact]
    public void Chart_IsDescribedOnlyWhenItHasADescription()
    {
        var bare = Render<OmniChart>(parameters => parameters.Add(component => component.Title, "Sans description"));
        var described = Render<OmniChart>(parameters => parameters
            .Add(component => component.Id, "decrit")
            .Add(component => component.Title, "Avec description")
            .Add(component => component.Description, "Ventes du semestre"));

        var svg = bare.Find("svg");
        Assert.Empty(bare.FindAll("desc"));
        Assert.Null(svg.GetAttribute("aria-describedby"));
        Assert.Equal(bare.Find("title").Id, svg.GetAttribute("aria-labelledby"));
        Assert.Equal("decrit-description", described.Find("svg").GetAttribute("aria-describedby"));
        Assert.Equal("Ventes du semestre", described.Find("#decrit-description").TextContent);
    }

    [Fact]
    public void PieChart_TableListsTheSlices_AndTheHoverTextUsesTheSharedSeparator()
    {
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Origine")
            .AddChildContent<OmniPieSeries>(pie => pie
                .Add(component => component.Title, "Canal")
                .Add(component => component.Data, [new OmniChartSlice("Guichet", 46), new OmniChartSlice("Vide", 0), new OmniChartSlice("En ligne", 19)])));

        chart.WaitForAssertion(() =>
        {
            var table = chart.Find("table.omni-chart__table");
            Assert.Equal("Canal", table.QuerySelector("caption")!.TextContent);
            Assert.Equal(["Guichet", "En ligne"], table.QuerySelectorAll("tbody th").Select(cell => cell.TextContent));
        });
        Assert.Equal("Guichet · 46", chart.Find(".omni-chart__pie path title").TextContent);
    }

    [Fact]
    public void DisplayedNumbers_FollowTheCurrentCulture_WhileGeometryStaysInvariant()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var labels = Render<OmniSeriesDataLabels>(parameters => parameters.Add(component => component.Data, [new OmniChartPoint(1, 2.5)]));
            var axis = Render<OmniValueAxis>(parameters => parameters
                .Add(component => component.Minimum, 0)
                .Add(component => component.Maximum, 1)
                .Add(component => component.TickCount, 2));
            var gauge = Render<OmniArcGaugeScaleValue>(parameters => parameters.Add(component => component.Value, 12.5));

            Assert.Equal("2,5", labels.Find("text").TextContent);
            Assert.Equal(["0", "0,5", "1"], axis.FindAll("text").Select(text => text.TextContent));
            Assert.Equal("12,5", gauge.Find("text").TextContent);
            Assert.DoesNotContain(",", labels.Find("text").GetAttribute("x"), StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void HorizontalStackedColumns_SitEndToEnd_OnOneBandPlace()
    {
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Empilé")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<OmniValueAxis>(0);
                builder.AddAttribute(1, nameof(OmniValueAxis.Maximum), 100d);
                builder.CloseComponent();
                builder.OpenComponent<OmniColumnSeries>(2);
                builder.AddAttribute(3, nameof(OmniColumnSeries.Horizontal), true);
                builder.AddAttribute(4, nameof(OmniColumnSeries.Stacked), true);
                builder.AddAttribute(5, nameof(OmniColumnSeries.Data), (IReadOnlyList<OmniChartPoint>)[new(1, 30), new(2, 10)]);
                builder.CloseComponent();
                builder.OpenComponent<OmniColumnSeries>(6);
                builder.AddAttribute(7, nameof(OmniColumnSeries.Horizontal), true);
                builder.AddAttribute(8, nameof(OmniColumnSeries.Stacked), true);
                builder.AddAttribute(9, nameof(OmniColumnSeries.Data), (IReadOnlyList<OmniChartPoint>)[new(1, 20), new(2, 40)]);
                builder.CloseComponent();
            }));

        chart.WaitForAssertion(() =>
        {
            var groups = chart.FindAll(".omni-chart__bars--stacked");
            Assert.Equal(2, groups.Count);
            var first = groups[0].QuerySelectorAll("rect");
            var second = groups[1].QuerySelectorAll("rect");
            for (var index = 0; index < 2; index++)
            {
                Assert.Equal(Number(first[index], "x") + Number(first[index], "width"), Number(second[index], "x"), 6);
                Assert.Equal(Number(first[index], "y"), Number(second[index], "y"), 6);
            }
        });
    }

    [Fact]
    public void Legend_ReadsTheTitledSeries_WithTheirColours()
    {
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Légende")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<OmniLineSeries>(0);
                builder.AddAttribute(1, nameof(OmniLineSeries.Title), "Dépôts");
                builder.AddAttribute(2, nameof(OmniLineSeries.ColorIndex), 5);
                builder.AddAttribute(3, nameof(OmniLineSeries.Data), (IReadOnlyList<OmniChartPoint>)[new(0, 1), new(1, 2)]);
                builder.CloseComponent();
                builder.OpenComponent<OmniMarkers>(4);
                builder.AddAttribute(5, nameof(OmniMarkers.Data), (IReadOnlyList<OmniChartPoint>)[new(0, 1)]);
                builder.CloseComponent();
                builder.OpenComponent<OmniAreaSeries>(6);
                builder.AddAttribute(7, nameof(OmniAreaSeries.Title), "Décisions");
                builder.AddAttribute(8, nameof(OmniAreaSeries.ColorIndex), 10);
                builder.AddAttribute(9, nameof(OmniAreaSeries.Data), (IReadOnlyList<OmniChartPoint>)[new(0, 1), new(1, 3)]);
                builder.CloseComponent();
                builder.OpenComponent<OmniLegend>(10);
                builder.AddAttribute(11, nameof(OmniLegend.Position), OmniLegendPosition.Bottom);
                builder.CloseComponent();
            }));

        chart.WaitForAssertion(() =>
        {
            var items = chart.FindAll("ul.omni-chart__legend--below li");
            Assert.Equal(["Dépôts", "Décisions"], items.Select(item => item.TextContent));
            Assert.Contains("omni-chart-color-5", items[0].QuerySelector(".omni-chart__swatch")!.ClassList);
            Assert.Contains("omni-chart-color-2", items[1].QuerySelector(".omni-chart__swatch")!.ClassList);
        });
    }

    private static double Number(AngleSharp.Dom.IElement element, string attribute) =>
        double.Parse(element.GetAttribute(attribute)!, CultureInfo.InvariantCulture);
}
