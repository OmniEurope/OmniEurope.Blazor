using Bunit;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Covers the header filter menu, its per-column editors, and the multi-valued operators: each test drives the rendered control and reads the rows it produces.
/// </summary>
public sealed class DataGridFilterSurfaceTests : OmniBunitContext
{
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

    [Fact]
    public void MultiSelectFilter_IsSearchableOnlyWhenTheColumnAsksForIt()
    {
        var plain = Render<DataGridFilterMenuTestHost>();
        Assert.Empty(plain.FindAll("th[data-omni-col=\"name\"] .omni-multi-select__search"));

        var searchable = Render<DataGridFilterMenuTestHost>(parameters => parameters
            .Add(component => component.FilterSearchable, true));

        var search = searchable.Find("th[data-omni-col=\"name\"] .omni-multi-select__search");
        Assert.Equal(3, searchable.FindAll("th[data-omni-col=\"name\"] .omni-multi-select__option").Count);

        search.Input("na");

        var narrowed = searchable.FindAll("th[data-omni-col=\"name\"] .omni-multi-select__option");
        Assert.Equal(["Namur"], narrowed.Select(option => option.TextContent.Trim()));
        // The searched fragment is marked up as text, never as markup supplied by the row.
        Assert.Equal("Na", searchable.Find("th[data-omni-col=\"name\"] .omni-combo__match").TextContent);
    }

    [Fact]
    public void DeclaredFilterValues_KeepTheOrderTheHostGave()
    {
        // A host lists values in their own sense (severity, workflow): never sorted alphabetically.
        var host = Render<DataGridFilterMenuTestHost>(parameters => parameters
            .Add(component => component.NameValues, ["Namur", "Liège", "Anvers", "namur"]));

        var options = host.FindAll("th[data-omni-col=\"name\"] .omni-multi-select__option");

        Assert.Equal(["Namur", "Liège", "Anvers"], options.Select(option => option.TextContent.Trim()));
    }

    [Fact]
    public void FilterTemplate_ReplacesTheEditorAndAppliesTheValueItPublishes()
    {
        var grid = Render<DataGridFilterMenuTestHost>();

        var custom = grid.Find("th[data-omni-col=\"id\"] .custom-filter");
        // The context carries the identifier, the placeholder and the column's candidate values.
        Assert.False(string.IsNullOrEmpty(custom.GetAttribute("id")));
        Assert.False(string.IsNullOrEmpty(custom.GetAttribute("placeholder")));
        Assert.Equal("3", custom.GetAttribute("data-candidates"));
        Assert.Empty(grid.FindAll("th[data-omni-col=\"id\"] .omni-data-grid__filter"));

        custom.Change("2");

        var row = Assert.Single(grid.FindAll("tbody tr"));
        Assert.Contains("Namur", row.TextContent, StringComparison.Ordinal);
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
            false,
            ignoreDiacritics,
            page: 1,
            pageSize: 10).Items.Select(region => region.Name).ToArray();

    private sealed record Region(string Name);
}
