using Bunit;
using OmniEurope.Blazor.Components;
using static OmniEurope.Blazor.Tests.ShippedLookTests;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Recette R-25: <see cref="OmniStatTile.Size"/> and <see cref="OmniBadge.Size"/> (modifier classes, none for
/// Medium) and the fit of the stat tile value (a data-fit mark written by the tile script, a stylesheet that
/// never cuts the value with an ellipsis). The markup is rendered; the look is read from the stylesheet and the
/// script; the fit in a real page is measured by the showcase probe.
/// </summary>
public sealed class StatTileAndBadgeSizeTests : OmniBunitContext
{
    [Fact]
    public void AStatTile_IsTheStandardSizeByDefault_WithNoSizeClass()
    {
        var tile = Render<OmniStatTile>(parameters => parameters.Add(component => component.Title, "Updated").Add(component => component.Value, "42"));

        Assert.Equal(OmniControlSize.Medium, tile.Instance.Size);
        var classes = tile.Find(".omni-stat-tile").ClassList;
        Assert.DoesNotContain("omni-stat-tile--small", classes);
        Assert.DoesNotContain("omni-stat-tile--large", classes);
    }

    [Theory]
    [InlineData(OmniControlSize.Small, "omni-stat-tile--small")]
    [InlineData(OmniControlSize.Large, "omni-stat-tile--large")]
    public void AStatTileWithASize_CarriesTheClassOfThatSize_InEveryElementKind_AndNoInlineStyle(OmniControlSize size, string expected)
    {
        var plain = Render<OmniStatTile>(parameters => parameters.Add(component => component.Title, "A").Add(component => component.Size, size));
        var button = Render<OmniStatTile>(parameters => parameters.Add(component => component.Title, "A").Add(component => component.Size, size).Add(component => component.OnClick, () => { }));
        var link = Render<OmniStatTile>(parameters => parameters.Add(component => component.Title, "A").Add(component => component.Size, size).Add(component => component.Href, "/a"));

        Assert.Contains(expected, plain.Find("div.omni-stat-tile").ClassList);
        Assert.Contains(expected, button.Find("button.omni-stat-tile").ClassList);
        Assert.Contains(expected, link.Find("a.omni-stat-tile").ClassList);
        Assert.Empty(plain.FindAll("[style]"));
    }

    [Fact]
    public void TheStatTileSizes_SetThePaddingAndTheSizeTheValueStartsFrom()
    {
        Assert.Equal("1.375rem", Value(Body(".omni-stat-tile"), "--omni-stat-tile-value-size"));
        Assert.Equal("1.125rem", Value(Body(".omni-stat-tile--small"), "--omni-stat-tile-value-size"));
        Assert.Equal("1.75rem", Value(Body(".omni-stat-tile--large"), "--omni-stat-tile-value-size"));
        Assert.Equal("var(--omni-stat-tile-value-size)", Value(Body(".omni-stat-tile__value"), "font-size"));
        Assert.Contains("padding-block", Body(".omni-stat-tile--small"), StringComparison.Ordinal);
        Assert.Contains("padding-block", Body(".omni-stat-tile--large"), StringComparison.Ordinal);
    }

    [Fact]
    public void TheStatTileValue_IsNeverCutWithAnEllipsis_ItShrinksThenWraps()
    {
        var value = Body(".omni-stat-tile__value");
        Assert.Equal("clip", Value(value, "text-overflow"));
        Assert.Equal("nowrap", Value(value, "white-space"));

        // Three smaller sizes, none under the floor, then the wrapping state at the smallest size.
        foreach (var step in new[] { "shrink-1", "shrink-2" })
        {
            Assert.StartsWith("max(0.8125rem,", Value(Body($".omni-stat-tile__value[data-fit=\"{step}\"]"), "font-size"), StringComparison.Ordinal);
        }

        var smallest = Value(Body(".omni-stat-tile__value[data-fit=\"shrink-3\"],\n.omni-stat-tile__value[data-fit=\"wrap\"]"), "font-size");
        Assert.StartsWith("max(0.8125rem,", smallest, StringComparison.Ordinal);
        var wrap = Body(".omni-stat-tile__value[data-fit=\"wrap\"]");
        Assert.Equal("normal", Value(wrap, "white-space"));
        Assert.Equal("visible", Value(wrap, "overflow"));
        Assert.Equal("anywhere", Value(wrap, "overflow-wrap"));
        Assert.DoesNotContain("ellipsis", Css.Split("omni-stat-tile__value", 2)[1].Split("omni-stat-tile__label", 2)[0], StringComparison.Ordinal);
    }

    [Fact]
    public void TheTileScript_FitsTheValueWithTheStepsTheStylesheetWrites_AndLendsItNoTooltip()
    {
        var script = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "OmniEurope.Blazor", "wwwroot", "omni-stat-tile.js"));

        Assert.Contains("['shrink-1', 'shrink-2', 'shrink-3', 'wrap']", script, StringComparison.Ordinal);
        Assert.Contains("'.omni-stat-tile__value'", script, StringComparison.Ordinal);
        Assert.DoesNotContain("value.setAttribute('title'", script, StringComparison.Ordinal);
    }

    [Fact]
    public void ABadge_IsTheStandardSizeByDefault_WithNoSizeClass()
    {
        var badge = Render<OmniBadge>(parameters => parameters.AddChildContent("A"));

        Assert.Equal(OmniControlSize.Medium, badge.Instance.Size);
        var classes = badge.Find(".omni-badge").ClassList;
        Assert.DoesNotContain("omni-badge--small", classes);
        Assert.DoesNotContain("omni-badge--large", classes);
    }

    [Theory]
    [InlineData(OmniControlSize.Small, "omni-badge--small")]
    [InlineData(OmniControlSize.Large, "omni-badge--large")]
    public void ABadgeWithASize_CarriesTheClassOfThatSize_InEveryElementKind_AndNoInlineStyle(OmniControlSize size, string expected)
    {
        var plain = Render<OmniBadge>(parameters => parameters.Add(component => component.Size, size).AddChildContent("A"));
        var button = Render<OmniBadge>(parameters => parameters.Add(component => component.Size, size).Add(component => component.OnClick, () => { }).AddChildContent("A"));
        var link = Render<OmniBadge>(parameters => parameters.Add(component => component.Size, size).Add(component => component.Href, "/a").AddChildContent("A"));

        Assert.Contains(expected, plain.Find("span.omni-badge").ClassList);
        Assert.Contains(expected, button.Find("button.omni-badge").ClassList);
        Assert.Contains(expected, link.Find("a.omni-badge").ClassList);
        Assert.Empty(plain.FindAll("[style]"));
    }

    [Fact]
    public void TheBadgeSizes_SetTheBadgeHeightAndTheFontSize()
    {
        var small = Body(".omni-badge--small");
        var large = Body(".omni-badge--large");

        Assert.Equal("calc(var(--omni-control-height) - 0.625rem)", Value(small, "--omni-badge-height"));
        Assert.Equal("calc(var(--omni-control-height) + 0.25rem)", Value(large, "--omni-badge-height"));
        Assert.Contains("--omni-control-font) - 0.1875rem", Value(small, "font-size"), StringComparison.Ordinal);
        Assert.Contains("--omni-control-font) + 0.25rem", Value(large, "font-size"), StringComparison.Ordinal);
    }
}
