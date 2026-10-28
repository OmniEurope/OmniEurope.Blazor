using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Charts;

/// <summary>The distributions shared by the pie and donut chart examples.</summary>
public static class PieChartData
{
    /// <summary>Filings by channel.</summary>
    public static IReadOnlyList<OmniChartSlice> Channels { get; } =
    [
        new("Desk", 46),
        new("Mail", 28),
        new("Online", 19),
        new("Phone", 11),
        new("Other", 7)
    ];

    /// <summary>Files by country.</summary>
    public static IReadOnlyList<OmniChartSlice> Countries { get; } =
    [
        new("Belgium", 124),
        new("France", 187),
        new("Luxembourg", 63),
        new("Netherlands", 98),
        new("Germany", 141),
        new("Spain", 76)
    ];

    /// <summary>A disc made of one slice.</summary>
    public static IReadOnlyList<OmniChartSlice> SingleSlice { get; } = [new("Online", 100)];

    /// <summary>A disc made of two slices: what is done and what remains.</summary>
    public static IReadOnlyList<OmniChartSlice> TwoSlices { get; } = [new("Done", 72), new("Remaining", 28)];

    /// <summary>The origin of the files, for the examples that write the labels outside the disc.</summary>
    public static IReadOnlyList<OmniChartSlice> Origins { get; } =
    [
        new("Counter", 46),
        new("Post", 28),
        new("Online", 19),
        new("Other", 7)
    ];

    /// <summary>The slices of a distribution.</summary>
    /// <param name="dataset">The distribution.</param>
    /// <returns>Its slices.</returns>
    public static IReadOnlyList<OmniChartSlice> Of(PieDataset dataset) =>
        dataset == PieDataset.Channels ? Channels : Countries;
}
