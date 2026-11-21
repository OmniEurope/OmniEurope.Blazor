using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The button and the two methods that open or close every group of a grouped grid at once, at every
/// level, in the paged and the virtualized scroll modes.
/// </summary>
public sealed class DataGridExpandAllGroupsTests : OmniBunitContext
{
    private const string ExpandText = "Développer tous les groupes";
    private const string CollapseText = "Réduire tous les groupes";

    // H group header, F group footer, R data row: two regions of two cities, two rows each, all open.
    private const string AllOpen = "HHRRFHRRFFHHRRFHRRFF";

    // Every group closed: the two outer headers, each with its subtotal.
    private const string AllClosed = "HFHF";

    private sealed record Row(string Region, string City, decimal Amount);

    private sealed record Line(int Value, int Bucket);

    private static readonly IReadOnlyList<Row> Rows =
    [
        new("North", "Lille", 1m), new("North", "Lille", 2m), new("North", "Ghent", 3m), new("North", "Ghent", 4m),
        new("South", "Rome", 5m), new("South", "Rome", 6m), new("South", "Turin", 7m), new("South", "Turin", 8m)
    ];

    private readonly CultureInfo _previousCulture = CultureInfo.CurrentCulture;

    // The figures are written in the current culture: pinned so the expected texts hold on any machine.
    public DataGridExpandAllGroupsTests() => CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

    protected override void Dispose(bool disposing)
    {
        CultureInfo.CurrentCulture = _previousCulture;
        base.Dispose(disposing);
    }

