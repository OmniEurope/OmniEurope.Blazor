using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Column aggregates and group footers: a subtotal per group and a total in the grid footer, from the
/// column's Aggregate and formats, or from its GroupFooterTemplate.
/// </summary>
public sealed class DataGridAggregateTests : OmniBunitContext
{
    private sealed record Row(string Name, string Group, decimal Amount, double Ratio, DateOnly Day);

    private static readonly IReadOnlyList<Row> Rows =
    [
        new("a", "Scope 1", 10m, 0.5, new DateOnly(2026, 1, 3)),
        new("b", "Scope 2", 5m, 1.5, new DateOnly(2026, 1, 1)),
        new("c", "Scope 1", 2.5m, 2, new DateOnly(2026, 1, 9)),
        new("d", "Scope 2", 7m, 4, new DateOnly(2026, 1, 5))
    ];

    private readonly CultureInfo _previousCulture = CultureInfo.CurrentCulture;

    // The figures are written in the current culture: pinned so the expected texts hold on any machine.
    public DataGridAggregateTests() => CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

    protected override void Dispose(bool disposing)
    {
        CultureInfo.CurrentCulture = _previousCulture;
        base.Dispose(disposing);
    }

    private IRenderedComponent<OmniDataGrid<Row>> RenderGrid(
        OmniDataGridAggregate aggregate,
        bool grouped,
        RenderFragment<OmniDataGridGroupContext<Row>>? groupFooter = null,
        string property = nameof(Row.Amount))
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<OmniDataGridColumn<Row>>(0);
            builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Group));
            builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Title), "Groupe");
            if (groupFooter is not null)
            {
                builder.AddComponentParameter(3, nameof(OmniDataGridColumn<Row>.GroupFooterTemplate), groupFooter);
            }
            builder.CloseComponent();
            builder.OpenComponent<OmniDataGridColumn<Row>>(4);
            builder.AddComponentParameter(5, nameof(OmniDataGridColumn<Row>.Property), property);
            builder.AddComponentParameter(6, nameof(OmniDataGridColumn<Row>.Title), "Valeur");
            builder.AddComponentParameter(7, nameof(OmniDataGridColumn<Row>.Aggregate), aggregate);
            builder.AddComponentParameter(8, nameof(OmniDataGridColumn<Row>.AggregateFormat), "Total : {0}");
            builder.AddComponentParameter(9, nameof(OmniDataGridColumn<Row>.GroupAggregateFormat), "Sous total : {0}");
            builder.CloseComponent();
        };

        return Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(grid => grid.Items, Rows)
            .Add(grid => grid.Columns, columns)
            .Add(grid => grid.AllowGrouping, grouped)
            .Add(grid => grid.Groups, grouped ? [new OmniDataGridGroup(nameof(Row.Group))] : []));
    }

    private static string FooterOf(IRenderedComponent<OmniDataGrid<Row>> grid, string key) =>
        grid.Find($"tfoot td[data-omni-col='{key}']").TextContent.Trim();

    [Theory]
    [InlineData(OmniDataGridAggregate.Sum, nameof(Row.Amount), "Total : 24.5")]
    [InlineData(OmniDataGridAggregate.Average, nameof(Row.Amount), "Total : 6.125")]
    [InlineData(OmniDataGridAggregate.Min, nameof(Row.Amount), "Total : 2.5")]
    [InlineData(OmniDataGridAggregate.Max, nameof(Row.Amount), "Total : 10")]
    [InlineData(OmniDataGridAggregate.Count, nameof(Row.Amount), "Total : 4")]
    [InlineData(OmniDataGridAggregate.Sum, nameof(Row.Ratio), "Total : 8")]
    [InlineData(OmniDataGridAggregate.Max, nameof(Row.Day), "Total : 01/09/2026")]
    public void GridFooter_ShowsTheAggregateOfEveryRow(OmniDataGridAggregate aggregate, string property, string expected)
    {
        var grid = RenderGrid(aggregate, grouped: false, property: property);

        Assert.Equal(expected, FooterOf(grid, property));
        Assert.Empty(grid.FindAll("tr.omni-data-grid__group-footer"));
    }

    [Fact]
    public void GridFooter_FollowsTheFilters()
    {
        var grid = RenderGrid(OmniDataGridAggregate.Sum, grouped: false);

        grid.InvokeAsync(() => grid.Instance.SetFiltersAsync(new Dictionary<string, string?> { [nameof(Row.Group)] = "Scope 2" }));

        Assert.Equal("Total : 12", FooterOf(grid, nameof(Row.Amount)));
    }

    [Fact]
    public void Groups_CloseOnTheirSubtotal_AndTheGridFooterKeepsTheTotal()
    {
        var grid = RenderGrid(OmniDataGridAggregate.Sum, grouped: true);

        var footers = grid.FindAll("tbody tr.omni-data-grid__group-footer");
        Assert.Equal(2, footers.Count);
        Assert.Equal(["Sous total : 12.5", "Sous total : 12"], footers.Select(row => row.QuerySelector("td[data-omni-col='Amount']")!.TextContent.Trim()));
        Assert.All(footers, row => Assert.Equal("0", row.GetAttribute("data-omni-group-level")));
        Assert.Equal("Total : 24.5", FooterOf(grid, nameof(Row.Amount)));

        // Each footer follows the last row of its group.
        var body = grid.FindAll("tbody tr").Select(row => row.ClassList.Contains("omni-data-grid__group-footer") ? "F" : row.ClassList.Contains("omni-data-grid__group") ? "H" : "R");
        Assert.Equal("HRRFHRRF", string.Concat(body));
    }

    [Fact]
    public void GroupFooterTemplate_ReceivesTheGroupKeyLevelAndRows()
    {
        var seen = new List<OmniDataGridGroupContext<Row>>();
        RenderFragment<OmniDataGridGroupContext<Row>> template = group => builder =>
        {
            seen.Add(group);
            builder.AddContent(0, $"{group.Key} ({group.Items.Count})");
        };

        var grid = RenderGrid(OmniDataGridAggregate.None, grouped: true, groupFooter: template);

        var cells = grid.FindAll("tbody tr.omni-data-grid__group-footer td[data-omni-col='Group']").Select(cell => cell.TextContent.Trim());
        Assert.Equal(["Scope 1 (2)", "Scope 2 (2)"], cells);
        var first = seen.First(group => Equals(group.Key, "Scope 1"));
        Assert.Equal("Group", first.ColumnKey);
        Assert.Equal(0, first.Level);
        Assert.Equal(["a", "c"], first.Items.Select(row => row.Name));
    }

    [Fact]
    public void ClosedGroup_KeepsItsSubtotal()
    {
        var grid = RenderGrid(OmniDataGridAggregate.Sum, grouped: true);

        grid.FindAll("tbody tr.omni-data-grid__group button")[0].Click();

        var body = grid.FindAll("tbody tr").Select(row => row.ClassList.Contains("omni-data-grid__group-footer") ? "F" : row.ClassList.Contains("omni-data-grid__group") ? "H" : "R");
        Assert.Equal("HFHRRF", string.Concat(body));
    }

    [Fact]
    public void WithoutAggregateNorTemplate_NoFooterIsRendered()
    {
        var grid = RenderGrid(OmniDataGridAggregate.None, grouped: true);

        Assert.Empty(grid.FindAll("tr.omni-data-grid__group-footer"));
        Assert.Empty(grid.FindAll("tfoot"));
    }
}
