using Bunit;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <c>OmniTileGrid.UniformSize</c> (owner decision of 2026-10-24): the common tile size rule of the application,
/// as an option of a grid and not its default. Proved here: the parameter and its class, what the script is
/// handed, the contradiction with <c>EqualHeights="false"</c>, and the shipped rules of the mode (the columns
/// of the grid's own width from the token, one row height with a floor of the density's, the double tile) beside
/// the default rules it must leave alone. The double tile wiring is in <see cref="TileGridDoubleWidthTests"/>;
/// the script in <c>tests/js/tile-grid.test.mjs</c>; the measured columns, heights and spans in a real browser
/// (see the showcase and the probe).
/// </summary>
public sealed class TileGridUniformSizeTests : OmniBunitContext
{
    private const string Function = "applyTileGrid";
    private const string AutoFill = "repeat(auto-fill, minmax(max(min(var(--omni-tile-min), 100%), calc((100% - (var(--omni-tile-columns) - 1) * var(--omni-tile-gap)) / var(--omni-tile-columns))), 1fr))";

    // ---- the parameter ---------------------------------------------------------------------------

    [Fact]
    public void ATileGrid_IsNotUniformByDefault_AndRendersAsItAlwaysHas()
    {
        var grid = Render<OmniTileGrid>(parameters => parameters.AddChildContent("<div>a</div>"));

        Assert.False(grid.Instance.UniformSize);
        var element = grid.Find(".omni-tile-grid");
        Assert.Equal("omni-tile-grid omni-tile-grid--columns-4 omni-tile-grid--gap-md", element.ClassName);
        Assert.False(element.HasAttribute("style"));
        Assert.Empty(JSInterop.Invocations);
    }

