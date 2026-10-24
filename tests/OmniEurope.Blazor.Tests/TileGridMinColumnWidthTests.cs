using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <c>OmniTileGrid.MinColumnWidth</c> (a request of a client application): the columns of a tile grid follow
/// the width of the grid, not that of the window. The markup, the value handed to the interop script and the
/// shipped rules are proved here; the script itself by <c>tests/js/tile-grid.test.mjs</c>; the measured
/// columns are proved in a real browser (see the showcase).
/// </summary>
public sealed class TileGridMinColumnWidthTests : OmniBunitContext
{
    private const string Function = "applyTileGrid";

    [Fact]
    public void ATileGridWithoutMinColumnWidth_KeepsTheColumnsOfTheWindow_AndLoadsNoScript()
    {
        var grid = Render<OmniTileGrid>(parameters => parameters.AddChildContent("<div>a</div>"));

        Assert.Null(grid.Instance.MinColumnWidth);
        Assert.DoesNotContain("omni-tile-grid--fluid", grid.Find(".omni-tile-grid").ClassName, StringComparison.Ordinal);
        Assert.False(grid.Find(".omni-tile-grid").HasAttribute("style"));
        Assert.Empty(JSInterop.Invocations);
    }

    [Theory]
    [InlineData("18rem")]
    [InlineData("240px")]
    [InlineData("30ch")]
    [InlineData("40%")]
    [InlineData("20vw")]
    [InlineData("12.5em")]
    public void ATileGridWithAMinColumnWidth_IsFluid_AndHandsTheLengthToTheScript_NeverAsAStyleAttribute(string width)
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Interop);

        var grid = Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.Columns, 3)
            .Add(component => component.MinColumnWidth, width)
            .AddChildContent("<div>a</div>"));

        grid.WaitForAssertion(() => Assert.Single(module.Invocations[Function]));
        Assert.Equal(width, module.Invocations[Function][0].Arguments[1]);
        var element = grid.Find(".omni-tile-grid");
        Assert.Contains("omni-tile-grid--fluid", element.ClassList);
        Assert.Contains("omni-tile-grid--columns-3", element.ClassList);
        Assert.False(element.HasAttribute("style"));
        Assert.DoesNotContain("style=", grid.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ATileGrid_WritesItsMinColumnWidthAgainOnlyWhenItOrItsClassesChange_AndRemovesItWhenNull()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Interop);
        var grid = Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.MinColumnWidth, "18rem")
            .AddChildContent("<div>a</div>"));
        grid.WaitForAssertion(() => Assert.Single(module.Invocations[Function]));

        // A redraw that changes nothing the script knows does not call it again.
        grid.Render(parameters => parameters.Add(component => component.Id, "tiles"));
        Assert.Single(module.Invocations[Function]);

        // A new length is written.
        grid.Render(parameters => parameters.Add(component => component.MinColumnWidth, "20rem"));
        grid.WaitForAssertion(() => Assert.Equal(2, module.Invocations[Function].Count));
        Assert.Equal("20rem", module.Invocations[Function][1].Arguments[1]);

        // A new class string replaces the one the script marked, so the script measures again.
        grid.Render(parameters => parameters.Add(component => component.Columns, 2));
        grid.WaitForAssertion(() => Assert.Equal(3, module.Invocations[Function].Count));
        Assert.Equal("20rem", module.Invocations[Function][2].Arguments[1]);

        // No length: the script removes the property and stops watching, and the class is gone.
        grid.Render(parameters => parameters.Add(component => component.MinColumnWidth, (string?)null));
        grid.WaitForAssertion(() => Assert.Equal(4, module.Invocations[Function].Count));
        Assert.Null(module.Invocations[Function][3].Arguments[1]);
        Assert.DoesNotContain("omni-tile-grid--fluid", grid.Find(".omni-tile-grid").ClassName, StringComparison.Ordinal);

        // And nothing is asked of it again while it stays null.
        grid.Render(parameters => parameters.Add(component => component.Columns, 3));
        Assert.Equal(4, module.Invocations[Function].Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("18")]
    [InlineData("0rem")]
    [InlineData("-18rem")]
    [InlineData(" 18rem")]
    [InlineData("18REM")]
    [InlineData("18rem; color: red")]
    [InlineData("calc(100% - 2rem)")]
    [InlineData("var(--x)")]
    [InlineData("auto")]
    [InlineData("18vh")]
    public void ATileGrid_RefusesAMinColumnWidthThatIsNotAPositiveWidth(string width)
    {
        var exception = Assert.Throws<ArgumentException>(() => Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.MinColumnWidth, width)
            .AddChildContent("<div>a</div>")));

        Assert.Equal(nameof(OmniTileGrid.MinColumnWidth), exception.ParamName);
    }

    [Fact]
    public async Task ATileGridOnALostCircuit_NeitherSizesNorFails_AndStillReleasesItsScript()
    {
        var runtime = new ManualJSRuntime { Module = new RecordingModule { DisposeFailure = new JSDisconnectedException("gone") } };
        runtime.Module.CallFailures[Function] = new JSDisconnectedException("gone");
        Services.AddSingleton<IJSRuntime>(runtime);
        var grid = Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.MinColumnWidth, "18rem")
            .AddChildContent("<div>a</div>"));
        grid.WaitForAssertion(() => Assert.Equal([Function], runtime.Module.Calls));

        await grid.Instance.DisposeAsync();

        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
    }

    [Fact]
    public async Task ATileGrid_StopsTheScriptWatchingItWhenDisposed_ThenReleasesTheScript()
    {
        var runtime = new ManualJSRuntime();
        Services.AddSingleton<IJSRuntime>(runtime);
        var grid = Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.MinColumnWidth, "18rem")
            .AddChildContent("<div>a</div>"));
        grid.WaitForAssertion(() => Assert.Equal([Function], runtime.Module.Calls));

        await grid.Instance.DisposeAsync();

        Assert.Equal([Function, Function], runtime.Module.Calls);
        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
    }

    [Fact]
    public async Task ATileGridThatNeverSetAMinColumnWidth_LoadsAndReleasesNoScript()
    {
        var runtime = new ManualJSRuntime();
        Services.AddSingleton<IJSRuntime>(runtime);
        var grid = Render<OmniTileGrid>(parameters => parameters.AddChildContent("<div>a</div>"));

        await grid.Instance.DisposeAsync();

        Assert.Empty(runtime.Module.Calls);
        Assert.False(runtime.Module.Disposal.Task.IsCompleted);
    }

    [Fact]
    public async Task ATileGridDisposedWhileItsScriptIsLoading_ReleasesTheScriptOnArrival_AndWritesNothing()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        Services.AddSingleton<IJSRuntime>(runtime);
        var grid = Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.MinColumnWidth, "18rem")
            .AddChildContent("<div>a</div>"));

        await grid.Instance.DisposeAsync();
        runtime.PendingImport.SetResult(runtime.Module);

        await runtime.Module.Disposal.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        Assert.Empty(runtime.Module.Calls);
    }

    // ---- the shipped rules ---------------------------------------------------------------------

    [Fact]
    public void TheFluidRule_MakesTheLargestCountThatKeepsEachColumnWideEnough_CappedAtColumns_AtEveryWidth()
    {
        var css = ShippedLookTests.Css;
        var fluid = ShippedLookTests.Body(".omni-tile-grid.omni-tile-grid--fluid");

        Assert.Equal(
            "repeat(auto-fill, minmax(max(min(var(--omni-tile-min, 100%), 100%), calc((100% - (var(--omni-tile-columns) - 1) * var(--omni-tile-gap)) / var(--omni-tile-columns))), 1fr))",
            ShippedLookTests.Value(fluid, "grid-template-columns"));
        // The implicit tracks of a double tile in a grid of one column are told from the real ones by being empty.
        Assert.Equal("0px", ShippedLookTests.Value(fluid, "grid-auto-columns"));

        // It sits after both media rules and outweighs them, so it wins at every window width.
        Assert.True(
            css.IndexOf(".omni-tile-grid.omni-tile-grid--fluid {", StringComparison.Ordinal) > css.IndexOf("@media (min-width: 64rem) {\n    .omni-tile-grid", StringComparison.Ordinal),
            "The fluid rule must follow the media rules of the tile grid.");

        // The fluid grid reads the full count, never the two columns of a window of 40 to 64rem.
        Assert.DoesNotContain("--omni-tile-columns-sm", fluid, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryGapClass_PublishesItsGapAsAVariable_AndKeepsItsGap()
    {
        var expected = new Dictionary<string, (string Variable, string Gap)>
        {
            ["none"] = ("0px", "0"),
            ["xs"] = ("var(--omni-space-xs)", "var(--omni-space-xs)"),
            ["sm"] = ("var(--omni-space-sm)", "var(--omni-space-sm)"),
            ["md"] = ("var(--omni-space-md)", "var(--omni-space-md)"),
            ["lg"] = ("var(--omni-space-lg)", "var(--omni-space-lg)"),
            ["xl"] = ("var(--omni-space-xl)", "var(--omni-space-xl)")
        };

        foreach (var (name, (variable, gap)) in expected)
        {
            var body = ShippedLookTests.Body($".omni-tile-grid--gap-{name}");
            Assert.Equal(variable, ShippedLookTests.Value(body, "--omni-tile-gap"));
            Assert.Equal(gap, ShippedLookTests.Value(body, "gap"));
        }

        // The default gap is the medium one, as the grid class without a gap class has always had.
        var grid = ShippedLookTests.Body(".omni-tile-grid");
        Assert.Equal("var(--omni-space-md)", ShippedLookTests.Value(grid, "--omni-tile-gap"));
        Assert.Equal("var(--omni-space-md)", ShippedLookTests.Value(grid, "gap"));
    }

    [Fact]
    public void ADoubleTileOfAFluidGrid_SpansTwoColumnsOnlyWhereTheScriptSawTwoOrMore()
    {
        var css = ShippedLookTests.Css;
        var alone = ShippedLookTests.Body(".omni-tile-grid--fluid > .omni-tile--double");
        var several = ShippedLookTests.Body(".omni-tile-grid--fluid.omni-tile-grid--multi-column > .omni-tile--double");

        // Safe by default: a grid the script has not marked (not yet, or never interactive) opens no implicit column.
        Assert.Equal("auto", ShippedLookTests.Value(alone, "grid-column"));
        Assert.Equal("span 2", ShippedLookTests.Value(several, "grid-column"));
        // The plain rule overrides the 40rem one that spans two columns; the marked rule outweighs both.
        Assert.True(
            css.IndexOf(".omni-tile-grid--fluid > .omni-tile--double {", StringComparison.Ordinal) > css.IndexOf("@media (min-width: 40rem) {\n    .omni-tile-grid", StringComparison.Ordinal),
            "The fluid double tile rule must follow the media rules of the tile grid.");
    }

    [Fact]
    public void TheNonFluidTileRule_IsUnchanged()
    {
        Assert.Equal("minmax(0, 1fr)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-tile-grid"), "grid-template-columns"));
        Assert.Equal("span 2", ShippedLookTests.Value(ShippedLookTests.Body(".omni-tile-grid > .omni-tile--double"), "grid-column"));
        Assert.Equal("auto", ShippedLookTests.Value(ShippedLookTests.Body(".omni-tile-grid--columns-1 > .omni-tile--double"), "grid-column"));
    }

    [Fact]
    public void TheInteropScript_WritesTheMinimumThroughTheCSSOM_AndMarksTheGrid()
    {
        var script = File.ReadAllText(Path.Combine(ShippedLookTests.RepositoryRoot(), "src", "OmniEurope.Blazor", "wwwroot", "omniInterop.js"));

        Assert.Contains("export function applyTileGrid(grid, minWidth, watchColumns)", script, StringComparison.Ordinal);
        Assert.Contains("style.setProperty('--omni-tile-min'", script, StringComparison.Ordinal);
        Assert.Contains("style.removeProperty('--omni-tile-min')", script, StringComparison.Ordinal);
        Assert.Contains("'omni-tile-grid--multi-column'", script, StringComparison.Ordinal);
    }
}
