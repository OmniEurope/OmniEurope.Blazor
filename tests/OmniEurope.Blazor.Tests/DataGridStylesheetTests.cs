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

    [Theory]
    [InlineData(".omni-data-grid__icon-button")]
    [InlineData(".omni-data-grid__filter-menu-toggle")]
    public void HeaderControls_AreAtLeast24PixelsWide_AndTall_WhateverTheDensity(string selector)
    {
        // WCAG 2.2 AA 2.5.8: the padding of the density alone made a compact one 20 px high.
        var body = ShippedLookTests.Body(selector);

        Assert.Equal("1.5rem", ShippedLookTests.Value(body, "min-block-size"));
        Assert.Equal("1.5rem", ShippedLookTests.Value(body, "min-inline-size"));
    }

    [Fact]
    public void FilterMenuToggle_CountsItsPaddingInsideTheMinimum()
    {
        // A summary is content-box: without it the 24 px floor came out as 32 px once the padding was added.
        Assert.Equal("border-box", ShippedLookTests.Value(ShippedLookTests.Body(".omni-data-grid__filter-menu-toggle"), "box-sizing"));
    }

    [Fact]
    public void RowActions_KeepTheSquareOfTheStatusBadges()
    {
        Assert.Equal("0", ShippedLookTests.Value(ShippedLookTests.Body(".omni-data-grid__row > td .omni-data-grid__icon-button"), "min-block-size"));
    }
}
