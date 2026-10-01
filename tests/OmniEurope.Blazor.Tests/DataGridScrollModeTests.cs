using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The grid's single scroll setting (<see cref="OmniDataGrid{TItem}.ScrollMode"/>), its case rule for
/// filters (<see cref="OmniDataGrid{TItem}.CaseSensitiveFilters"/>) and its fixed row height, each
/// driven through the rendered grid.
/// </summary>
public sealed class DataGridScrollModeTests : OmniBunitContext
{
    private const string GridModule = Internal.OmniModules.Grid;

    [Fact]
    public void Paged_IsTheDefault_AndRendersOnePageUnderAPager()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, Enumerable.Range(0, 25).ToArray())
            .Add(component => component.PageSize, 10));

        Assert.Equal(OmniDataGridScrollMode.Paged, grid.Instance.ScrollMode);
        Assert.Equal(10, grid.FindAll("tbody tr[data-omni-row-index]").Count);
        Assert.NotEmpty(grid.FindAll(".omni-pager"));
    }

    [Fact]
    public void Virtual_RendersAWindowOfTheWholeCount_WithoutAPager()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, Enumerable.Range(0, 10_000).ToArray())
            .Add(component => component.PageSize, 10)
            .Add(component => component.ScrollMode, OmniDataGridScrollMode.Virtual)
            .Add(component => component.EstimatedRowHeight, 40d));

        Assert.InRange(grid.FindAll("tbody tr[data-omni-row-index]").Count, 1, 64);
        Assert.Equal("10000", grid.Find("table").GetAttribute("aria-rowcount"));
        Assert.Empty(grid.FindAll(".omni-pager"));
    }

    [Fact]
    public void All_RendersEveryRow_WithoutPagerOrWindow()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, Enumerable.Range(0, 25).ToArray())
            .Add(component => component.PageSize, 10)
            .Add(component => component.ScrollMode, OmniDataGridScrollMode.All));

        Assert.Equal(25, grid.FindAll("tbody tr[data-omni-row-index]").Count);
        Assert.Empty(grid.FindAll(".omni-pager"));
        Assert.Contains(">24</td>", grid.Markup, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 1)]
    public void CaseSensitiveFilters_DecidesWhetherCapitalsMatter(bool caseSensitive, int expected)
    {
        var grid = Render<OmniDataGrid<City>>(parameters => parameters
            .Add(component => component.Items, [new City(1, "Paris"), new City(2, "paris"), new City(3, "Lyon")])
            .Add(component => component.CaseSensitiveFilters, caseSensitive)
            .Add(component => component.Columns, (RenderFragment)(builder =>
            {
                builder.OpenComponent<OmniDataGridColumn<City>>(0);
                builder.AddComponentParameter(1, nameof(OmniDataGridColumn<City>.Key), "name");
                builder.AddComponentParameter(2, nameof(OmniDataGridColumn<City>.Title), "Ville");
                builder.AddComponentParameter(3, nameof(OmniDataGridColumn<City>.Value), (Func<City, object?>)(city => city.Name));
                builder.AddComponentParameter(4, nameof(OmniDataGridColumn<City>.Filterable), true);
                builder.CloseComponent();
            })));

        grid.Find(".omni-data-grid__filter").Input("Paris");

        Assert.Equal(expected, grid.FindAll("tbody tr[data-omni-row-index]").Count);
        Assert.Equal(StringComparison.CurrentCultureIgnoreCase, OmniDataGridFilterText.Comparison(false));
        Assert.Equal(StringComparison.CurrentCulture, OmniDataGridFilterText.Comparison(true));
    }

    [Fact]
    public void FixedRowHeight_UsesTheEstimateAsTheRowHeight_AndStopsMeasuringRows()
    {
        var module = JSInterop.SetupModule(GridModule);
        module.Mode = JSRuntimeMode.Loose;

        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, Enumerable.Range(0, 5_000).ToArray())
            .Add(component => component.ScrollMode, OmniDataGridScrollMode.Virtual)
            .Add(component => component.EstimatedRowHeight, 36d)
            .Add(component => component.FixedRowHeight, true));

        grid.WaitForAssertion(() =>
        {
            // The second argument of sync says whether rows are to be measured; the spacers follow it.
            var sync = module.Invocations["sync"][0];
            Assert.Equal(false, sync.Arguments[1]);
            Assert.Equal(4, sync.Arguments.Count);
            Assert.Equal(36d, module.Invocations["applyRowHeight"][0].Arguments[1]);
        });
        Assert.Contains("omni-data-grid--fixed-row-height", grid.Find(".omni-data-grid").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void WithoutFixedRowHeight_RowsAreMeasured_AndNoRowHeightIsImposed()
    {
        var module = JSInterop.SetupModule(GridModule);
        module.Mode = JSRuntimeMode.Loose;

        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, Enumerable.Range(0, 5_000).ToArray())
            .Add(component => component.ScrollMode, OmniDataGridScrollMode.Virtual)
            .Add(component => component.EstimatedRowHeight, 36d));

        grid.WaitForAssertion(() => Assert.Equal(true, module.Invocations["sync"][0].Arguments[1]));
        Assert.DoesNotContain("omni-data-grid--fixed-row-height", grid.Find(".omni-data-grid").ClassName, StringComparison.Ordinal);
        Assert.All(module.Invocations["applyRowHeight"], call => Assert.Null(call.Arguments[1]));
    }

    [Fact]
    public void Cards_and_tiles_count_border_and_padding_inside_their_size()
    {
        // A tile stretched to its column (height: 100%) overflowed by its border and padding and ate
        // the gap to the next block (recette R2-006).
        var tiles = ShippedLookTests.Body(".omni-card, .omni-settings-tile, .omni-stat-tile");

        Assert.Equal("border-box", ShippedLookTests.Value(tiles, "box-sizing"));
    }

    [Fact]
    public void A_fixed_row_is_exactly_the_declared_height_padding_and_rule_included()
    {
        // The scroll total is computed from the declared height: a cell that added its padding and its
        // rule on top of it made every row taller than the spacers count.
        var cell = ShippedLookTests.Body(".omni-data-grid--fixed-row-height tbody tr:not(.omni-data-grid__spacer),\n.omni-data-grid--fixed-row-height tbody tr:not(.omni-data-grid__spacer) td");

        Assert.Equal("var(--omni-row-height)", ShippedLookTests.Value(cell, "block-size"));
        Assert.Equal("border-box", ShippedLookTests.Value(cell, "box-sizing"));
    }

    [Fact]
    public void The_sticky_header_is_opaque_whatever_the_fill_of_the_frame()
    {
        // Under a theme whose grid frame is translucent, the rows scrolling under the header must not
        // show through its titles: the surface colour, which no theme makes translucent, sits behind them.
        var header = ShippedLookTests.Body(".omni-data-grid__table thead");

        Assert.Equal("sticky", ShippedLookTests.Value(header, "position"));
        Assert.Equal("var(--omni-color-surface)", ShippedLookTests.Value(header, "background"));
    }

    public sealed record City(int Id, string Name);
}
