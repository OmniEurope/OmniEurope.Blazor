namespace OmniEurope.Blazor.Tests;

/// <summary>What the stylesheet gives the grid's fixed rows and small header controls, read from the shipped CSS.</summary>
public sealed class DataGridStylesheetTests
{
    [Fact]
    public void FixedRowHeight_WithoutAnEstimate_IsDrawnAtTheDensityToken()
    {
        // The rows read --omni-row-height; the grid defaults it to the token the virtualization reads
        // as its item size (the script writes the consumer's height nearer to the rows, on the viewport).
        var grid = ShippedLookTests.Body(".omni-data-grid--fixed-row-height");

        Assert.Equal("var(--omni-grid-row-estimate)", ShippedLookTests.Value(grid, "--omni-row-height"));
    }
}
