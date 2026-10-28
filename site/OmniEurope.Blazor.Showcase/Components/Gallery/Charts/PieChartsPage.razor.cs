using OmniEurope.Blazor.Showcase.Examples.Charts;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Charts;

/// <summary>The Pies and donuts page: the kind, data, slice count, labels, legend and data table of a chart, then a gallery and the outside labels.</summary>
public partial class PieChartsPage
{
    [Inject]
    private IStringLocalizer<ChartsStrings> Text { get; set; } = default!;

    private OmniPieLabels OutsideLabels { get; set; } = OmniPieLabels.None;

    private PieChartKind Kind { get; set; } = PieChartKind.Donut;

    private PieDataset Dataset { get; set; } = PieDataset.Channels;

    private double SliceCountValue { get; set; } = 5;

    private bool IncludeZeroSlice { get; set; }

    private bool ShowLegend { get; set; } = true;

    private bool ShowDataTable { get; set; }

    private double MaximumSlices => PieChartData.Of(Dataset).Count;

    /// <summary>Passing to a shorter distribution must not leave the slider past its end.</summary>
    private int SliceCount => Math.Clamp((int)SliceCountValue, 1, PieChartData.Of(Dataset).Count);

    private IReadOnlyList<OmniOption<OmniPieLabels>> OutsideLabelOptions =>
    [
        new(OmniPieLabels.None, Text["PieChartsLabelsNone"]),
        new(OmniPieLabels.Name, Text["PieChartsLabelsName"]),
        new(OmniPieLabels.NameAndValue, Text["PieChartsLabelsNameAndValue"]),
        new(OmniPieLabels.NameAndPercent, Text["PieChartsLabelsNameAndPercent"])
    ];

    private IReadOnlyList<OmniOption<PieChartKind>> KindOptions =>
    [
        new(PieChartKind.Pie, Text["PieChartsKindPie"]),
        new(PieChartKind.Donut, Text["PieChartsKindDonut"])
    ];

    private IReadOnlyList<OmniOption<PieDataset>> DatasetOptions =>
    [
        new(PieDataset.Channels, Text["PieChartsDatasetChannels"]),
        new(PieDataset.Countries, Text["PieChartsDatasetCountries"])
    ];
}
