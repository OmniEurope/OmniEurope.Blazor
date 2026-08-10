using Bunit;

namespace OmniEurope.Blazor.Tests;

public sealed class ChartComponentTests : BunitContext
{
    [Fact]
    public void Charts_RenderSemanticSvgSeriesAndAccessibleAlternative()
    {
        var charts = Render<ChartTestHost>();

        Assert.Equal(2, charts.FindAll(".omni-chart__svg").Count);
        Assert.Equal("img", charts.Find("#sales .omni-chart__svg").GetAttribute("role"));
        Assert.Contains("Ventes mensuelles", charts.Find("#sales desc").TextContent, StringComparison.Ordinal);
        Assert.Equal(3, charts.FindAll("#sales .omni-chart__markers circle").Count);
        Assert.Equal(4, charts.FindAll(".omni-chart__pie path, .omni-chart__donut path").Count);
        Assert.Contains("Données des ventes", charts.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("style=", charts.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ArcGauge_ClampsAndAnnouncesItsValue()
    {
        var gauge = Render<ChartTestHost>();
        Assert.Contains("75", gauge.Find(".omni-arc-gauge__value text").TextContent, StringComparison.Ordinal);
        Assert.Equal("Progression", gauge.Find(".omni-arc-gauge__svg").GetAttribute("aria-label"));
        Assert.StartsWith("M ", gauge.Find(".omni-arc-gauge__value path").GetAttribute("d"), StringComparison.Ordinal);
    }
}
