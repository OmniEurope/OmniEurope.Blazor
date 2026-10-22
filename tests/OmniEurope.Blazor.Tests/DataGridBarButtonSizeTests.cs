using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniDataGrid{TItem}.BarButtonSize"/>: unset, the buttons of the bars (export, column chooser)
/// stay compact; set, they are buttons of that size, like the other buttons of the page.
/// </summary>
public sealed class DataGridBarButtonSizeTests : OmniBunitContext
{
    public sealed record Row(string Name, int Amount);

    private static readonly IReadOnlyList<Row> Rows = [new("a", 1), new("b", 2)];

    private static readonly RenderFragment Columns = builder =>
    {
        builder.OpenComponent<OmniDataGridColumn<Row>>(0);
        builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Name));
        builder.CloseComponent();
        builder.OpenComponent<OmniDataGridColumn<Row>>(2);
        builder.AddComponentParameter(3, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Amount));
        builder.CloseComponent();
    };

    private IRenderedComponent<OmniDataGrid<Row>> RenderGrid(OmniControlSize? size) =>
        Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.ShowHeaderBar, true)
            .Add(component => component.AllowColumnHide, true)
            .Add(component => component.BarButtonSize, size)
            .Add(component => component.Columns, Columns));

    [Fact]
    public void Unset_KeepsTheBarButtonsCompact()
    {
        var grid = RenderGrid(null);

        var buttons = grid.FindAll(".omni-data-grid__export-button, .omni-data-grid__chooser-toggle");
        Assert.NotEmpty(grid.FindAll(".omni-data-grid__export-button"));
        Assert.Single(grid.FindAll(".omni-data-grid__chooser-toggle"));
        Assert.All(buttons, button =>
        {
            Assert.Contains("omni-data-grid__bar-button--compact", button.ClassList);
            Assert.Contains("omni-button--small", button.ClassList);
            // Without their text (ShowExportButtonText false), the export buttons and the column chooser
            // are icon-only squares: their icon is the button's Icon, not its content.
            Assert.Contains("omni-button--icon", button.ClassList);
        });
    }

    [Theory]
    [InlineData(OmniControlSize.Small, "omni-button--small")]
    [InlineData(OmniControlSize.Medium, "omni-button--medium")]
    [InlineData(OmniControlSize.Large, "omni-button--large")]
    public void Set_GivesTheBarButtonsThatSize_WithoutTheCompactReduction(OmniControlSize size, string sizeClass)
    {
        var grid = RenderGrid(size);

        var buttons = grid.FindAll(".omni-data-grid__export-button, .omni-data-grid__chooser-toggle");
        Assert.True(buttons.Count > 1);
        Assert.All(buttons, button =>
        {
            Assert.DoesNotContain("omni-data-grid__bar-button--compact", button.ClassList);
            Assert.Contains(sizeClass, button.ClassList);
        });
    }
}
