using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Three requests of a client application: a common width for the badges of a column (R1-23), one size rule
/// for every tile of a grid of tiles (R1-20) and a visible check box on the selectable cards that bind
/// several choices (R2-4). The markup and the shipped rules are proved here; the measured sizes are proved
/// in a real browser (see the showcase).
/// </summary>
public sealed class TileRuleAndBadgeWidthTests : OmniBunitContext
{
    private enum RunState
    {
        Failed,
        Succeeded
    }

    private static readonly OmniStatusMap<RunState> RunStates = new()
    {
        { RunState.Failed, OmniTone.Danger, "Échoué", OmniIconName.Error },
        { RunState.Succeeded, OmniTone.Success, "Succès", OmniIconName.CheckCircle }
    };

    // ---- R1-23: the badge width ---------------------------------------------------------------

    [Fact]
    public void ABadge_KeepsTheWidthOfItsContent_UnlessAStepIsAsked()
    {
        var badge = Render<OmniBadge>(parameters => parameters.AddChildContent("Échoué"));

        Assert.Equal(OmniBadgeWidth.Auto, badge.Instance.Width);
        Assert.DoesNotContain("omni-badge--width", badge.Find(".omni-badge").ClassName, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(OmniBadgeWidth.Small, "omni-badge--width-small")]
    [InlineData(OmniBadgeWidth.Medium, "omni-badge--width-medium")]
    [InlineData(OmniBadgeWidth.Large, "omni-badge--width-large")]
    public void ABadgeWithAStep_CarriesTheClassOfThatStep_AndNoInlineStyle(OmniBadgeWidth width, string expected)
    {
        var badge = Render<OmniBadge>(parameters => parameters.Add(component => component.Width, width).AddChildContent("SPF"));

        var element = badge.Find(".omni-badge");
        Assert.Contains(expected, element.ClassList);
        Assert.False(element.HasAttribute("style"));
    }

    [Fact]
    public void AnIconOnlyBadge_StaysASquare_WhateverTheStep()
    {
        var badge = Render<OmniBadge>(parameters => parameters
            .Add(component => component.Width, OmniBadgeWidth.Large)
            .Add(component => component.Icon, (RenderFragment)(builder => builder.AddContent(0, "i"))));

        Assert.Contains("omni-badge--icon", badge.Find(".omni-badge").ClassList);
        Assert.DoesNotContain("omni-badge--width", badge.Find(".omni-badge").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void AStatusBadge_PassesItsStepToItsBadge_SoAColumnOfStatusWordsLinesUp()
    {
        var failed = Render<OmniStatusBadge<RunState>>(parameters => parameters
            .Add(component => component.Value, RunState.Failed)
            .Add(component => component.Map, RunStates)
            .Add(component => component.Width, OmniBadgeWidth.Medium));
        var succeeded = Render<OmniStatusBadge<RunState>>(parameters => parameters
            .Add(component => component.Value, RunState.Succeeded)
            .Add(component => component.Map, RunStates)
            .Add(component => component.Width, OmniBadgeWidth.Medium));
        var plain = Render<OmniStatusBadge<RunState>>(parameters => parameters
            .Add(component => component.Value, RunState.Failed)
            .Add(component => component.Map, RunStates));

        Assert.Contains("omni-badge--width-medium", failed.Find(".omni-badge").ClassList);
        Assert.Contains("omni-badge--width-medium", succeeded.Find(".omni-badge").ClassList);
        Assert.DoesNotContain("omni-badge--width", plain.Find(".omni-badge").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void TheWidthSteps_AreFloorsInEmOfTheBadgeText_ThatAHostCanRetune()
    {
        // A floor (min-inline-size), never a fixed width: a longer text widens its badge and nothing is cut.
        foreach (var (step, fallback) in new[] { ("small", "5.5em"), ("medium", "7.5em"), ("large", "10em") })
        {
            var body = ShippedLookTests.Body($".omni-badge--width-{step}");
            Assert.Equal($"var(--omni-badge-width-{step}, {fallback})", ShippedLookTests.Value(body, "min-inline-size"));
            Assert.DoesNotContain("max-inline-size", body, StringComparison.Ordinal);
            Assert.DoesNotMatch(@"(?:^|[;{\s])(?:inline-size|width)\s*:", body);
        }

        // The steps come after the badge rule that sets its own floor, so they win over it.
        var css = ShippedLookTests.Css;
        Assert.True(
            css.IndexOf(".omni-badge--width-small", StringComparison.Ordinal) > css.IndexOf("min-width: var(--omni-badge-height)", StringComparison.Ordinal),
            "The width steps must follow the base badge rule.");
    }

    // ---- R1-20: the tile rule -----------------------------------------------------------------

    [Fact]
    public void ATileGrid_HasFourEqualColumnsAndTheMediumGapByDefault()
    {
        var grid = Render<OmniTileGrid>(parameters => parameters.AddChildContent("<div>a</div>"));

        var element = grid.Find(".omni-tile-grid");
        Assert.Contains("omni-tile-grid--columns-4", element.ClassList);
        Assert.Contains("omni-tile-grid--gap-md", element.ClassList);
        Assert.False(element.HasAttribute("style"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(6)]
    public void ATileGrid_TakesBetweenOneAndSixColumns(int columns)
    {
        var grid = Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.Columns, columns)
            .Add(component => component.Gap, OmniSpacing.Large)
            .AddChildContent("<div>a</div>"));

        Assert.Contains($"omni-tile-grid--columns-{columns}", grid.Find(".omni-tile-grid").ClassList);
        Assert.Contains("omni-tile-grid--gap-lg", grid.Find(".omni-tile-grid").ClassList);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(-1)]
    public void ATileGrid_RefusesAColumnCountOutsideOneToSix(int columns)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.Columns, columns)
            .AddChildContent("<div>a</div>")));

        Assert.Equal("Columns", exception.ParamName);
    }

    [Fact]
    public void EveryTile_MarksItselfDoubleWidth_AndOnlyWhenAsked()
    {
        var card = Render<OmniCard>(parameters => parameters.Add(component => component.ChildContent, "x").Add(component => component.DoubleWidth, true));
        var selectable = Render<OmniSelectableCard>(parameters => parameters.Add(component => component.Title, "Linux").Add(component => component.DoubleWidth, true));
        var stat = Render<OmniStatTile>(parameters => parameters.Add(component => component.Title, "Jetons").Add(component => component.DoubleWidth, true));
        var statButton = Render<OmniStatTile>(parameters => parameters.Add(component => component.Title, "Jetons").Add(component => component.OnClick, () => { }).Add(component => component.DoubleWidth, true));
        var nav = Render<OmniNavTile>(parameters => parameters.Add(component => component.Href, "/a").Add(component => component.Title, "A").Add(component => component.DoubleWidth, true));

        Assert.Contains("omni-tile--double", card.Find(".omni-card").ClassList);
        Assert.Contains("omni-tile--double", selectable.Find(".omni-selectable-card").ClassList);
        Assert.Contains("omni-tile--double", stat.Find(".omni-stat-tile").ClassList);
        Assert.Contains("omni-tile--double", statButton.Find(".omni-stat-tile").ClassList);
        Assert.Contains("omni-tile--double", nav.Find(".omni-nav-tile").ClassList);

        var single = Render<OmniNavTile>(parameters => parameters.Add(component => component.Href, "/a").Add(component => component.Title, "A"));
        Assert.DoesNotContain("omni-tile--double", single.Find(".omni-nav-tile").ClassList);
    }

    [Fact]
    public void ATileGrid_KeepsTheTilesAsItsDirectChildren_SoTheRuleReachesThem()
    {
        var grid = Render<OmniTileGrid>(parameters => parameters.AddChildContent<OmniNavTile>(tile => tile
            .Add(component => component.Href, "/a")
            .Add(component => component.Title, "A")));

        Assert.Equal("a", grid.Find(".omni-tile-grid").FirstElementChild!.TagName.ToLowerInvariant());
    }

    [Fact]
    public void ACardGroupInColumns_BecomesATileGrid_AndWithoutColumnsStaysAStack()
    {
        var stacked = RenderGroup(columns: null);
        var tiled = RenderGroup(columns: 2);

        Assert.DoesNotContain("omni-tile-grid", stacked.Find(".omni-selectable-card-group").ClassName, StringComparison.Ordinal);
        var element = tiled.Find(".omni-selectable-card-group");
        Assert.Contains("omni-tile-grid", element.ClassList);
        Assert.Contains("omni-tile-grid--columns-2", element.ClassList);
        Assert.Throws<ArgumentOutOfRangeException>(() => RenderGroup(columns: 7));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ACardGroupInColumns_RefusesAColumnCountBelowOne(int columns)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => RenderGroup(columns));

        Assert.Equal("Columns", exception.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    public void ACardGroupInColumns_AcceptsTheBoundsOfOneAndSix(int columns)
    {
        var group = RenderGroup(columns);

        Assert.Contains($"omni-tile-grid--columns-{columns}", group.Find(".omni-selectable-card-group").ClassList);
    }

    [Fact]
    public void TheTileRule_GivesEqualColumns_OneRowHeight_AndADoubleTileTwoColumns()
    {
        var grid = ShippedLookTests.Body(".omni-tile-grid");
        // Every row is as tall as the tallest tile of the grid: the same height for all.
        Assert.Equal("1fr", ShippedLookTests.Value(grid, "grid-auto-rows"));
        Assert.Equal("grid", ShippedLookTests.Value(grid, "display"));
        Assert.Equal("minmax(0, 1fr)", ShippedLookTests.Value(grid, "grid-template-columns"));

        var wide = ShippedLookTests.Rules().Where(rule => rule.Selector == ".omni-tile-grid").Select(rule => rule.Body).ToArray();
        Assert.Contains(wide, body => body.Contains("repeat(var(--omni-tile-columns-sm), minmax(0, 1fr))", StringComparison.Ordinal));
        Assert.Contains(wide, body => body.Contains("repeat(var(--omni-tile-columns), minmax(0, 1fr))", StringComparison.Ordinal));
        Assert.Equal("span 2", ShippedLookTests.Value(ShippedLookTests.Body(".omni-tile-grid > .omni-tile--double"), "grid-column"));
        // A grid of one column never opens an implicit column for a double tile.
        Assert.Equal("auto", ShippedLookTests.Value(ShippedLookTests.Body(".omni-tile-grid--columns-1 > .omni-tile--double"), "grid-column"));
    }

    [Fact]
    public void TheTileRule_NeverLetsAContentSpill()
    {
        var tiles = ShippedLookTests.Body(".omni-tile-grid > :is(.omni-card, .omni-selectable-card, .omni-stat-tile, .omni-fact-tile, .omni-nav-tile)");
        Assert.Equal("100%", ShippedLookTests.Value(tiles, "block-size"));
        Assert.Equal("0", ShippedLookTests.Value(tiles, "min-inline-size"));
        Assert.Equal("0", ShippedLookTests.Value(tiles, "min-block-size"));
        Assert.Equal("border-box", ShippedLookTests.Value(tiles, "box-sizing"));
        // A card keeps its header and footer and scrolls its body; the other tiles wrap or cut their words.
        Assert.Equal("auto", ShippedLookTests.Value(ShippedLookTests.Body(".omni-tile-grid > .omni-card > .omni-card__body"), "overflow"));
        Assert.Equal("anywhere", ShippedLookTests.Value(ShippedLookTests.Body(".omni-selectable-card__text"), "overflow-wrap"));
        Assert.Equal("anywhere", ShippedLookTests.Value(ShippedLookTests.Body(".omni-nav-tile__title"), "overflow-wrap"));
        Assert.Contains("overflow: hidden", ShippedLookTests.Body(".omni-stat-tile__value"), StringComparison.Ordinal);
        Assert.Contains("overflow: hidden", ShippedLookTests.Body(".omni-stat-tile__label"), StringComparison.Ordinal);
        Assert.Equal("anywhere", ShippedLookTests.Value(ShippedLookTests.Body(".omni-stat-tile__detail"), "overflow-wrap"));
    }

    [Fact]
    public void TheTileGrid_FollowsTheBreakpointsOfTheColumns()
    {
        var css = ShippedLookTests.Css;
        var small = css.IndexOf("@media (min-width: 40rem) {\n    .omni-tile-grid", StringComparison.Ordinal);
        var medium = css.IndexOf("@media (min-width: 64rem) {\n    .omni-tile-grid", StringComparison.Ordinal);

        Assert.True(small >= 0, "The two-column breakpoint (40rem) of the tile grid is missing.");
        Assert.True(medium > small, "The full-width breakpoint (64rem) of the tile grid must follow the 40rem one.");
    }

    // ---- R2-4: the visible check box of a check box card --------------------------------------

    [Fact]
    public void ACheckBoxCard_DrawsADecorativeBox_ThatIsNeitherAControlNorAnnounced()
    {
        var card = Render<OmniSelectableCard>(parameters => parameters
            .Add(component => component.Title, "Docker")
            .Add(component => component.Multiple, true)
            .Add(component => component.Value, true));

        var button = card.Find("button.omni-selectable-card");
        var box = button.QuerySelector(".omni-selectable-card__box");
        Assert.NotNull(box);
        Assert.Equal("true", box.GetAttribute("aria-hidden"));
        Assert.False(box.HasAttribute("tabindex"));
        Assert.False(box.HasAttribute("role"));
        // One control, announced once: the card is the checkbox, the box is not a second one.
        Assert.Equal("checkbox", button.GetAttribute("role"));
        Assert.Equal("true", button.GetAttribute("aria-checked"));
        Assert.Empty(card.FindAll("input, [role=checkbox]:not(button)"));
        Assert.Single(card.FindAll("[tabindex], button, a, input"));
        // The box adds no text to the accessible name.
        Assert.Equal(string.Empty, box.TextContent);
    }

    [Fact]
    public void TheBox_FollowsTheChoice_AndAClickOnItIsAClickOnTheCard()
    {
        var picked = new List<bool>();
        var card = Render<OmniSelectableCard>(parameters => parameters
            .Add(component => component.Title, "Docker")
            .Add(component => component.Multiple, true)
            .Add(component => component.Value, false)
            .Add(component => component.ValueChanged, value => picked.Add(value)));

        Assert.DoesNotContain("omni-selectable-card--selected", card.Find("button").ClassList);
        card.Find(".omni-selectable-card__box").Click();
        Assert.Equal([true], picked);

        card.Render(parameters => parameters.Add(component => component.Value, true));
        Assert.Contains("omni-selectable-card--selected", card.Find("button").ClassList);
        Assert.Equal("true", card.Find("button").GetAttribute("aria-checked"));
        Assert.Single(card.FindAll(".omni-selectable-card__box"));
    }

    [Fact]
    public void ARadioCard_NeverDrawsABox_AndShowCheckBoxFalseRemovesIt()
    {
        var radio = Render<OmniSelectableCard>(parameters => parameters.Add(component => component.Title, "Linux"));
        var plain = Render<OmniSelectableCard>(parameters => parameters
            .Add(component => component.Title, "Docker")
            .Add(component => component.Multiple, true)
            .Add(component => component.ShowCheckBox, false));

        Assert.Empty(radio.FindAll(".omni-selectable-card__box"));
        Assert.Empty(plain.FindAll(".omni-selectable-card__box"));
        Assert.Equal("checkbox", plain.Find("button").GetAttribute("role"));
    }

    [Fact]
    public void ACheckBoxGroup_DrawsTheBoxOnEveryCard_AndTheGroupDecidesForItsCards()
    {
        IReadOnlyList<string> value = ["linux"];
        var group = Render<OmniSelectableCardGroup<string, IReadOnlyList<string>>>(parameters => parameters
            .Add(component => component.Options, [new OmniOption<string>("linux", "Linux"), new OmniOption<string>("bsd", "BSD")])
            .Add(component => component.Multiple, true)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        Assert.Equal(2, group.FindAll(".omni-selectable-card__box").Count);
        var cards = group.FindAll("button.omni-selectable-card");
        Assert.Contains("omni-selectable-card--selected", cards[0].ClassList);
        Assert.DoesNotContain("omni-selectable-card--selected", cards[1].ClassList);
        Assert.All(cards, card => Assert.Equal("checkbox", card.GetAttribute("role")));

        group.Render(parameters => parameters.Add(component => component.ShowCheckBox, false));
        Assert.Empty(group.FindAll(".omni-selectable-card__box"));

        var radio = Render<OmniSelectableCardGroup<string, string?>>(parameters => parameters
            .Add(component => component.Options, [new OmniOption<string>("linux", "Linux")])
            .Add(component => component.Value, "linux")
            .Add(component => component.ValueExpression, () => _bound));
        Assert.Empty(radio.FindAll(".omni-selectable-card__box"));
    }

    [Fact]
    public void TheBoxRule_KeepsOneSizeWhetherCheckedOrNot_AndPaintsTheAccentWhenChosen()
    {
        var box = ShippedLookTests.Body(".omni-selectable-card__box");
        Assert.Equal("var(--omni-check-size)", ShippedLookTests.Value(box, "block-size"));
        Assert.Equal("var(--omni-check-size)", ShippedLookTests.Value(box, "inline-size"));

        var selected = ShippedLookTests.Body(".omni-selectable-card--selected .omni-selectable-card__box");
        Assert.Equal("var(--omni-color-accent-fill)", ShippedLookTests.Value(selected, "background"));
        Assert.DoesNotContain("block-size", selected, StringComparison.Ordinal);
        Assert.DoesNotContain("border-width", selected, StringComparison.Ordinal);
        Assert.Equal("0", ShippedLookTests.Value(ShippedLookTests.Body(".omni-selectable-card__box::before"), "opacity"));
        Assert.Equal("1", ShippedLookTests.Value(ShippedLookTests.Body(".omni-selectable-card--selected .omni-selectable-card__box::before"), "opacity"));
    }

    private readonly string? _bound = null;

    private IRenderedComponent<OmniSelectableCardGroup<string, string?>> RenderGroup(int? columns) =>
        Render<OmniSelectableCardGroup<string, string?>>(parameters => parameters
            .Add(component => component.Options, [new OmniOption<string>("a", "A"), new OmniOption<string>("b", "B")])
            .Add(component => component.Columns, columns)
            .Add(component => component.Value, "a")
            .Add(component => component.ValueExpression, () => _bound));
}
