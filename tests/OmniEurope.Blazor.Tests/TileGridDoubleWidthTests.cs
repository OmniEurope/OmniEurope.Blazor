using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A <c>DoubleWidth</c> tile spans two columns only where its grid has two or more, which the stylesheet cannot
/// tell when the columns follow the grid's own width (<c>UniformSize</c> or <c>MinColumnWidth</c>): such a grid
/// has the interop script watch its columns while it holds a double tile. The tiles tell their grid through a
/// cascaded scope. A grid with neither option keeps the columns of the window and its CSS rule for the double
/// tile, and loads no script whatever it holds. The wiring is proved here; the script by
/// <c>tests/js/tile-grid.test.mjs</c>; the spans measured in a real browser (see the showcase).
/// </summary>
public sealed class TileGridDoubleWidthTests : OmniBunitContext
{
    private const string Function = "applyTileGrid";

    [Fact]
    public void AUniformTileGridWithoutADoubleTile_LoadsNoScript()
    {
        var grid = Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.UniformSize, true)
            .AddChildContent<OmniNavTile>(tile => tile.Add(component => component.Href, "/a").Add(component => component.Title, "A"))
            .AddChildContent<OmniCard>(card => card.AddChildContent("b")));

        Assert.Empty(grid.FindAll(".omni-tile--double"));
        Assert.Empty(JSInterop.Invocations);
    }

    [Theory]
    [InlineData("card")]
    [InlineData("selectable")]
    [InlineData("stat")]
    [InlineData("fact")]
    [InlineData("nav")]
    public void ADoubleTile_OfEachKind_HasAUniformGridWatchItsColumns(string kind)
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Interop);

        var grid = Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.UniformSize, true)
            .AddChildContent(DoubleTile(kind)));

        grid.WaitForAssertion(() => Assert.Single(module.Invocations[Function]));
        // No minimum of its own (the token applies), and the columns watched.
        Assert.Null(module.Invocations[Function][0].Arguments[1]);
        Assert.Equal(true, module.Invocations[Function][0].Arguments[2]);
        Assert.Single(grid.FindAll(".omni-tile--double"));
        Assert.DoesNotContain("style=", grid.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("card")]
    [InlineData("selectable")]
    [InlineData("stat")]
    [InlineData("fact")]
    [InlineData("nav")]
    public void ADoubleTile_OfEachKind_HasAMinColumnWidthGridWatchItsColumns(string kind)
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Interop);

        var grid = Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.MinColumnWidth, "18rem")
            .AddChildContent(DoubleTile(kind)));

        grid.WaitForAssertion(() => Assert.Single(module.Invocations[Function]));
        Assert.Equal("18rem", module.Invocations[Function][0].Arguments[1]);
        Assert.Equal(true, module.Invocations[Function][0].Arguments[2]);
    }

    [Theory]
    [InlineData("card")]
    [InlineData("selectable")]
    [InlineData("stat")]
    [InlineData("fact")]
    [InlineData("nav")]
    public void ADoubleTile_OfEachKind_InADefaultGrid_LoadsNoScript_AndKeepsItsDoubleClass(string kind)
    {
        var grid = Render<OmniTileGrid>(parameters => parameters.AddChildContent(DoubleTile(kind)));

        Assert.Single(grid.FindAll(".omni-tile--double"));
        Assert.Equal("omni-tile-grid omni-tile-grid--columns-4 omni-tile-grid--gap-md", grid.Find(".omni-tile-grid").ClassName);
        // A stat tile and a fact tile load scripts of their own (tooltips); the grid script is the one in question.
        Assert.DoesNotContain(JSInterop.Invocations, call => call.Arguments.Contains(Internal.OmniModules.Interop));
    }

    [Fact]
    public void ADoubleTile_InAUniformGrid_ImportsTheGridScript_ThePositiveControlOfTheTestAbove()
    {
        Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.UniformSize, true)
            .AddChildContent(DoubleTile("stat")));

        Assert.Contains(JSInterop.Invocations, call => call.Arguments.Contains(Internal.OmniModules.Interop));
    }

    [Fact]
    public void ADoubleTile_InADefaultGridOfNaturalHeights_LoadsNoScript()
    {
        var grid = Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.EqualHeights, false)
            .AddChildContent(NavTile(doubleWidth: true)));

        Assert.Single(grid.FindAll(".omni-tile--double"));
        Assert.Empty(JSInterop.Invocations);
    }

    [Fact]
    public void ADoubleTileAndAMinColumnWidth_ReachTheScriptTogether_InOneCall()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Interop);

        var grid = Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.MinColumnWidth, "14rem")
            .AddChildContent<OmniNavTile>(tile => tile.Add(component => component.Href, "/a").Add(component => component.Title, "A").Add(component => component.DoubleWidth, true)));

        grid.WaitForAssertion(() => Assert.Single(module.Invocations[Function]));
        Assert.Equal("14rem", module.Invocations[Function][0].Arguments[1]);
        Assert.Equal(true, module.Invocations[Function][0].Arguments[2]);
    }

    [Fact]
    public void AMinColumnWidthGridWithoutADoubleTile_WritesItsMinimum_AndWatchesNothing()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Interop);

        var grid = Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.MinColumnWidth, "14rem")
            .AddChildContent(NavTile(doubleWidth: false)));

        grid.WaitForAssertion(() => Assert.Single(module.Invocations[Function]));
        Assert.Equal("14rem", module.Invocations[Function][0].Arguments[1]);
        Assert.Equal(false, module.Invocations[Function][0].Arguments[2]);
    }

    [Fact]
    public void AUniformGrid_WatchesItsColumnsOnlyWhileADoubleTileIsInIt()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Interop);
        var grid = Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.UniformSize, true)
            .AddChildContent(NavTile(doubleWidth: false)));
        Assert.Empty(JSInterop.Invocations);

        // A tile that turns double: the grid watches.
        grid.Render(parameters => parameters.AddChildContent(NavTile(doubleWidth: true)));
        grid.WaitForAssertion(() => Assert.Single(module.Invocations[Function]));
        Assert.Equal(true, module.Invocations[Function][0].Arguments[2]);

        // The same tiles again change nothing for the script.
        grid.Render(parameters => parameters.AddChildContent(NavTile(doubleWidth: true)));
        Assert.Single(module.Invocations[Function]);

        // The last double tile leaves (turns single): the grid stops the watch, once.
        grid.Render(parameters => parameters.AddChildContent(NavTile(doubleWidth: false)));
        grid.WaitForAssertion(() => Assert.Equal(2, module.Invocations[Function].Count));
        Assert.Equal(false, module.Invocations[Function][1].Arguments[2]);
        grid.Render(parameters => parameters.AddChildContent(NavTile(doubleWidth: false)));
        Assert.Equal(2, module.Invocations[Function].Count);

        // A double tile that is removed from the grid stops the watch too.
        grid.Render(parameters => parameters.AddChildContent(NavTile(doubleWidth: true)));
        grid.WaitForAssertion(() => Assert.Equal(3, module.Invocations[Function].Count));
        grid.Render(parameters => parameters.AddChildContent(string.Empty));
        grid.WaitForAssertion(() => Assert.Equal(4, module.Invocations[Function].Count));
        Assert.Equal(false, module.Invocations[Function][3].Arguments[2]);
    }

    [Fact]
    public void ATwoDoubleTileGrid_StopsWatchingOnlyWhenTheLastOneLeaves()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Interop);
        var grid = Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.UniformSize, true)
            .AddChildContent(NavTile(doubleWidth: true, count: 2)));
        grid.WaitForAssertion(() => Assert.Single(module.Invocations[Function]));

        grid.Render(parameters => parameters.AddChildContent(NavTile(doubleWidth: true, count: 1)));
        Assert.Single(module.Invocations[Function]);

        grid.Render(parameters => parameters.AddChildContent(NavTile(doubleWidth: true, count: 0)));
        grid.WaitForAssertion(() => Assert.Equal(2, module.Invocations[Function].Count));
        Assert.Equal(false, module.Invocations[Function][1].Arguments[2]);
    }

    [Fact]
    public void AGridThatTurnsUniform_WhileHoldingADoubleTile_StartsWatching_AndStopsWhenItTurnsBack()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Interop);
        var grid = Render<OmniTileGrid>(parameters => parameters.AddChildContent(NavTile(doubleWidth: true)));
        Assert.Empty(JSInterop.Invocations);

        grid.Render(parameters => parameters.Add(component => component.UniformSize, true));
        grid.WaitForAssertion(() => Assert.Single(module.Invocations[Function]));
        Assert.Equal(true, module.Invocations[Function][0].Arguments[2]);

        grid.Render(parameters => parameters.Add(component => component.UniformSize, false));
        grid.WaitForAssertion(() => Assert.Equal(2, module.Invocations[Function].Count));
        Assert.Null(module.Invocations[Function][1].Arguments[1]);
        Assert.Equal(false, module.Invocations[Function][1].Arguments[2]);
    }

    [Fact]
    public async Task ATileThatTurnsDoubleAlone_InAComponentOfItsOwn_AsksTheUniformGridToDrawAgain()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Interop);
        var grid = Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.UniformSize, true)
            .AddChildContent<ToggledNavTile>());
        Assert.Empty(JSInterop.Invocations);
        var toggled = grid.FindComponent<ToggledNavTile>().Instance;

        // Only the wrapper redraws: the grid is not re-rendered by anyone, the tile tells it.
        await grid.InvokeAsync(() => toggled.Set(true));
        grid.WaitForAssertion(() => Assert.Single(module.Invocations[Function]));
        Assert.Equal(true, module.Invocations[Function][0].Arguments[2]);

        await grid.InvokeAsync(() => toggled.Set(false));
        grid.WaitForAssertion(() => Assert.Equal(2, module.Invocations[Function].Count));
        Assert.Equal(false, module.Invocations[Function][1].Arguments[2]);
    }

    [Fact]
    public async Task ATileThatTurnsDoubleAlone_InADefaultGrid_LoadsNoScript()
    {
        var grid = Render<OmniTileGrid>(parameters => parameters.AddChildContent<ToggledNavTile>());
        var toggled = grid.FindComponent<ToggledNavTile>().Instance;

        await grid.InvokeAsync(() => toggled.Set(true));
        await grid.InvokeAsync(() => toggled.Set(false));
        await grid.InvokeAsync(() => toggled.Set(true));

        // The default grid has nothing to do with a double tile (its stylesheet rule spans it): no script.
        Assert.Empty(JSInterop.Invocations);
        Assert.Single(grid.FindAll(".omni-tile--double"));
    }

    [Fact]
    public async Task AUniformGridDisposed_ThenItsDoubleTiles_AskNothingMoreOfTheScript()
    {
        var runtime = new ManualJSRuntime();
        Services.AddSingleton<IJSRuntime>(runtime);
        var grid = Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.UniformSize, true)
            .AddChildContent(NavTile(doubleWidth: true)));
        grid.WaitForAssertion(() => Assert.Equal([Function], runtime.Module.Calls));

        await grid.Instance.DisposeAsync();
        var calls = runtime.Module.Calls.Count;
        grid.Render(parameters => parameters.AddChildContent(string.Empty));

        // The release itself stopped the watch (one call); the tile leaving afterwards adds none.
        Assert.Equal(calls, runtime.Module.Calls.Count);
        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
    }

    [Fact]
    public void ADoubleTileOutsideAnyGrid_IsHarmless_AndLoadsNoScript()
    {
        var tile = Render<OmniNavTile>(parameters => parameters
            .Add(component => component.Href, "/a")
            .Add(component => component.Title, "A")
            .Add(component => component.DoubleWidth, true));

        Assert.Contains("omni-tile--double", tile.Find(".omni-nav-tile").ClassList);
        Assert.Empty(JSInterop.Invocations);
    }

    [Fact]
    public void ACardGroupInColumns_WithADoubleCard_StaysAsItWas_AndLoadsNoScript()
    {
        var group = RenderGroup(columns: 3, doubleWidth: true);

        var element = group.Find(".omni-selectable-card-group");
        Assert.Contains("omni-tile-grid", element.ClassList);
        Assert.Contains("omni-tile-grid--columns-3", element.ClassList);
        Assert.DoesNotContain("omni-tile-grid--uniform", element.ClassName, StringComparison.Ordinal);
        Assert.Contains("omni-tile--double", group.Find(".omni-selectable-card").ClassList);
        Assert.Empty(JSInterop.Invocations);
    }

    private readonly string? _bound = null;

    private IRenderedComponent<OmniSelectableCardGroup<string, string?>> RenderGroup(int? columns, bool doubleWidth) =>
        Render<OmniSelectableCardGroup<string, string?>>(parameters => parameters
            .Add(component => component.Columns, columns)
            .Add(component => component.Value, "a")
            .Add(component => component.ValueExpression, () => _bound)
            .AddChildContent<OmniSelectableCard>(card => card
                .Add(component => component.Title, "A")
                .Add(component => component.Choice, "a")
                .Add(component => component.DoubleWidth, doubleWidth)));

    // One tile of each kind, all DoubleWidth, as the direct child of a tile grid.
    private static RenderFragment DoubleTile(string kind) => builder =>
    {
        switch (kind)
        {
            case "card":
                builder.OpenComponent<OmniCard>(0);
                builder.AddComponentParameter(1, nameof(OmniCard.DoubleWidth), true);
                builder.AddComponentParameter(2, nameof(OmniCard.ChildContent), (RenderFragment)(content => content.AddContent(0, "x")));
                break;
            case "selectable":
                builder.OpenComponent<OmniSelectableCard>(0);
                builder.AddComponentParameter(1, nameof(OmniSelectableCard.DoubleWidth), true);
                builder.AddComponentParameter(2, nameof(OmniSelectableCard.Title), "Linux");
                break;
            case "stat":
                builder.OpenComponent<OmniStatTile>(0);
                builder.AddComponentParameter(1, nameof(OmniStatTile.DoubleWidth), true);
                builder.AddComponentParameter(2, nameof(OmniStatTile.Title), "Jetons");
                break;
            case "fact":
                builder.OpenComponent<OmniFactTile>(0);
                builder.AddComponentParameter(1, nameof(OmniFactTile.DoubleWidth), true);
                builder.AddComponentParameter(2, nameof(OmniFactTile.Title), "Source");
                break;
            default:
                builder.OpenComponent<OmniNavTile>(0);
                builder.AddComponentParameter(1, nameof(OmniNavTile.DoubleWidth), true);
                builder.AddComponentParameter(2, nameof(OmniNavTile.Href), "/a");
                builder.AddComponentParameter(3, nameof(OmniNavTile.Title), "A");
                break;
        }

        builder.CloseComponent();
    };

    private static RenderFragment NavTile(bool doubleWidth, int count = 1) => builder =>
    {
        for (var index = 0; index < count; index++)
        {
            builder.OpenComponent<OmniNavTile>(0);
            builder.SetKey(index);
            builder.AddComponentParameter(1, nameof(OmniNavTile.DoubleWidth), doubleWidth);
            builder.AddComponentParameter(2, nameof(OmniNavTile.Href), "/a");
            builder.AddComponentParameter(3, nameof(OmniNavTile.Title), "A");
            builder.CloseComponent();
        }
    };

    /// <summary>A component of its own that holds one nav tile and redraws without its parent grid.</summary>
    private sealed class ToggledNavTile : ComponentBase
    {
        private bool _double;

        internal void Set(bool value)
        {
            _double = value;
            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<OmniNavTile>(0);
            builder.AddComponentParameter(1, nameof(OmniNavTile.DoubleWidth), _double);
            builder.AddComponentParameter(2, nameof(OmniNavTile.Href), "/a");
            builder.AddComponentParameter(3, nameof(OmniNavTile.Title), "A");
            builder.CloseComponent();
        }
    }
}
