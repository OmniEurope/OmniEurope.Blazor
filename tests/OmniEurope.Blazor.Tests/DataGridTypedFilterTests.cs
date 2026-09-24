using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>Typed column filters: a number read through a function, and a narrowed operator menu.</summary>
public sealed class DataGridTypedFilterTests : OmniBunitContext
{
    private sealed record Row(int Number, string Name);

    private static readonly Row[] Rows = [new(1, "Alpha"), new(12, "Beta"), new(28, "Gamma")];

    private static readonly IReadOnlyList<OmniDataGridFilterOperator> TextOperators =
    [
        OmniDataGridFilterOperator.Contains, OmniDataGridFilterOperator.StartsWith,
        OmniDataGridFilterOperator.EndsWith, OmniDataGridFilterOperator.DoesNotContain,
        // Not a text operator: dropped instead of offered.
        OmniDataGridFilterOperator.GreaterThan
    ];

    private IRenderedComponent<OmniDataGrid<Row>> RenderGrid() => Render<OmniDataGrid<Row>>(parameters => parameters
        .Add(grid => grid.Items, Rows)
        .Add(grid => grid.AllowFiltering, true)
        .Add(grid => grid.FilterMode, OmniDataGridFilterMode.SimpleWithMenu)
        .Add(grid => grid.Columns, (RenderFragment)(builder =>
        {
            builder.OpenComponent<OmniDataGridColumn<Row>>(0);
            builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Key), "number");
            builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Title), "N°");
            builder.AddComponentParameter(3, nameof(OmniDataGridColumn<Row>.Value), (Func<Row, object?>)(row => row.Number));
            builder.AddComponentParameter(4, nameof(OmniDataGridColumn<Row>.Filterable), true);
            builder.AddComponentParameter(5, nameof(OmniDataGridColumn<Row>.FilterType), OmniDataGridColumnFilterType.Number);
            builder.CloseComponent();
            builder.OpenComponent<OmniDataGridColumn<Row>>(10);
            builder.AddComponentParameter(11, nameof(OmniDataGridColumn<Row>.Key), "name");
            builder.AddComponentParameter(12, nameof(OmniDataGridColumn<Row>.Title), "Name");
            builder.AddComponentParameter(13, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Name));
            builder.AddComponentParameter(14, nameof(OmniDataGridColumn<Row>.Filterable), true);
            builder.AddComponentParameter(15, nameof(OmniDataGridColumn<Row>.FilterOperators), TextOperators);
            builder.CloseComponent();
        })));

    [Fact]
    public void Number_filter_is_a_number_input_with_the_ordered_operators()
    {
        var grid = RenderGrid();
        var cell = grid.Find("thead td[data-omni-col='number']");
        var input = cell.QuerySelector("input.omni-data-grid__filter")!;
        Assert.Equal("number", input.GetAttribute("type"));
        var operators = cell.QuerySelectorAll(".omni-data-grid__filter-operator option").Select(option => option.GetAttribute("value")).ToArray();
        Assert.Contains(nameof(OmniDataGridFilterOperator.GreaterThan), operators);
        Assert.DoesNotContain(nameof(OmniDataGridFilterOperator.Contains), operators);

        cell.QuerySelector(".omni-data-grid__filter-operator")!.Change(nameof(OmniDataGridFilterOperator.GreaterThan));
        grid.Find("thead td[data-omni-col='number'] input.omni-data-grid__filter").Input("10");
        Assert.Equal(2, grid.FindAll("tbody tr").Count);
    }

    [Fact]
    public void Text_column_offers_only_its_chosen_operators_in_their_order()
    {
        var grid = RenderGrid();
        var operators = grid.FindAll("thead td[data-omni-col='name'] .omni-data-grid__filter-operator option")
            .Select(option => option.GetAttribute("value") ?? string.Empty).ToArray();
        Assert.Equal<string>(
            [nameof(OmniDataGridFilterOperator.Contains), nameof(OmniDataGridFilterOperator.StartsWith),
             nameof(OmniDataGridFilterOperator.EndsWith), nameof(OmniDataGridFilterOperator.DoesNotContain)],
            operators);
    }
}
