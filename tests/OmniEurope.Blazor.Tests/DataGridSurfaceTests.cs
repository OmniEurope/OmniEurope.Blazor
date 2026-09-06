using Bunit;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Covers the grid surface the consuming projects actually use, as inventoried in
/// <c>docs/component-contracts.md</c>.
/// </summary>
public sealed class DataGridSurfaceTests : OmniBunitContext
{
    [Fact]
    public void PropertyAccessor_ReadsNestedPathsAndToleratesNullLinks()
    {
        var accessor = GridPropertyAccessor.Create<Order>("Customer.Name");

        Assert.NotNull(accessor);
        Assert.Equal("Alice", accessor!(new Order(1, new Customer("Alice"), 12.5m)));
        Assert.Null(accessor(new Order(2, null, 0m)));
        Assert.Null(GridPropertyAccessor.Create<Order>("Nope.Nope"));
        Assert.Null(GridPropertyAccessor.Create<Order>(null));
    }

    [Fact]
    public void Column_DeclaredByPropertyAndFormatStringRendersTheFormattedValue()
    {
        var grid = Render<DataGridSurfaceTestHost>();

        var cells = grid.FindAll("tbody tr td");
        Assert.Contains(cells, cell => cell.TextContent.Contains("Alice", StringComparison.Ordinal));
        Assert.Contains(cells, cell => cell.TextContent.Contains("12,50", StringComparison.Ordinal));
    }

    [Fact]
    public void Column_WithoutAnExplicitKeyFallsBackToItsProperty()
    {
        var grid = Render<DataGridSurfaceTestHost>();

        Assert.Equal("customer", grid.FindAll("thead th[data-omni-col]")[0].GetAttribute("data-omni-col"));
        Assert.Equal("Total", grid.FindAll("thead th[data-omni-col]")[1].GetAttribute("data-omni-col"));
    }

