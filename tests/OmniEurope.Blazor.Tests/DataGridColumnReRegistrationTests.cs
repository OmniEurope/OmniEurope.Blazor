using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A column that keeps its key but changes one template, aggregate or format registers again, so the
/// grid shows the new content: one case per parameter the column compares for its templates and aggregates.
/// </summary>
public sealed class DataGridColumnReRegistrationTests : OmniBunitContext
{
    private sealed record Row(string Name, string Group, decimal Amount);

    /// <summary>One changed parameter, what the grid shows before and after, and the edit mode some cases need first.</summary>
    private sealed record Case(
        Dictionary<string, object?> Before,
        Dictionary<string, object?> After,
        string Selector,
        string ShownBefore,
        string ShownAfter,
        bool EditFirstRow = false);

    private static readonly IReadOnlyList<Row> Rows = [new("a", "g1", 1m), new("b", "g2", 2m), new("c", "g1", 3m)];

    private const string DataCell = "tbody tr:not(.omni-data-grid__group):not(.omni-data-grid__group-footer) td[data-omni-col='Amount']";

    private static readonly RenderFragment<Row> CellTemplate = row => builder => builder.AddContent(0, "T:" + row.Name);

    private static readonly RenderFragment<Row> Editor = row => builder => builder.AddContent(0, "E:" + row.Name);

    private static readonly RenderFragment Footer = builder => builder.AddContent(0, "F");

    private static readonly RenderFragment Header = builder => builder.AddContent(0, "H!");

    private static readonly RenderFragment<OmniDataGridGroupContext<Row>> GroupFooter = group => builder => builder.AddContent(0, $"G:{group.Key}");

    private static readonly Dictionary<string, Case> Cases = new()
    {
        [nameof(OmniDataGridColumn<Row>.Template)] = new(
            [],
            new() { [nameof(OmniDataGridColumn<Row>.Template)] = CellTemplate },
            DataCell, "1", "T:a"),
        [nameof(OmniDataGridColumn<Row>.EditTemplate)] = new(
            [],
            new() { [nameof(OmniDataGridColumn<Row>.EditTemplate)] = Editor },
            DataCell, "1", "E:a", EditFirstRow: true),
        [nameof(OmniDataGridColumn<Row>.FooterContent)] = new(
            [],
            new() { [nameof(OmniDataGridColumn<Row>.FooterContent)] = Footer },
            "tfoot td[data-omni-col='Amount']", "", "F"),
        [nameof(OmniDataGridColumn<Row>.GroupFooterTemplate)] = new(
            [],
            new() { [nameof(OmniDataGridColumn<Row>.GroupFooterTemplate)] = GroupFooter },
            "tbody tr.omni-data-grid__group-footer td[data-omni-col='Amount']", "", "G:g1"),
        [nameof(OmniDataGridColumn<Row>.HeaderContent)] = new(
            [],
            new() { [nameof(OmniDataGridColumn<Row>.HeaderContent)] = Header },
            "thead th[data-omni-col='Amount'] .omni-data-grid__header", "Valeur", "H!"),
        [nameof(OmniDataGridColumn<Row>.Aggregate)] = new(
            [],
            new() { [nameof(OmniDataGridColumn<Row>.Aggregate)] = OmniDataGridAggregate.Sum },
            "tfoot td[data-omni-col='Amount']", "", "6"),
        [nameof(OmniDataGridColumn<Row>.AggregateFormat)] = new(
            new() { [nameof(OmniDataGridColumn<Row>.Aggregate)] = OmniDataGridAggregate.Sum },
            new()
            {
                [nameof(OmniDataGridColumn<Row>.Aggregate)] = OmniDataGridAggregate.Sum,
                [nameof(OmniDataGridColumn<Row>.AggregateFormat)] = "Total : {0}"
            },
            "tfoot td[data-omni-col='Amount']", "6", "Total : 6"),
        [nameof(OmniDataGridColumn<Row>.GroupAggregateFormat)] = new(
            new() { [nameof(OmniDataGridColumn<Row>.Aggregate)] = OmniDataGridAggregate.Sum },
            new()
            {
                [nameof(OmniDataGridColumn<Row>.Aggregate)] = OmniDataGridAggregate.Sum,
                [nameof(OmniDataGridColumn<Row>.GroupAggregateFormat)] = "Sous total : {0}"
            },
            "tbody tr.omni-data-grid__group-footer td[data-omni-col='Amount']", "4", "Sous total : 4")
    };

    public static TheoryData<string> Changes => [.. Cases.Keys];

    private readonly CultureInfo _previousCulture = CultureInfo.CurrentCulture;

    // The figures are written in the current culture: pinned so the expected texts hold on any machine.
    public DataGridColumnReRegistrationTests() => CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

    protected override void Dispose(bool disposing)
    {
        CultureInfo.CurrentCulture = _previousCulture;
        base.Dispose(disposing);
    }

    private static RenderFragment Columns(Dictionary<string, object?> amount) => builder =>
    {
        builder.OpenRegion(0);
        builder.OpenComponent<OmniDataGridColumn<Row>>(0);
        builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Group));
        builder.CloseComponent();
        builder.CloseRegion();
        builder.OpenRegion(1);
        builder.OpenComponent<OmniDataGridColumn<Row>>(0);
        builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Amount));
        builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Title), "Valeur");
        foreach (var (name, value) in amount)
        {
            builder.AddComponentParameter(3, name, value);
        }

        builder.CloseComponent();
        builder.CloseRegion();
    };

    // The first text the element holds: a header also carries the buttons of its sort, group and filter.
    private static string Shown(IRenderedComponent<OmniDataGrid<Row>> grid, string selector) =>
        grid.FindAll(selector).FirstOrDefault()?.ChildNodes
            .Select(node => node.TextContent.Trim())
            .FirstOrDefault(text => text.Length > 0)
        ?? string.Empty;

    [Theory]
    [MemberData(nameof(Changes))]
    public async Task ColumnWhoseTemplateOrAggregateChanges_ShowsTheNewContent(string changed)
    {
        var change = Cases[changed];
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.Columns, Columns(change.Before))
            .Add(component => component.KeyOf, row => row.Name)
            .Add(component => component.AllowGrouping, true)
            .Add(component => component.Groups, [new OmniDataGridGroup(nameof(Row.Group))]));
        if (change.EditFirstRow)
        {
            await grid.InvokeAsync(() => grid.Instance.EditRowAsync(Rows[0]));
        }

        Assert.Equal(change.ShownBefore, Shown(grid, change.Selector));

        grid.Render(parameters => parameters.Add(component => component.Columns, Columns(change.After)));

        Assert.Equal(change.ShownAfter, Shown(grid, change.Selector));
    }
}
