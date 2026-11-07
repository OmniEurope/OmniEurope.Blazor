using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <c>FitTitle</c> tiles of an <see cref="OmniTileGrid"/> share one title size: the grid takes the longest of their
/// titles and carries it as a class, the tiles inherit it and compute the size from their own width in the
/// stylesheet. A tile outside a grid keeps the step of its own title. The wiring is proved here (render and
/// re-render); the size in pixels is measured in a real browser (see the showcase page Navigation tiles).
/// </summary>
public sealed class TileGridFitTitleTests : OmniBunitContext
{
    private static RenderFragment Tiles(params (string Title, bool Fit)[] tiles) => builder =>
    {
        foreach (var (title, fit) in tiles)
        {
            builder.OpenComponent<OmniNavTile>(0);
            builder.SetKey(title);
            builder.AddComponentParameter(1, nameof(OmniNavTile.Href), "/a");
            builder.AddComponentParameter(2, nameof(OmniNavTile.Title), title);
            builder.AddComponentParameter(3, nameof(OmniNavTile.FitTitle), fit);
            builder.CloseComponent();
        }
    };

    private IRenderedComponent<OmniTileGrid> Grid(params (string Title, bool Fit)[] tiles) =>
        Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.Columns, 6)
            .Add(component => component.ChildContent, Tiles(tiles)));

    private static void Show(IRenderedComponent<OmniTileGrid> grid, params (string Title, bool Fit)[] tiles) =>
        grid.Render(parameters => parameters.Add(component => component.ChildContent, Tiles(tiles)));

    // What one re-render of a grid costs in this harness (bUnit renders a component twice when its parameters are
    // set again): a grid with no fitted tile, which never asks for a render of its own.
    private int ControlRenderDelta()
    {
        var control = Grid(("Qualité", false), ("Pipelines", false));
        var before = control.RenderCount;
        Show(control, ("Qualité", false), ("Pipelines", false));
        return control.RenderCount - before;
    }

    private static string[] GridFitClasses(IRenderedComponent<OmniTileGrid> grid) =>
        [.. grid.Find(".omni-tile-grid").ClassList.Where(name => name.StartsWith("omni-tile-grid--fit-", StringComparison.Ordinal))];

    private static bool HasTileFitClass(AngleSharp.Dom.IElement tile) =>
        tile.ClassList.Any(name => name.StartsWith("omni-nav-tile--fit", StringComparison.Ordinal));

    [Fact]
    public void InAGrid_EveryFitTitleTile_SharesTheSizeOfTheLongestTitle()
    {
        var grid = Grid(("Qualité", true), ("Pipelines", true), ("Environnements", true), ("Bibliothèques", true));

        Assert.Equal(["omni-tile-grid--fit-14"], GridFitClasses(grid));
        var tiles = grid.FindAll("a.omni-nav-tile");
        Assert.Equal(4, tiles.Count);
        foreach (var tile in tiles)
        {
            // No step of its own: the length comes from the grid, so a short title is never sized smaller.
            Assert.Contains("omni-nav-tile--fit", tile.ClassList);
            Assert.Contains("omni-nav-tile--fit-grid", tile.ClassList);
            Assert.DoesNotContain(tile.ClassList, name => name is "omni-nav-tile--fit-8" or "omni-nav-tile--fit-12" or "omni-nav-tile--fit-16");
        }

        Assert.DoesNotContain("style=", grid.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InAGrid_AFitTitleTile_WithANullTitle_CountsAsTheSmallestStep_AndLeavesTheLongerTitlesTheirs()
    {
        var grid = Grid((null!, true));
        Assert.Equal(["omni-tile-grid--fit-1"], GridFitClasses(grid));

        Show(grid, (null!, true), ("Pipelines", true));
        Assert.Equal(["omni-tile-grid--fit-9"], GridFitClasses(grid));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(14, 14)]
    [InlineData(32, 32)]
    [InlineData(33, 40)]
    [InlineData(40, 40)]
    [InlineData(41, 48)]
    [InlineData(200, 48)]
    public void TheGridClass_IsTheExactLength_UpTo32_ThenTheCoarseSteps(int length, int step)
    {
        var grid = Grid(("a", true), (new string('x', length), true));

        Assert.Equal([$"omni-tile-grid--fit-{step}"], GridFitClasses(grid));
        Assert.Equal(step, OmniTileGridScope.StepFor(length));
    }

    [Fact]
    public void AnEmptyTitle_ReadsAsTheSmallestStep()
    {
        var grid = Grid(("", true));

        Assert.Equal(["omni-tile-grid--fit-1"], GridFitClasses(grid));
    }

    [Fact]
    public void ChangingTheLongestTitle_UpdatesTheGridClass()
    {
        var grid = Grid(("Qualité", true), ("Pipelines", true));
        Assert.Equal(["omni-tile-grid--fit-9"], GridFitClasses(grid));

        Show(grid, ("Qualité", true), ("Environnements", true));
        Assert.Equal(["omni-tile-grid--fit-14"], GridFitClasses(grid));

        Show(grid, ("Qualité", true), ("Env.", true));
        Assert.Equal(["omni-tile-grid--fit-7"], GridFitClasses(grid));
    }

    [Fact]
    public void ARenderOfTheSameTitles_DoesNotRenderTheGridAgain_SoThereIsNoLoop()
    {
        var grid = Grid(("Qualité", true), ("Pipelines", true));
        var renders = grid.RenderCount;

        Show(grid, ("Qualité", true), ("Pipelines", true));

        Assert.Equal(["omni-tile-grid--fit-9"], GridFitClasses(grid));
        Assert.Equal(renders + ControlRenderDelta(), grid.RenderCount);
    }

    [Fact]
    public void ATitleChange_ThatKeepsTheLongestLength_DoesNotRenderTheGridAgain()
    {
        var grid = Grid(("Qualité", true), ("Pipelines", true));
        var renders = grid.RenderCount;

        Show(grid, ("Qualités", true), ("Pipelines", true));

        Assert.Equal(["omni-tile-grid--fit-9"], GridFitClasses(grid));
        Assert.Equal(renders + ControlRenderDelta(), grid.RenderCount);
    }

    [Fact]
    public void RemovingTheLongestTile_ShrinksTheSharedLength_AndRemovingTheLastOneClearsIt()
    {
        var grid = Grid(("Qualité", true), ("Environnements", true));
        Assert.Equal(["omni-tile-grid--fit-14"], GridFitClasses(grid));

        Show(grid, ("Qualité", true));
        Assert.Equal(["omni-tile-grid--fit-7"], GridFitClasses(grid));

        Show(grid);
        Assert.Empty(GridFitClasses(grid));
    }

    [Fact]
    public void AddingALongerTile_GrowsTheSharedLength()
    {
        var grid = Grid(("Qualité", true));

        Show(grid, ("Qualité", true), ("Environnements", true));

        Assert.Equal(["omni-tile-grid--fit-14"], GridFitClasses(grid));
    }

    [Fact]
    public void ATileThatStopsFittingItsTitle_LeavesTheCount_AndOneThatStartsJoinsIt()
    {
        var grid = Grid(("Qualité", true), ("Environnements", true));

        Show(grid, ("Qualité", true), ("Environnements", false));
        Assert.Equal(["omni-tile-grid--fit-7"], GridFitClasses(grid));

        Show(grid, ("Qualité", true), ("Environnements", true));
        Assert.Equal(["omni-tile-grid--fit-14"], GridFitClasses(grid));
    }

    [Fact]
    public void ATileWithoutFitTitle_InAGrid_IsUnaffected_AndAGridWithoutFitTilesCarriesNoFitClass()
    {
        var grid = Grid(("Qualité", false), ("Environnements", false));

        Assert.Empty(GridFitClasses(grid));
        Assert.DoesNotContain(grid.FindAll("a.omni-nav-tile"), HasTileFitClass);
    }

    [Fact]
    public void ANonFitTile_NextToFitTiles_DoesNotCountForTheSharedLength()
    {
        var grid = Grid(("Qualité", true), ("Une description très très longue", false));

        Assert.Equal(["omni-tile-grid--fit-7"], GridFitClasses(grid));
        Assert.False(HasTileFitClass(grid.FindAll("a.omni-nav-tile")[1]));
    }

    [Fact]
    public void ATileOutsideAGrid_KeepsTheStepOfItsOwnTitle()
    {
        var tile = Render<OmniNavTile>(parameters => parameters
            .Add(component => component.Href, "/a")
            .Add(component => component.Title, "Environnements")
            .Add(component => component.FitTitle, true));

        var classes = tile.Find("a").ClassList;
        Assert.Contains("omni-nav-tile--fit-16", classes);
        Assert.DoesNotContain("omni-nav-tile--fit-grid", classes);
    }

    [Fact]
    public void TheAlignments_KeepWorking_InAGridOfFittedTitles()
    {
        var grid = Render<OmniTileGrid>(parameters => parameters
            .AddChildContent<OmniNavTile>(tile => tile
                .Add(component => component.Href, "/a")
                .Add(component => component.Title, "Qualité")
                .Add(component => component.FitTitle, true)
                .Add(component => component.IconAlign, OmniAlignment.End)
                .Add(component => component.TitleAlign, OmniAlignment.Center)));

        var classes = grid.Find("a.omni-nav-tile").ClassList;
        Assert.Contains("omni-nav-tile--icon-end", classes);
        Assert.Contains("omni-nav-tile--title-center", classes);
        Assert.Contains("omni-nav-tile--fit-grid", classes);
        Assert.Equal(["omni-tile-grid--fit-7"], GridFitClasses(grid));
    }

    [Fact]
    public async Task DisposingTheGridAndItsTiles_DoesNotThrow()
    {
        var grid = Grid(("Qualité", true), ("Environnements", true));

        await DisposeComponentsAsync();

        Assert.NotNull(grid);
    }

    [Fact]
    public void TheStylesheet_HasAGridClassPerStep_AndTheTileReadsTheGridLength()
    {
        foreach (var step in Enumerable.Range(1, 32).Concat([40, 48]))
        {
            var rule = ShippedLookTests.Body($".omni-tile-grid--fit-{step}");
            Assert.Equal(step.ToString(CultureInfo.InvariantCulture), ShippedLookTests.Value(rule, "--omni-tile-grid-fit-chars"));
        }

        var tile = ShippedLookTests.Body(".omni-nav-tile--fit-grid");
        Assert.Contains("--omni-tile-grid-fit-chars", ShippedLookTests.Value(tile, "--omni-nav-tile-fit-chars"), StringComparison.Ordinal);
    }
}