    [Fact]
    public void Column_SortOrderAppliesTheInitialSort()
    {
        var grid = Render<DataGridSurfaceTestHost>();

        Assert.Equal("descending", grid.FindAll("thead th[data-omni-col]")[1].GetAttribute("aria-sort"));
        Assert.Contains("30,00", grid.FindAll("tbody tr")[0].TextContent, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(OmniDataGridFilterOperator.Equals, "Alice", true)]
    [InlineData(OmniDataGridFilterOperator.NotEquals, "Alice", false)]
    [InlineData(OmniDataGridFilterOperator.DoesNotContain, "zz", true)]
    [InlineData(OmniDataGridFilterOperator.StartsWith, "Al", true)]
    [InlineData(OmniDataGridFilterOperator.EndsWith, "ce", true)]
    [InlineData(OmniDataGridFilterOperator.IsNotEmpty, "", true)]
    [InlineData(OmniDataGridFilterOperator.IsEmpty, "", false)]
    public void FilterOperators_MatchTheExpectedValues(OmniDataGridFilterOperator candidate, string filter, bool expected) =>
        Assert.Equal(expected, GridProjection<string>.MatchesFilter("Alice", filter, candidate, StringComparison.CurrentCultureIgnoreCase));

    [Fact]
    public void FilterOperators_HonourCaseSensitivity()
    {
        Assert.True(GridProjection<string>.MatchesFilter("Alice", "alice", OmniDataGridFilterOperator.Equals, StringComparison.CurrentCultureIgnoreCase));
        Assert.False(GridProjection<string>.MatchesFilter("Alice", "alice", OmniDataGridFilterOperator.Equals, StringComparison.CurrentCulture));
    }

    [Fact]
    public void Grid_FiltersOnTheOperatorPickedInTheMenu()
    {
        var grid = Render<DataGridSurfaceTestHost>(parameters => parameters
            .Add(component => component.FilterMode, OmniDataGridFilterMode.SimpleWithMenu));

        grid.Find(".omni-data-grid__filter-operator").Change(nameof(OmniDataGridFilterOperator.Equals));
        grid.Find(".omni-data-grid__filter").Input("Bob");

        Assert.Single(grid.FindAll("tbody tr"));
        Assert.Contains("Bob", grid.Find("tbody tr").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Grid_AdvancedFilterWaitsForTheApplyAction()
    {
        var grid = Render<DataGridSurfaceTestHost>(parameters => parameters
            .Add(component => component.FilterMode, OmniDataGridFilterMode.Advanced));

        grid.Find(".omni-data-grid__filter").Input("Bob");
        Assert.Equal(3, grid.FindAll("tbody tr").Count);

        grid.Find(".omni-data-grid__filter-apply").Click();
        Assert.Single(grid.FindAll("tbody tr"));

        grid.Find(".omni-data-grid__filter-clear").Click();
        Assert.Equal(3, grid.FindAll("tbody tr").Count);
    }

    [Fact]
    public void Grid_GroupsByColumnKeyAndCollapsesAGroup()
    {
        var grid = Render<DataGridSurfaceTestHost>(parameters => parameters
            .Add(component => component.AllowGrouping, true)
            .Add(component => component.ShowGroupPanel, true));

        Assert.Empty(grid.FindAll(".omni-data-grid__group"));

        grid.Find(".omni-data-grid__group-toggle").Click();

        var headers = grid.FindAll(".omni-data-grid__group");
        // Alice appears twice and the rows are ordered by group value, so two groups open.
        Assert.Equal(2, headers.Count);
        Assert.Contains("Client", grid.Find(".omni-data-grid__group-chip").TextContent, StringComparison.Ordinal);

        headers[0].QuerySelector(".omni-data-grid__group-expand")!.Click();

        Assert.Single(grid.FindAll("tbody tr[data-omni-row-index]"));
    }

    [Fact]
    public void Grid_RowRenderCanStyleARowAndVetoItsSelection()
    {
        var grid = Render<DataGridSurfaceTestHost>(parameters => parameters
            .Add(component => component.SelectionMode, OmniDataGridSelectionMode.Multiple)
            .Add(component => component.RowRender, args =>
            {
                args.CssClass = "flagged";
                args.Selectable = args.Index != 0;
            }));

        Assert.All(grid.FindAll("tbody tr[data-omni-row-index]"), row =>
            Assert.Contains("flagged", row.GetAttribute("class") ?? string.Empty, StringComparison.Ordinal));
        Assert.True(grid.FindAll("tbody input[type=checkbox]")[0].HasAttribute("disabled"));
    }

    [Fact]
    public void Grid_ShowsThePagingSummaryAndTheRequestedPageSizes()
    {
        var grid = Render<DataGridSurfaceTestHost>(parameters => parameters
            .Add(component => component.PageSize, 2)
            .Add(component => component.ShowPagingSummary, true)
            .Add(component => component.PageSizeOptions, new[] { 2, 5 }));

        Assert.Contains("1", grid.Find(".omni-data-grid__summary").TextContent, StringComparison.Ordinal);
        Assert.Equal(2, grid.FindAll(".omni-pager__page-size option").Count);

        grid.Find(".omni-pager__page-size").Change("5");

        Assert.Equal(5, grid.Instance.PageSize);
    }

    [Fact]
    public void Grid_HonoursACustomPagingSummaryFormat()
    {
        var grid = Render<DataGridSurfaceTestHost>(parameters => parameters
            .Add(component => component.PageSize, 2)
            .Add(component => component.ShowPagingSummary, true)
            .Add(component => component.PagingSummaryFormat, "{0}-{1}/{2}"));

        Assert.Equal("1-2/3", grid.Find(".omni-data-grid__summary").TextContent);
    }

    [Fact]
    public void Grid_AllowFlagsSuppressSortingFilteringAndPaging()
    {
        var grid = Render<DataGridSurfaceTestHost>(parameters => parameters
            .Add(component => component.PageSize, 1)
            .Add(component => component.AllowSorting, false)
            .Add(component => component.AllowFiltering, false)
            .Add(component => component.AllowPaging, false));

        Assert.Empty(grid.FindAll(".omni-data-grid__sort"));
        Assert.Empty(grid.FindAll(".omni-data-grid__filter"));
        Assert.Empty(grid.FindAll(".omni-pager"));
        Assert.Equal(3, grid.FindAll("tbody tr").Count);
    }

    [Fact]
    public void Grid_ProjectsGridLinesDensityAndResponsiveAsClassesOnly()
    {
        var grid = Render<DataGridSurfaceTestHost>(parameters => parameters
            .Add(component => component.GridLines, OmniDataGridLines.Both)
            .Add(component => component.Density, OmniDensity.Compact)
            .Add(component => component.Responsive, true));

        var root = grid.Find(".omni-data-grid");
        Assert.Contains("omni-data-grid--lines-both", root.GetAttribute("class") ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains("omni-data-grid--responsive", root.GetAttribute("class") ?? string.Empty, StringComparison.Ordinal);
        Assert.Equal("compact", root.GetAttribute("data-omni-density"));
        Assert.DoesNotContain("style=", grid.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Grid_TracksItsOwnEditStateInSingleEditMode()
    {
        var grid = Render<DataGridSurfaceTestHost>(parameters => parameters
            .Add(component => component.EditMode, OmniDataGridEditMode.Single));

        grid.FindAll(".omni-data-grid__actions button")[0].Click();
        Assert.Single(grid.FindAll(".surface-edit"));

        grid.FindAll("tbody tr")[1].QuerySelectorAll(".omni-data-grid__actions button")[0].Click();
        Assert.Single(grid.FindAll(".surface-edit"));
    }

    [Fact]
    public void Grid_CountOverridesTheLocalTotalForPaging()
    {
        var grid = Render<DataGridSurfaceTestHost>(parameters => parameters
            .Add(component => component.PageSize, 2)
            .Add(component => component.Count, 40)
            .Add(component => component.ShowPagingSummary, true));

        Assert.Contains("40", grid.Find(".omni-data-grid__summary").TextContent, StringComparison.Ordinal);
        Assert.NotEmpty(grid.FindAll(".omni-pager"));
    }

    public sealed record Customer(string Name);

    public sealed record Order(int Id, Customer? Customer, decimal Total);
}
