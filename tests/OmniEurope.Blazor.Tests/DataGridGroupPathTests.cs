using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The identity of a group: values with a slash or a backslash, and null, never make two groups share
/// a count or an open state (audit RCL-GRID-GROUP-KEY-001).
/// </summary>
public sealed class DataGridGroupPathTests : OmniBunitContext
{
    private sealed record Row(string Name, string? Outer, string? Inner);

    private IRenderedComponent<OmniDataGrid<Row>> RenderGrid(IReadOnlyList<Row> rows, params string[] groups)
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<OmniDataGridColumn<Row>>(0);
            builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Name));
            builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Title), "Nom");
            builder.CloseComponent();
            builder.OpenComponent<OmniDataGridColumn<Row>>(3);
            builder.AddComponentParameter(4, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Outer));
            builder.AddComponentParameter(5, nameof(OmniDataGridColumn<Row>.Title), "Externe");
            builder.CloseComponent();
            builder.OpenComponent<OmniDataGridColumn<Row>>(6);
            builder.AddComponentParameter(7, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Inner));
            builder.AddComponentParameter(8, nameof(OmniDataGridColumn<Row>.Title), "Interne");
            builder.CloseComponent();
        };

        return Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(grid => grid.Items, rows)
            .Add(grid => grid.Columns, columns)
            .Add(grid => grid.AllowGrouping, true)
            .Add(grid => grid.Groups, [.. groups.Select(key => new OmniDataGridGroup(key))]));
    }

    private static IReadOnlyList<string> Headers(IRenderedComponent<OmniDataGrid<Row>> grid) =>
        [.. grid.FindAll("tbody tr.omni-data-grid__group").Select(row => row.TextContent.Trim())];

    private static IReadOnlyList<string> Names(IRenderedComponent<OmniDataGrid<Row>> grid) =>
        [.. grid.FindAll("tbody tr[data-omni-row-index] td[data-omni-col='Name']").Select(cell => cell.TextContent.Trim())];

    [Fact]
    public void SlashInsideAValue_KeepsTwoGroupsApart()
    {
        // (a/b, c) and (a, b/c) both used to read /a/b/c: one shared count of 2 and one open state.
        var grid = RenderGrid([new("first", "a/b", "c"), new("second", "a", "b/c")], nameof(Row.Outer), nameof(Row.Inner));

        Assert.Equal(["Externe : a (1)", "Interne : b/c (1)", "Externe : a/b (1)", "Interne : c (1)"], Headers(grid));

        // Closing the inner group of "a" hides its row only.
        grid.FindAll("tbody tr.omni-data-grid__group button")[1].Click();

        Assert.Equal(["first"], Names(grid));
    }

    [Fact]
    public void BackslashInsideAValue_KeepsTwoGroupsApart()
    {
        // With the slash escaped alone, the inner group b of a\ and the outer group a/b would both read a\/b.
        var grid = RenderGrid([new("first", @"a\", "b"), new("second", "a/b", "x")], nameof(Row.Outer), nameof(Row.Inner));

        Assert.All(Headers(grid), header => Assert.EndsWith("(1)", header, StringComparison.Ordinal));
        Assert.Equal(4, Headers(grid).Count);
    }

    [Fact]
    public void GroupNamingNoColumn_IsDropped_WithoutShiftingTheDirectionOfTheNextGroup()
    {
        // The second group sorts its values descending; the first names no column. Its direction used
        // to be read for the second group, which then sorted ascending.
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, [new Row("first", "a", null), new Row("second", "b", null)])
            .Add(component => component.Columns, (RenderFragment)(builder =>
            {
                builder.OpenComponent<OmniDataGridColumn<Row>>(0);
                builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Outer));
                builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Title), "Externe");
                builder.CloseComponent();
            }))
            .Add(component => component.AllowGrouping, true)
            .Add(component => component.Groups, [new OmniDataGridGroup("missing"), new OmniDataGridGroup(nameof(Row.Outer), Descending: true)]));

        Assert.Equal(["Externe : b (1)", "Externe : a (1)"], Headers(grid));
    }

    [Fact]
    public void NullValue_IsNotTheGroupOfAValueThatReadsLikeTheMarker()
    {
        // The null marker was an object written as "System.Object": a value with that text joined its group.
        var grid = RenderGrid([new("first", null, null), new("second", "System.Object", null), new("third", @"\0", null)], nameof(Row.Outer));

        Assert.Equal(3, Headers(grid).Count);
        Assert.All(Headers(grid), header => Assert.EndsWith("(1)", header, StringComparison.Ordinal));
    }
}
