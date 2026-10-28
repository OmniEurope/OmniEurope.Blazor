namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>A grid veiled until the pictures of its first rows have arrived.</summary>
public partial class DataGridPicturesExample
{
    // Four rows with a picture.
    private static readonly IReadOnlyList<CaseRow> Rows = [.. CaseData.Fill.Take(4)];
}
