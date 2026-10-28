using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Charts family of the gallery: its pages, in menu order.</summary>
internal static class ChartsFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyCharts", OmniIconName.ChartBarHorizontal, typeof(ChartsStrings),
    [
        new("column-charts", "ColumnChartsTitle", "ColumnChartsDescription", OmniIconName.ChartBarHorizontal,
            ["OmniChart", "OmniColumnSeries", "OmniCategoryAxis", "OmniValueAxis", "OmniAxisTitle", "OmniGridLines", "OmniLegend", "OmniSeriesDataLabels"],
            [
                "Charts/ColumnChartExample.razor", "Charts/ColumnChartExample.razor.cs",
                "Charts/ColumnChartKindsExample.razor", "Charts/ColumnChartKindsExample.razor.cs",
                "Charts/ColumnChartMaxHeightExample.razor",
                "Charts/ColumnChartStackedBarsExample.razor",
                "Charts/ColumnChartKind.cs", "Charts/ColumnChartData.cs"
            ]),
        new("line-charts", "LineChartsTitle", "LineChartsDescription", OmniIconName.Path,
            ["OmniLineSeries", "OmniAreaSeries", "OmniMarkers", "OmniRangeNavigator", "OmniChart", "OmniLegend"],
            [
                "Charts/LineChartExample.razor", "Charts/LineChartExample.razor.cs",
                "Charts/LineChartKindsExample.razor",
                "Charts/LineChartMonthlyExample.razor",
                "Charts/LineChartDailyExample.razor", "Charts/LineChartDailyExample.razor.cs",
                "Charts/LineChartBacklogExample.razor",
                "Charts/LineChartRangeExample.razor", "Charts/LineChartRangeExample.razor.cs",
                "Charts/LineChartKind.cs", "Charts/LineChartData.cs"
            ]),
        new("pie-charts", "PieChartsTitle", "PieChartsDescription", OmniIconName.ThemeSystem,
            ["OmniPieSeries", "OmniChart", "OmniLegend"],
            [
                "Charts/PieChartExample.razor", "Charts/PieChartExample.razor.cs",
                "Charts/PieChartKindsExample.razor",
                "Charts/PieChartLabelsExample.razor",
                "Charts/PieChartKind.cs", "Charts/PieDataset.cs", "Charts/PieChartData.cs"
            ]),
        new("gauges", "GaugesTitle", "GaugesDescription", OmniIconName.Timer,
            ["OmniArcGauge", "OmniArcGaugeScale", "OmniArcGaugeScaleValue"],
            [
                "Charts/GaugeExample.razor", "Charts/GaugeExample.razor.cs",
                "Charts/GaugePaletteExample.razor",
                "Charts/GaugeLimitsExample.razor", "Charts/GaugeLimitsExample.razor.cs",
                "Charts/GaugeScale.cs", "Charts/GaugeFormat.cs", "Charts/GaugeScales.cs"
            ])
    ]);
}