    private IRenderedComponent<OmniDataGrid<Row>> RenderGrid(
        bool showExpandAllGroups = true,
        string[]? groups = null,
        bool allGroupsExpanded = true,
        bool showGroupPanel = false,
        int pageSize = 20)
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<OmniDataGridColumn<Row>>(0);
            builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Region));
            builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Title), "Region");
            builder.CloseComponent();
            builder.OpenComponent<OmniDataGridColumn<Row>>(3);
            builder.AddComponentParameter(4, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.City));
            builder.AddComponentParameter(5, nameof(OmniDataGridColumn<Row>.Title), "City");
            builder.CloseComponent();
            builder.OpenComponent<OmniDataGridColumn<Row>>(6);
            builder.AddComponentParameter(7, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Amount));
            builder.AddComponentParameter(8, nameof(OmniDataGridColumn<Row>.Title), "Amount");
            builder.AddComponentParameter(9, nameof(OmniDataGridColumn<Row>.Aggregate), OmniDataGridAggregate.Sum);
            builder.AddComponentParameter(10, nameof(OmniDataGridColumn<Row>.GroupAggregateFormat), "Sous total : {0}");
            builder.CloseComponent();
        };

        return Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(grid => grid.Items, Rows)
            .Add(grid => grid.Columns, columns)
            .Add(grid => grid.AllowGrouping, true)
            .Add(grid => grid.Groups, (groups ?? [nameof(Row.Region), nameof(Row.City)]).Select(key => new OmniDataGridGroup(key)).ToArray())
            .Add(grid => grid.AllGroupsExpanded, allGroupsExpanded)
            .Add(grid => grid.ShowGroupPanel, showGroupPanel)
            .Add(grid => grid.PageSize, pageSize)
            .Add(grid => grid.ShowExpandAllGroups, showExpandAllGroups));
    }

    private static string Body<TItem>(IRenderedComponent<OmniDataGrid<TItem>> grid) => string.Concat(grid.FindAll("tbody tr").Select(row =>
        row.ClassList.Contains("omni-data-grid__group-footer") ? "F" : row.ClassList.Contains("omni-data-grid__group") ? "H" : "R"));

    private static IElement Toggle<TItem>(IRenderedComponent<OmniDataGrid<TItem>> grid) =>
        grid.Find(".omni-data-grid__group-panel > .omni-button");

    private static string ToggleText<TItem>(IRenderedComponent<OmniDataGrid<TItem>> grid) => Toggle(grid).TextContent.Trim();

    private static void AssertHeaderState<TItem>(IRenderedComponent<OmniDataGrid<TItem>> grid, string expanded) =>
        Assert.All(grid.FindAll("tbody tr.omni-data-grid__group .omni-data-grid__group-expand"), header => Assert.Equal(expanded, header.GetAttribute("aria-expanded")));

    [Fact]
    public void Button_IsHidden_WhenTheParameterIsFalse()
    {
        var grid = RenderGrid(showExpandAllGroups: false, showGroupPanel: true);

        Assert.Empty(grid.FindAll(".omni-data-grid__group-panel > .omni-button"));
        Assert.DoesNotContain(CollapseText, grid.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Button_IsHidden_WithoutAGroup()
    {
        var withPanel = RenderGrid(groups: [], showGroupPanel: true);
        var withoutPanel = RenderGrid(groups: []);

        Assert.Empty(withPanel.FindAll(".omni-data-grid__group-panel > .omni-button"));
        Assert.Empty(withoutPanel.FindAll(".omni-data-grid__group-panel"));
    }

    [Fact]
    public void Button_IsHidden_WhenGroupingIsNotAllowed()
    {
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.AllowGrouping, false)
            .Add(component => component.Groups, [new OmniDataGridGroup(nameof(Row.Region))])
            .Add(component => component.ShowExpandAllGroups, true));

        Assert.DoesNotContain(ExpandText, grid.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain(CollapseText, grid.Markup, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Button_IsShownWithAGroup_BesideThePanelOrAlone(bool showGroupPanel)
    {
        var grid = RenderGrid(showGroupPanel: showGroupPanel);

        var button = Toggle(grid);
        Assert.Equal("button", button.LocalName);
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal(CollapseText, button.TextContent.Trim());
        Assert.Equal(showGroupPanel, grid.FindAll(".omni-data-grid__group-chip").Count > 0);
    }

    [Fact]
    public void Click_ClosesEveryGroup_ThenOpensThemAgain()
    {
        var grid = RenderGrid();
        Assert.Equal(AllOpen, Body(grid));
        Assert.Equal(CollapseText, ToggleText(grid));

        Toggle(grid).Click();

        // Every level is closed: only the outer headers remain, each with its subtotal.
        Assert.Equal(AllClosed, Body(grid));
        Assert.Equal(["Sous total : 10", "Sous total : 26"], grid.FindAll("tbody tr.omni-data-grid__group-footer td[data-omni-col='Amount']").Select(cell => cell.TextContent.Trim()));
        AssertHeaderState(grid, "false");
        Assert.Equal(ExpandText, ToggleText(grid));

        Toggle(grid).Click();

        Assert.Equal(AllOpen, Body(grid));
        AssertHeaderState(grid, "true");
        Assert.Equal(CollapseText, ToggleText(grid));
    }

    [Fact]
    public void Button_OffersToOpenAll_WhenOneGroupWasClosedByItself()
    {
        var grid = RenderGrid();

        grid.FindAll("tbody tr.omni-data-grid__group .omni-data-grid__group-expand")[0].Click();
        Assert.Equal(ExpandText, ToggleText(grid));

        Toggle(grid).Click();

        Assert.Equal(AllOpen, Body(grid));
        Assert.Equal(CollapseText, ToggleText(grid));
    }

    [Fact]
    public void Button_StartsOnOpenAll_WhenGroupsStartClosed()
    {
        var grid = RenderGrid(allGroupsExpanded: false);
        Assert.Equal(AllClosed, Body(grid));
        Assert.Equal(ExpandText, ToggleText(grid));

        Toggle(grid).Click();

        Assert.Equal(AllOpen, Body(grid));
    }

    [Fact]
    public async Task Methods_OpenAndCloseEveryGroup_AtEveryLevel()
    {
        var grid = RenderGrid(allGroupsExpanded: false);
        Assert.Equal(AllClosed, Body(grid));

        await grid.InvokeAsync(() => grid.Instance.ExpandAllGroupsAsync());
        Assert.Equal(AllOpen, Body(grid));

        await grid.InvokeAsync(() => grid.Instance.CollapseAllGroupsAsync());
        Assert.Equal(AllClosed, Body(grid));

        // The order wins over the group the reader opened by itself.
        grid.FindAll("tbody tr.omni-data-grid__group .omni-data-grid__group-expand")[0].Click();
        Assert.NotEqual(AllClosed, Body(grid));
        await grid.InvokeAsync(() => grid.Instance.CollapseAllGroupsAsync());
        Assert.Equal(AllClosed, Body(grid));
    }

    [Fact]
    public async Task Methods_WorkWithoutTheButton()
    {
        var grid = RenderGrid(showExpandAllGroups: false);

        await grid.InvokeAsync(() => grid.Instance.CollapseAllGroupsAsync());

        Assert.Equal(AllClosed, Body(grid));
        Assert.Empty(grid.FindAll(".omni-data-grid__group-panel"));
    }

    [Fact]
    public void ChangingAllGroupsExpanded_ReplacesTheStateSetByTheButton()
    {
        var grid = RenderGrid();
        Toggle(grid).Click();
        Assert.Equal(AllClosed, Body(grid));

        grid.Render(parameters => parameters.Add(component => component.AllGroupsExpanded, false));
        grid.Render(parameters => parameters.Add(component => component.AllGroupsExpanded, true));

        Assert.Equal(AllOpen, Body(grid));
    }

    [Fact]
    public void Paged_ClosesTheGroupsOfThePage_AndKeepsThePageCount()
    {
        var grid = RenderGrid(groups: [nameof(Row.Region)], pageSize: 4);
        Assert.Equal("HRRRRF", Body(grid));
        var pager = grid.Find(".omni-pager__status").TextContent;

        Toggle(grid).Click();

        Assert.Equal("HF", Body(grid));
        Assert.Equal(pager, grid.Find(".omni-pager__status").TextContent);

        grid.Find(".omni-pager__button[aria-label='Page suivante']").Click();

        // The next page is closed as well: the choice is the grid's, not the page's.
        Assert.Equal("HF", Body(grid));
        Assert.Equal(ExpandText, ToggleText(grid));

        Toggle(grid).Click();

        Assert.Equal("HRRRRF", Body(grid));
    }

    [Fact]
    public async Task Virtual_ClosesEveryGroup_AndBringsEveryHeaderIntoTheWindow()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<OmniDataGridColumn<Line>>(0);
            builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Line>.Key), "bucket");
            builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Line>.Property), nameof(Line.Bucket));
            builder.CloseComponent();
            builder.OpenComponent<OmniDataGridColumn<Line>>(3);
            builder.AddComponentParameter(4, nameof(OmniDataGridColumn<Line>.Key), "value");
            builder.AddComponentParameter(5, nameof(OmniDataGridColumn<Line>.Property), nameof(Line.Value));
            builder.CloseComponent();
        };
        var grid = Render<OmniDataGrid<Line>>(parameters => parameters
            .Add(component => component.Items, Enumerable.Range(0, 300).Select(value => new Line(value, value / 100)).ToArray())
            .Add(component => component.Columns, columns)
            .Add(component => component.ScrollMode, OmniDataGridScrollMode.Virtual)
            .Add(component => component.EstimatedRowHeight, 40d)
            .Add(component => component.AllowGrouping, true)
            .Add(component => component.Groups, [new OmniDataGridGroup("bucket")])
            .Add(component => component.ShowExpandAllGroups, true));
        await grid.InvokeAsync(() => grid.Instance.OnViewportChangedAsync(0d, 400d));
        Assert.Single(grid.FindAll("tbody tr.omni-data-grid__group"));
        Assert.NotEmpty(grid.FindAll("tbody tr[data-omni-row-index]:not(.omni-data-grid__group)"));

        Toggle(grid).Click();

        // Three groups of a hundred rows are three slots of one header: all of them fit in the window.
        var headers = grid.FindAll("tbody tr.omni-data-grid__group");
        Assert.Equal(3, headers.Count);
        Assert.Equal(["0", "1", "2"], headers.Select(header => header.GetAttribute("data-omni-slot")));
        Assert.Empty(grid.FindAll("tbody tr[data-omni-row-index]:not(.omni-data-grid__group)"));
        Assert.Equal(ExpandText, ToggleText(grid));

        await grid.InvokeAsync(() => grid.Instance.ExpandAllGroupsAsync());

        Assert.Single(grid.FindAll("tbody tr.omni-data-grid__group"));
        Assert.NotEmpty(grid.FindAll("tbody tr[data-omni-row-index]:not(.omni-data-grid__group)"));
        Assert.Equal(CollapseText, ToggleText(grid));
    }
}
