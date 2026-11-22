using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// HighlightColumnOnHover: the grid marks itself and the columns that opt out, and wires the script
/// that tints the hovered column (a class toggled in the browser, never a style).
/// </summary>
public sealed class DataGridColumnHoverTests : OmniBunitContext
{
    private sealed record Row(int Id, string Name, decimal Amount);

    private static readonly IReadOnlyList<Row> Rows = [new(1, "Un", 10m), new(2, "Deux", 20m)];

    private IRenderedComponent<OmniDataGrid<Row>> RenderGrid(bool highlight)
    {
        RenderFragment columns = builder =>
        {
            DataGridTestSetup.PropertyColumn<Row>(builder, 0, nameof(Row.Name), "Nom", (nameof(OmniDataGridColumn<Row>.HighlightOnHover), false));
            DataGridTestSetup.PropertyColumn<Row>(builder, 4, nameof(Row.Amount), "Montant");
        };

        return this.RenderDataGrid(Rows, columns, parameters => parameters
            .Add(grid => grid.HighlightColumnOnHover, highlight));
    }

    [Fact]
    public void On_TheGridAndTheOptedOutColumnCarryTheirClasses_AndTheScriptIsWiredOnce()
    {
        var grid = RenderGrid(highlight: true);

        Assert.Contains("omni-data-grid--column-hover", grid.Find(".omni-data-grid").ClassList);
        Assert.All(grid.FindAll("[data-omni-col='Name']:is(th, td)"), cell => Assert.Contains("omni-data-grid__column--no-hover", cell.ClassList));
        Assert.All(grid.FindAll("[data-omni-col='Amount']:is(th, td)"), cell => Assert.DoesNotContain("omni-data-grid__column--no-hover", cell.ClassList));
        Assert.Empty(grid.FindAll("[style]"));

        grid.Render();

        Assert.Single(JSInterop.Invocations, invocation => invocation.Identifier == "attachColumnHover");
    }

    [Fact]
    public async Task Disposal_DetachesTheHover()
    {
        var grid = RenderGrid(highlight: true);

        await grid.InvokeAsync(() => grid.Instance.DisposeAsync().AsTask());

        Assert.Single(JSInterop.Invocations, invocation => invocation.Identifier == "detachColumnHover");
    }

    [Fact]
    public void Off_NothingIsMarkedNorWired()
    {
        var grid = RenderGrid(highlight: false);

        Assert.DoesNotContain("omni-data-grid--column-hover", grid.Find(".omni-data-grid").ClassList);
        Assert.Empty(grid.FindAll(".omni-data-grid__column--no-hover"));
        Assert.DoesNotContain(JSInterop.Invocations, invocation => invocation.Identifier == "attachColumnHover");
    }
}
