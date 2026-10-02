using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Which columns get a filter: a column that says nothing follows its grid, a column that says true or
/// false decides for itself.
/// </summary>
public sealed class DataGridColumnFilterDefaultTests : OmniBunitContext
{
    private sealed record Row(int Number, string Name, string City);

    private static readonly Row[] Rows = [new(1, "Alpha", "Bruxelles"), new(12, "Beta", "Namur")];

    /// <summary>name: unset, reads a property. city: set by the test. actions: a template alone, unset.</summary>
    private IRenderedComponent<OmniDataGrid<Row>> RenderGrid(bool? gridFilterable, bool? cityFilterable) =>
        Render<OmniDataGrid<Row>>(parameters =>
        {
            // These tests read the inline filter row, which a grid now asks for (R-041).
            parameters.Add(grid => grid.Items, Rows);
            parameters.Add(grid => grid.ShowHeaderFilterMenu, false);
            if (gridFilterable is { } onGrid)
            {
                parameters.Add(grid => grid.Filterable, onGrid);
            }

            parameters.Add(grid => grid.Columns, (RenderFragment)(builder =>
            {
                builder.OpenComponent<OmniDataGridColumn<Row>>(0);
                builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Title), "Name");
                builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Name));
                builder.CloseComponent();
                builder.OpenComponent<OmniDataGridColumn<Row>>(10);
                builder.AddComponentParameter(11, nameof(OmniDataGridColumn<Row>.Title), "City");
                builder.AddComponentParameter(12, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.City));
                if (cityFilterable is { } onColumn)
                {
                    builder.AddComponentParameter(13, nameof(OmniDataGridColumn<Row>.Filterable), (bool?)onColumn);
                }

                builder.CloseComponent();
                builder.OpenComponent<OmniDataGridColumn<Row>>(20);
                builder.AddComponentParameter(21, nameof(OmniDataGridColumn<Row>.Key), "actions");
                builder.AddComponentParameter(22, nameof(OmniDataGridColumn<Row>.Title), "Actions");
                builder.AddComponentParameter(23, nameof(OmniDataGridColumn<Row>.Template), (RenderFragment<Row>)(row => cell => cell.AddContent(0, "open")));
                builder.CloseComponent();
            }));
        });

    private static string[] FilteredColumns(IRenderedComponent<OmniDataGrid<Row>> grid) => grid
        .FindAll("thead td[data-omni-col]")
        .Where(cell => cell.QuerySelector(".omni-data-grid__filter") is not null)
        .Select(cell => cell.GetAttribute("data-omni-col")!)
        .ToArray();

    [Fact]
    public void A_grid_that_says_nothing_filters_from_its_header_menu_without_the_inline_row()
    {
        // Astraia recette R-041: the header menu is the default for every site; the row is asked for.
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(grid => grid.Items, Rows)
            .Add(grid => grid.Columns, (RenderFragment)(builder =>
            {
                builder.OpenComponent<OmniDataGridColumn<Row>>(0);
                builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Name));
                builder.CloseComponent();
            })));

        Assert.True(grid.Instance.ShowHeaderFilterMenu);
        Assert.Empty(grid.FindAll(".omni-data-grid__filters"));
        Assert.Single(grid.FindAll("thead .omni-data-grid__filter-menu"));
    }

    [Fact]
    public void A_column_that_says_nothing_follows_its_grid_which_filters_by_default()
    {
        var grid = RenderGrid(gridFilterable: null, cityFilterable: null);

        // The template-only column has nothing to filter on: it never follows the grid.
        Assert.Equal([nameof(Row.Name), nameof(Row.City)], FilteredColumns(grid));
    }

    [Fact]
    public void A_column_opts_out_of_a_filtered_grid()
    {
        var grid = RenderGrid(gridFilterable: true, cityFilterable: false);

        Assert.Equal([nameof(Row.Name)], FilteredColumns(grid));
    }

    [Fact]
    public void A_grid_switched_off_leaves_no_filter_on_the_columns_that_say_nothing()
    {
        var grid = RenderGrid(gridFilterable: false, cityFilterable: null);

        Assert.Empty(FilteredColumns(grid));
    }

    [Fact]
    public void A_column_opts_in_while_its_grid_is_switched_off_and_its_filter_works()
    {
        var grid = RenderGrid(gridFilterable: false, cityFilterable: true);

        Assert.Equal([nameof(Row.City)], FilteredColumns(grid));
        grid.Find($"thead td[data-omni-col='{nameof(Row.City)}'] input.omni-data-grid__filter").Input("Nam");
        Assert.Single(grid.FindAll("tbody tr"));
    }

    [Fact]
    public void A_column_that_computes_its_value_follows_its_grid_like_one_that_reads_a_property()
    {
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(grid => grid.Items, Rows)
            .Add(grid => grid.ShowHeaderFilterMenu, false)
            .Add(grid => grid.Columns, (RenderFragment)(builder =>
            {
                builder.OpenComponent<OmniDataGridColumn<Row>>(0);
                builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Key), "label");
                builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Title), "Label");
                builder.AddComponentParameter(3, nameof(OmniDataGridColumn<Row>.Value), (Func<Row, object?>)(row => $"{row.Name} ({row.City})"));
                builder.CloseComponent();
            })));

        Assert.Equal(["label"], FilteredColumns(grid));
        grid.Find("thead td[data-omni-col='label'] input.omni-data-grid__filter").Input("Namur");
        Assert.Single(grid.FindAll("tbody tr"));
    }

    [Fact]
    public void A_column_opted_in_keeps_the_filter_mode_of_a_grid_switched_off()
    {
        // The grid's Filterable no longer gates the filter mode: the one column that asks for a filter
        // gets the advanced editor the grid declares, not a plain box.
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(grid => grid.Items, Rows)
            .Add(grid => grid.ShowHeaderFilterMenu, false)
            .Add(grid => grid.Filterable, false)
            .Add(grid => grid.FilterMode, OmniDataGridFilterMode.Advanced)
            .Add(grid => grid.Columns, (RenderFragment)(builder =>
            {
                builder.OpenComponent<OmniDataGridColumn<Row>>(0);
                builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Title), "Name");
                builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Name));
                builder.CloseComponent();
                builder.OpenComponent<OmniDataGridColumn<Row>>(10);
                builder.AddComponentParameter(11, nameof(OmniDataGridColumn<Row>.Title), "City");
                builder.AddComponentParameter(12, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.City));
                builder.AddComponentParameter(13, nameof(OmniDataGridColumn<Row>.Filterable), (bool?)true);
                builder.CloseComponent();
            })));

        Assert.Empty(grid.FindAll($"thead td[data-omni-col='{nameof(Row.Name)}'] .omni-data-grid__advanced"));
        Assert.Single(grid.FindAll($"thead td[data-omni-col='{nameof(Row.City)}'] .omni-data-grid__advanced"));
    }
}