    [Fact]
    public void AUniformTileGrid_TakesTheUniformClass_KeepsItsColumnsAndGap_AndLoadsNoScriptWithoutADoubleTile()
    {
        var grid = Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.UniformSize, true)
            .Add(component => component.Columns, 3)
            .Add(component => component.Gap, OmniSpacing.Large)
            .AddChildContent("<div>a</div>"));

        var element = grid.Find(".omni-tile-grid");
        Assert.Equal("omni-tile-grid omni-tile-grid--columns-3 omni-tile-grid--gap-lg omni-tile-grid--uniform", element.ClassName);
        Assert.False(element.HasAttribute("style"));
        Assert.DoesNotContain("style=", grid.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("omni-tile-grid--natural-heights", element.ClassName, StringComparison.Ordinal);
        Assert.DoesNotContain("omni-tile-grid--fluid", element.ClassName, StringComparison.Ordinal);
        Assert.Empty(JSInterop.Invocations);
    }

    [Fact]
    public void AUniformTileGrid_WithAMinColumnWidth_TakesBothClasses_AndHandsTheLengthToTheScript()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Interop);

        var grid = Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.UniformSize, true)
            .Add(component => component.MinColumnWidth, "14rem")
            .AddChildContent("<div>a</div>"));

        grid.WaitForAssertion(() => Assert.Single(module.Invocations[Function]));
        Assert.Equal("14rem", module.Invocations[Function][0].Arguments[1]);
        Assert.Equal(false, module.Invocations[Function][0].Arguments[2]);
        var element = grid.Find(".omni-tile-grid");
        Assert.Contains("omni-tile-grid--uniform", element.ClassList);
        Assert.Contains("omni-tile-grid--fluid", element.ClassList);
        Assert.False(element.HasAttribute("style"));
    }

    [Fact]
    public void AUniformTileGrid_AcceptsEqualHeightsExplicitlyTrue()
    {
        var grid = Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.UniformSize, true)
            .Add(component => component.EqualHeights, true)
            .AddChildContent("<div>a</div>"));

        Assert.Contains("omni-tile-grid--uniform", grid.Find(".omni-tile-grid").ClassList);
    }

    [Fact]
    public void AUniformTileGrid_OfNaturalHeights_IsAContradiction_ThatThrowsAnArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.UniformSize, true)
            .Add(component => component.EqualHeights, false)
            .AddChildContent("<div>a</div>")));

        Assert.Equal(nameof(OmniTileGrid.UniformSize), exception.ParamName);
        Assert.Contains(nameof(OmniTileGrid.UniformSize), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(OmniTileGrid.EqualHeights), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ANaturalHeightsGrid_IsStillAccepted_AsLongAsItIsNotUniform()
    {
        var grid = Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.EqualHeights, false)
            .AddChildContent("<div>a</div>"));

        var element = grid.Find(".omni-tile-grid");
        Assert.Contains("omni-tile-grid--natural-heights", element.ClassList);
        Assert.DoesNotContain("omni-tile-grid--uniform", element.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void TurningAGridUniform_OrItsHeightsNatural_AtRunTime_IsCheckedToo()
    {
        var uniform = Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.UniformSize, true)
            .AddChildContent("<div>a</div>"));
        Assert.Throws<ArgumentException>(() => uniform.Render(parameters => parameters.Add(component => component.EqualHeights, false)));

        var natural = Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.EqualHeights, false)
            .AddChildContent("<div>a</div>"));
        Assert.Throws<ArgumentException>(() => natural.Render(parameters => parameters.Add(component => component.UniformSize, true)));
    }

    [Fact]
    public void AUniformTileGrid_TellsTheScriptAgain_WhenItsClassAttributeChanges()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Interop);
        var grid = Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.UniformSize, true)
            .AddChildContent<OmniNavTile>(tile => tile.Add(component => component.Href, "/a").Add(component => component.Title, "A").Add(component => component.DoubleWidth, true)));
        grid.WaitForAssertion(() => Assert.Single(module.Invocations[Function]));

        // A redraw that changes nothing the script knows does not call it again.
        grid.Render(parameters => parameters.Add(component => component.Id, "tiles"));
        Assert.Single(module.Invocations[Function]);

        // A new class string (the columns, then the host's own class) replaces the one the script marked.
        grid.Render(parameters => parameters.Add(component => component.Columns, 2));
        grid.WaitForAssertion(() => Assert.Equal(2, module.Invocations[Function].Count));
        grid.Render(parameters => parameters.Add(component => component.Class, "wide"));
        grid.WaitForAssertion(() => Assert.Equal(3, module.Invocations[Function].Count));
        Assert.All(module.Invocations[Function], call => Assert.Equal(true, call.Arguments[2]));
    }

    [Fact]
    public void AMinColumnWidthGrid_TellsTheScriptAgain_WhenTheHostClassChanges()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Interop);
        var grid = Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.MinColumnWidth, "18rem")
            .AddChildContent("<div>a</div>"));
        grid.WaitForAssertion(() => Assert.Single(module.Invocations[Function]));

        grid.Render(parameters => parameters.Add(component => component.Class, "wide"));

        grid.WaitForAssertion(() => Assert.Equal(2, module.Invocations[Function].Count));
    }

    // ---- the shipped rules -----------------------------------------------------------------------

    [Fact]
    public void TheUniformRule_MakesTheLargestCountThatKeepsEachColumnWideEnough_FromTheTokenAndNotTheWindow()
    {
        var css = ShippedLookTests.Css;
        var uniform = ShippedLookTests.Body(".omni-tile-grid.omni-tile-grid--uniform");

        Assert.Equal(AutoFill, ShippedLookTests.Value(uniform, "grid-template-columns"));
        // The grid's own minimum is the shared token until a MinColumnWidth writes another one through the
        // CSSOM, and is declared on every uniform grid so a grid nested in a tile never inherits the length of
        // the grid around it.
        Assert.Equal("var(--omni-tile-min-width)", ShippedLookTests.Value(uniform, "--omni-tile-min"));
        // The implicit tracks of a double tile in a grid of one column are told from the real ones by being empty.
        Assert.Equal("0px", ShippedLookTests.Value(uniform, "grid-auto-columns"));
        // The uniform grid reads the full count, never the two columns of a window of 40 to 64rem.
        Assert.DoesNotContain("--omni-tile-columns-sm", uniform, StringComparison.Ordinal);

        // It sits after both media rules of the default grid and outweighs them, so it wins at every window width,
        // and after the fluid rule, so a grid that sets both gets the same columns from its own minimum.
        var rule = css.IndexOf(".omni-tile-grid.omni-tile-grid--uniform {", StringComparison.Ordinal);
        Assert.True(rule > css.IndexOf("@media (min-width: 64rem) {\n    .omni-tile-grid", StringComparison.Ordinal), "The uniform rule must follow the media rules of the tile grid.");
        Assert.True(rule > css.IndexOf(".omni-tile-grid.omni-tile-grid--fluid {", StringComparison.Ordinal), "The uniform rule must follow the fluid rule.");
    }

    [Fact]
    public void TheUniformRule_GivesEveryRowTheTallestTileOfTheGrid_AndAFloorOfTheDensity()
    {
        var uniform = ShippedLookTests.Body(".omni-tile-grid.omni-tile-grid--uniform");

        Assert.Equal("minmax(var(--omni-tile-height), 1fr)", ShippedLookTests.Value(uniform, "grid-auto-rows"));
        // The tiles fill their cell, as in any grid with equal heights (the natural-heights modifier is the only
        // thing that lifts it, and it cannot come with UniformSize).
        var tiles = ShippedLookTests.Body(".omni-tile-grid > :is(.omni-card, .omni-selectable-card, .omni-stat-tile, .omni-fact-tile, .omni-nav-tile)");
        Assert.Equal("100%", ShippedLookTests.Value(tiles, "block-size"));
        Assert.Equal("0", ShippedLookTests.Value(tiles, "min-block-size"));
    }

    [Fact]
    public void ADoubleTileOfAUniformGrid_SpansTwoColumnsOnlyWhereTheScriptSawTwoOrMore()
    {
        var css = ShippedLookTests.Css;
        var alone = ShippedLookTests.Body(".omni-tile-grid--uniform > .omni-tile--double");
        var several = ShippedLookTests.Body(".omni-tile-grid--uniform.omni-tile-grid--multi-column > .omni-tile--double");

        // Safe by default: a grid the script has not marked (not yet, or never interactive) opens no implicit column.
        Assert.Equal("auto", ShippedLookTests.Value(alone, "grid-column"));
        Assert.Equal("span 2", ShippedLookTests.Value(several, "grid-column"));
        // The plain rule overrides the 40rem one of the default grid that spans two columns.
        Assert.True(
            css.IndexOf(".omni-tile-grid--uniform > .omni-tile--double {", StringComparison.Ordinal) > css.IndexOf("@media (min-width: 40rem) {\n    .omni-tile-grid", StringComparison.Ordinal),
            "The uniform double tile rule must follow the media rules of the tile grid.");
    }

    [Fact]
    public void TheDefaultGridRule_LeavesTheUniformTokensAlone()
    {
        var grid = ShippedLookTests.Body(".omni-tile-grid");

        // None of the uniform mode leaks into a grid that does not ask for it: its rows, its columns, the media
        // rules and the double tile rule are those it has always had.
        Assert.DoesNotContain("--omni-tile-min", grid, StringComparison.Ordinal);
        Assert.DoesNotContain("--omni-tile-height", grid, StringComparison.Ordinal);
        Assert.Equal("1fr", ShippedLookTests.Value(grid, "grid-auto-rows"));
        Assert.Equal("minmax(0, 1fr)", ShippedLookTests.Value(grid, "grid-template-columns"));
        Assert.Equal("span 2", ShippedLookTests.Value(ShippedLookTests.Body(".omni-tile-grid > .omni-tile--double"), "grid-column"));
        Assert.Equal("auto", ShippedLookTests.Value(ShippedLookTests.Body(".omni-tile-grid--columns-1 > .omni-tile--double"), "grid-column"));
        Assert.Contains("@media (min-width: 40rem) {\n    .omni-tile-grid", ShippedLookTests.Css, StringComparison.Ordinal);
        Assert.Contains("@media (min-width: 64rem) {\n    .omni-tile-grid", ShippedLookTests.Css, StringComparison.Ordinal);
    }

    [Fact]
    public void TheTileTokens_AreDeclared_TheWidthOnTheRootInRem_TheHeightInEveryDensityBlock()
    {
        Assert.Equal("18rem", ShippedLookTests.Value(ShippedLookTests.BodyWith(":root", "--omni-tile-min-width:"), "--omni-tile-min-width"));

        // The height floor follows the density (the same token set per level is proved by ShippedLookDensityTests).
        foreach (var (density, height) in new[]
        {
            (":root,\n[data-omni-density=\"comfortable\"]", "10rem"),
            ("[data-omni-density=\"compact\"]", "8.5rem"),
            ("[data-omni-density=\"spacious\"]", "11.5rem")
        })
        {
            Assert.Equal(height, ShippedLookTests.Value(ShippedLookTests.Body(density), "--omni-tile-height"));
        }
    }

    [Fact]
    public void NoContainerIsDeclaredOnTheTileGrid_SoItStaysTheOriginOfNoFixedDescendant()
    {
        // A container-type would contain the layout of the grid and make a position: fixed descendant
        // (a dialog, a menu) resolve against it instead of the window.
        Assert.DoesNotContain(
            ShippedLookTests.Rules(),
            rule => rule.Selector.Contains(".omni-tile-grid", StringComparison.Ordinal)
                && rule.Body.Contains("container-type", StringComparison.Ordinal));
    }
}
