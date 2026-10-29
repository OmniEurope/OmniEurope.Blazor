using Bunit;
using OmniEurope.Blazor.Components;
using static OmniEurope.Blazor.Tests.ShippedLookTests;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniStatTile.WrapTitle"/>: a long caption wraps instead of shrinking and being cut. The markup is
/// rendered; the look is read from the stylesheet and the tile script.
/// </summary>
public sealed class StatTileWrapTitleTests : OmniBunitContext
{
    private const string LongTitle = "Meilleure série de bonnes réponses";

    [Fact]
    public void ByDefault_TheTileKeepsItsCaptionOnOneLine_WithoutTheWrapClass()
    {
        var tile = Render<OmniStatTile>(parameters => parameters
            .Add(component => component.Title, LongTitle)
            .Add(component => component.Value, "42"));

        Assert.False(tile.Instance.WrapTitle);
        Assert.DoesNotContain("omni-stat-tile--wrap", tile.Find(".omni-stat-tile").ClassList);
    }

    [Fact]
    public void WrapTitle_AddsTheWrapClassToTheRootInEveryElementKind()
    {
        var plain = Render<OmniStatTile>(parameters => parameters
            .Add(component => component.Title, LongTitle)
            .Add(component => component.WrapTitle, true));
        var button = Render<OmniStatTile>(parameters => parameters
            .Add(component => component.Title, LongTitle)
            .Add(component => component.WrapTitle, true)
            .Add(component => component.OnClick, () => { }));
        var link = Render<OmniStatTile>(parameters => parameters
            .Add(component => component.Title, LongTitle)
            .Add(component => component.WrapTitle, true)
            .Add(component => component.Href, "/a")
            .Add(component => component.DoubleWidth, true));

        Assert.Contains("omni-stat-tile--wrap", plain.Find("div.omni-stat-tile").ClassList);
        Assert.Contains("omni-stat-tile--wrap", button.Find("button.omni-stat-tile").ClassList);
        var anchor = link.Find("a.omni-stat-tile").ClassList;
        Assert.Contains("omni-stat-tile--wrap", anchor);
        Assert.Contains("omni-tile--double", anchor);
        Assert.Equal(LongTitle, plain.Find(".omni-stat-tile__label").TextContent);
        Assert.Empty(plain.FindAll("[style]"));
    }

    [Fact]
    public void WrapTitle_LetsTheCaptionWrapAtItsNormalSize_EvenWithAFitMarkLeftOver()
    {
        var wrapped = Body(".omni-stat-tile--wrap .omni-stat-tile__label,\n.omni-stat-tile--wrap .omni-stat-tile__label[data-fit]");

        Assert.Equal("normal", Value(wrapped, "white-space"));
        Assert.Equal("clip", Value(wrapped, "text-overflow"));
        Assert.Equal("visible", Value(wrapped, "overflow"));
        Assert.Equal("anywhere", Value(wrapped, "overflow-wrap"));
        Assert.Equal(Value(Body(".omni-stat-tile__label"), "font-size"), Value(wrapped, "font-size"));
        // The default caption is still the one-line one.
        Assert.Equal("nowrap", Value(Body(".omni-stat-tile__label"), "white-space"));
    }

    [Fact]
    public void ATileGrid_StillGivesEveryTheSameHeight_SoAWrappingTileRaisesItsNeighbours()
    {
        Assert.Equal("1fr", Value(Body(".omni-tile-grid"), "grid-auto-rows"));
        Assert.Contains("block-size: 100%", Rules().First(rule => rule.Selector.StartsWith(".omni-tile-grid > :is(", StringComparison.Ordinal)
            && rule.Selector.Contains(".omni-stat-tile", StringComparison.Ordinal)).Body, StringComparison.Ordinal);
    }

    [Fact]
    public void TheTileScript_LeavesAWrappingCaptionAloneAndWatchesItsClass()
    {
        var script = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "OmniEurope.Blazor", "wwwroot", "omni-stat-tile.js"));

        Assert.Contains("classList.contains('omni-stat-tile--wrap')", script, StringComparison.Ordinal);
        Assert.Contains("attributeFilter: ['class']", script, StringComparison.Ordinal);
    }
}
