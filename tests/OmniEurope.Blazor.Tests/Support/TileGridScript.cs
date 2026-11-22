using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The calls a tile grid makes to the interop script that sizes it and watches its double tiles, as the tile
/// grid suites read them, and the fluid grid and double navigation tile several of them start from.
/// </summary>
internal static class TileGridScript
{
    /// <summary>The function of the interop script that sizes a tile grid and watches its double tiles.</summary>
    public const string Function = "applyTileGrid";

    /// <summary>A fluid grid, whose columns are at least 18rem wide, over one plain item.</summary>
    public static IRenderedComponent<OmniTileGrid> RenderFluidGrid(this BunitContext context) =>
        context.Render<OmniTileGrid>(parameters => parameters
            .Add(component => component.MinColumnWidth, "18rem")
            .AddChildContent("<div>a</div>"));

    /// <summary>A double-width navigation tile to /a, titled A, drawn on its own.</summary>
    public static IRenderedComponent<OmniNavTile> RenderDoubleNavTile(this BunitContext context) =>
        context.Render<OmniNavTile>(parameters => parameters
            .Add(component => component.Href, "/a")
            .Add(component => component.Title, "A")
            .Add(component => component.DoubleWidth, true));

    /// <summary>Waits until the grid has called the script <paramref name="count"/> times, and answers the arguments of that last call.</summary>
    public static IReadOnlyList<object?> WaitForCalls(this IRenderedComponent<OmniTileGrid> grid, BunitJSModuleInterop module, int count)
    {
        grid.WaitForAssertion(() => Assert.Equal(count, module.Invocations[Function].Count));
        return module.Invocations[Function][count - 1].Arguments;
    }
}
