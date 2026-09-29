using Bunit;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A grid with a <see cref="OmniDataGrid{TItem}.LoadingContent"/> stays hidden until
/// <c>omni-grid.js</c> says its visible images are ready. That wait is bounded: when it times out or
/// fails, the grid is shown as it is rather than left hidden. The script's own image timeout runs in
/// a browser; here the .NET side receives what a timeout or a failure hands it.
/// </summary>
public sealed class DataGridPreparationTests : OmniBunitContext
{
    private const string ModulePath = Internal.OmniModules.Grid;

    [Fact]
    public void ReadinessWait_TimedOut_ShowsTheGrid()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("waitForReady", _ => true).SetCanceled();

        var grid = RenderPreparedGrid();

        grid.WaitForAssertion(() => Assert.Empty(grid.FindAll(".omni-data-grid--preparing")));
    }

    [Fact]
    public void ReadinessWait_Failed_ShowsTheGrid()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("waitForReady", _ => true).SetException(new JSException("image probe failed"));

        var grid = RenderPreparedGrid();

        grid.WaitForAssertion(() => Assert.Empty(grid.FindAll(".omni-data-grid--preparing")));
    }

    [Fact]
    public void ReadinessWait_IsGivenACancellationToken()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var wait = module.Setup<bool>("waitForReady", _ => true);

        RenderPreparedGrid();

        var invocation = Assert.Single(wait.Invocations);
        Assert.True(invocation.CancellationToken is { CanBeCanceled: true });
    }

    [Fact]
    public void WhileTheImagesAreAwaited_TheHeadersStayAndTheTemplateTakesTheFirstRow()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("waitForReady", _ => true);

        var grid = RenderPreparedGrid();

        Assert.NotEmpty(grid.FindAll(".omni-data-grid--preparing"));
        Assert.NotEmpty(grid.FindAll("thead th"));
        Assert.Equal("Chargement", grid.Find("tbody > tr.omni-data-grid__preparation").TextContent);
    }

    private IRenderedComponent<OmniDataGrid<int>> RenderPreparedGrid() =>
        Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, [1, 2])
            .Add(component => component.LoadingContent, builder => builder.AddContent(0, "Chargement")));
}
