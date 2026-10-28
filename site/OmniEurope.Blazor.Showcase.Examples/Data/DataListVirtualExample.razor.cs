namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>A virtualized list of two thousand files in a frame of bounded height.</summary>
public partial class DataListVirtualExample
{
    private IReadOnlyList<CaseRow> Files { get; } =
    [
        .. Enumerable.Range(1, 2_000).Select(index => CaseData.Rows[index % CaseData.Rows.Count] with { Reference = $"L-{index:0000}" })
    ];
}
