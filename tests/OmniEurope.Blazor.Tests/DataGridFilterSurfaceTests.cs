using Bunit;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Covers the header filter menu, its per-column editors, the multi-valued operators and the
/// grid-local theme toggle: each test drives the rendered control and reads the rows it produces.
/// </summary>
public sealed class DataGridFilterSurfaceTests : OmniBunitContext
{
    [Fact]
    public void ThemeToggle_SwitchesOnlyTheGridItSitsIn()
    {
        var grid = Render<DataGridFilterMenuTestHost>();

        Assert.Equal("light", grid.Find("#regions").GetAttribute("data-omni-grid-theme"));

        grid.Find(".omni-data-grid__theme-toggle").Click();

        Assert.Equal("dark", grid.Find("#regions").GetAttribute("data-omni-grid-theme"));
        // The palette is carried by the grid element itself, so nothing outside it is repainted.
        Assert.Single(grid.FindAll("[data-omni-grid-theme]"));
        Assert.Equal("true", grid.Find(".omni-data-grid__theme-toggle").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void MultiSelectFilter_TicksAValueAndKeepsOnlyTheMatchingRows()
    {
        var grid = Render<DataGridFilterMenuTestHost>();
        Assert.Equal(3, grid.FindAll("tbody tr").Count);

        // Suggestions are the column's distinct values, ordered: Anvers, Liege, Namur.
        var options = grid.FindAll("th[data-omni-col=\"name\"] .omni-multi-select__checkbox");
        Assert.Equal(3, options.Count);
        options[2].Change(true);

        var row = Assert.Single(grid.FindAll("tbody tr"));
        Assert.Contains("Namur", row.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void ComboFilter_ShowsMatchingSuggestionsAndAppliesThePickedOne()
    {
        var grid = Render<DataGridFilterMenuTestHost>();

        var input = grid.Find("th[data-omni-col=\"code\"] .omni-combo__input");
        input.Focus();
        input.Input("n");

        var suggestions = grid.FindAll("th[data-omni-col=\"code\"] .omni-combo__option");
        Assert.Equal(["AN", "NA"], suggestions.Select(option => option.TextContent).Order(StringComparer.Ordinal));

        grid.FindAll("th[data-omni-col=\"code\"] .omni-combo__option")
            .Single(option => option.TextContent == "NA")
            .MouseDown();

        var row = Assert.Single(grid.FindAll("tbody tr"));
        Assert.Contains("Namur", row.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void GridProjection_AnswersInAndNotInAndFoldsDiacriticsOnlyWhenAsked()
    {
        var columns = new[]
        {
            new OmniDataGridColumnDefinition<Region>
            {
                Key = "name",
                Title = "Name",
                Value = region => region.Name,
                Filterable = true,
                FilterOperator = OmniDataGridFilterOperator.In
            }
        };
        Region[] rows = [new("Liège"), new("Namur"), new("Anvers")];
        var wanted = OmniDataGridFilterValues.Join(["Liege", "Namur"]);

        Assert.Equal(["Namur"], Project(rows, columns, OmniDataGridFilterOperator.In, wanted, false));
        // "Liege" only reaches "Liège" once both sides are folded.
        Assert.Equal(["Liège", "Namur"], Project(rows, columns, OmniDataGridFilterOperator.In, wanted, true));
        Assert.Equal(["Anvers"], Project(rows, columns, OmniDataGridFilterOperator.NotIn, wanted, true));
    }

    private static IReadOnlyList<string> Project(
        IReadOnlyList<Region> rows,
        IReadOnlyList<OmniDataGridColumnDefinition<Region>> columns,
        OmniDataGridFilterOperator filterOperator,
        string value,
        bool ignoreDiacritics) => GridProjection<Region>.Create(
            rows,
            columns,
            new Dictionary<string, GridColumnFilter>
            {
                ["name"] = GridColumnFilter.Empty with { Operator = filterOperator, Value = value }
            },
            [],
            OmniDataGridFilterCaseSensitivity.Default,
            ignoreDiacritics,
            page: 1,
            pageSize: 10).Items.Select(region => region.Name).ToArray();

    private sealed record Region(string Name);
}
