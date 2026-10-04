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
            builder.OpenComponent<OmniDataGridColumn<Row>>(0);
            builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Name));
            builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Title), "Nom");
            builder.AddComponentParameter(3, nameof(OmniDataGridColumn<Row>.HighlightOnHover), false);
            builder.CloseComponent();
            builder.OpenComponent<OmniDataGridColumn<Row>>(4);
            builder.AddComponentParameter(5, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Amount));
            builder.AddComponentParameter(6, nameof(OmniDataGridColumn<Row>.Title), "Montant");
            builder.CloseComponent();
        };

        return Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(grid => grid.Items, Rows)
            .Add(grid => grid.Columns, columns)
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
