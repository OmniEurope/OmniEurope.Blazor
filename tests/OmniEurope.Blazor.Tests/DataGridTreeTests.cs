using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Tree rows (ChildrenOf): an income statement of categories, sub-totals and accounts, opened and closed by
/// the chevron, the row click or the arrows, with always-open rows, an initial state, sorting within
/// parents and filters that keep the ancestors of what they match.
/// </summary>
public sealed class DataGridTreeTests : OmniBunitContext
{
    private sealed record Line(string Id, string Label, decimal Amount, IReadOnlyList<Line>? Children = null);

    private static readonly IReadOnlyList<Line> Statement =
    [
        new("P", "Produits", 300m,
        [
            new("P70", "Ventes", 250m, [new("706", "Prestations", 200m), new("701", "Produits finis", 50m)]),
            new("P75", "Autres produits", 50m)
        ]),
        new("C", "Charges", 120m,
        [
            new("C60", "Achats", 120m, [new("607", "Marchandises", 120m)])
        ])
    ];

    private IRenderedComponent<OmniDataGrid<Line>> RenderGrid(Action<ComponentParameterCollectionBuilder<OmniDataGrid<Line>>>? configure = null)
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<OmniDataGridColumn<Line>>(0);
            builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Line>.Property), nameof(Line.Label));
            builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Line>.Title), "Poste");
            builder.CloseComponent();
            builder.OpenComponent<OmniDataGridColumn<Line>>(3);
            builder.AddComponentParameter(4, nameof(OmniDataGridColumn<Line>.Property), nameof(Line.Amount));
            builder.AddComponentParameter(5, nameof(OmniDataGridColumn<Line>.Title), "Montant");
            builder.CloseComponent();
        };

        return Render<OmniDataGrid<Line>>(parameters =>
        {
            parameters.Add(grid => grid.Items, Statement)
                .Add(grid => grid.Columns, columns)
                .Add(grid => grid.KeyOf, line => line.Id)
                .Add(grid => grid.ChildrenOf, line => line.Children)
                .Add(grid => grid.PageSize, 2);
            configure?.Invoke(parameters);
        });
    }

    private static IEnumerable<string> Labels(IRenderedComponent<OmniDataGrid<Line>> grid) =>
        grid.FindAll("tbody tr[data-omni-row-index] td[data-omni-col='Label']").Select(cell => cell.TextContent.Trim());

    [Fact]
    public void StartsClosed_RootsOnly_AsATreegridWithLevelsAndChevrons()
    {
        var grid = RenderGrid();

        Assert.Equal(["Produits", "Charges"], Labels(grid));
        Assert.Equal("treegrid", grid.Find("table").GetAttribute("role"));
        var first = grid.Find("tbody tr[data-omni-row-index]");
        Assert.Equal("1", first.GetAttribute("aria-level"));
        Assert.Equal("false", first.GetAttribute("aria-expanded"));
        Assert.Equal("Développer la ligne", first.QuerySelector("td[data-omni-col='Label'] .omni-data-grid__tree-toggle")!.GetAttribute("aria-label"));
        Assert.Null(first.QuerySelector("td[data-omni-col='Amount'] .omni-data-grid__tree"));
        // A tree grid renders every row: no pager despite a page size of 2.
        Assert.Empty(grid.FindAll(".omni-pager"));
    }

    [Fact]
    public void Chevron_OpensOneLevel_AndClosingHidesEveryDescendant()
    {
        IReadOnlyList<object>? reported = null;
        var grid = RenderGrid(parameters => parameters.Add(g => g.TreeExpandedKeysChanged, keys => reported = keys));

        grid.Find("tbody tr[data-omni-row-index] .omni-data-grid__tree-toggle").Click();
        Assert.Equal(["Produits", "Ventes", "Autres produits", "Charges"], Labels(grid));
        Assert.Equal(["P"], reported!);
        var ventes = grid.FindAll("tbody tr[data-omni-row-index]")[1];
        Assert.Equal("2", ventes.GetAttribute("aria-level"));
        Assert.Equal("1", ventes.QuerySelector(".omni-data-grid__tree")!.GetAttribute("data-omni-tree-level"));
        // A leaf has a spacer, no chevron and no aria-expanded.
        var leaf = grid.FindAll("tbody tr[data-omni-row-index]")[2];
        Assert.Null(leaf.GetAttribute("aria-expanded"));
        Assert.NotNull(leaf.QuerySelector(".omni-data-grid__tree-spacer"));

        grid.FindAll("tbody tr[data-omni-row-index]")[1].QuerySelector(".omni-data-grid__tree-toggle")!.Click();
        Assert.Equal(["Produits", "Ventes", "Prestations", "Produits finis", "Autres produits", "Charges"], Labels(grid));

        grid.Find("tbody tr[data-omni-row-index] .omni-data-grid__tree-toggle").Click();
        Assert.Equal(["Produits", "Charges"], Labels(grid));
    }

    [Fact]
    public void AlwaysOpenRows_ShowTheirChildrenWithoutChevron_AndInitiallyExpandedDecidesTheOthers()
    {
        var grid = RenderGrid(parameters => parameters
            .Add(g => g.CanToggleTreeRow, line => line.Id.Length > 1)
            .Add(g => g.InitiallyExpanded, line => line.Id == "C60"));

        Assert.Equal(["Produits", "Ventes", "Autres produits", "Charges", "Achats", "Marchandises"], Labels(grid));
        var produits = grid.Find("tbody tr[data-omni-row-index]");
        Assert.Null(produits.GetAttribute("aria-expanded"));
        Assert.Null(produits.QuerySelector(".omni-data-grid__tree-toggle"));
    }

    [Fact]
    public void RowClick_Toggles_ADoubleClickLeavesTheRowAsItWas_AndTheCellDoubleClickStillFires()
    {
        var doubleClicks = 0;
        var grid = RenderGrid(parameters => parameters
            .Add(g => g.ToggleTreeOnRowClick, true)
            .Add(g => g.OnCellDoubleClick, _ => doubleClicks++));

        grid.FindAll("tbody tr[data-omni-row-index]")[1].Click(new MouseEventArgs { Detail = 1 });
        Assert.Equal(["Produits", "Charges", "Achats"], Labels(grid));

        // A double click: two clicks, then the dblclick event.
        grid.FindAll("tbody tr[data-omni-row-index]")[1].Click(new MouseEventArgs { Detail = 1 });
        Assert.Equal(["Produits", "Charges"], Labels(grid));
        grid.FindAll("tbody tr[data-omni-row-index]")[1].Click(new MouseEventArgs { Detail = 2 });
        grid.FindAll("tbody tr[data-omni-row-index]")[1].QuerySelector("td[data-omni-col='Amount']")!.DoubleClick();
        Assert.Equal(["Produits", "Charges", "Achats"], Labels(grid));
        Assert.Equal(1, doubleClicks);
    }

    [Fact]
    public void Arrows_OpenAndCloseTheFocusedRow()
    {
        var grid = RenderGrid(parameters => parameters.Add(g => g.ToggleTreeOnRowClick, true));

        grid.Find("tbody tr[data-omni-row-index]").KeyDown("ArrowRight");
        Assert.Equal(["Produits", "Ventes", "Autres produits", "Charges"], Labels(grid));
        grid.Find("tbody tr[data-omni-row-index]").KeyDown("ArrowRight");
        Assert.Equal(4, Labels(grid).Count());
        grid.Find("tbody tr[data-omni-row-index]").KeyDown("ArrowLeft");
        Assert.Equal(["Produits", "Charges"], Labels(grid));
    }

    [Fact]
    public async Task ExpandAllAndCollapseAll_ReportTheKeys()
    {
        IReadOnlyList<object> reported = [];
        var grid = RenderGrid(parameters => parameters.Add(g => g.TreeExpandedKeysChanged, keys => reported = keys));

        await grid.InvokeAsync(grid.Instance.ExpandAllTreeRowsAsync);
        Assert.Equal(8, Labels(grid).Count());
        Assert.Equal(["C", "C60", "P", "P70"], reported.Cast<string>().Order());

        await grid.InvokeAsync(grid.Instance.CollapseAllTreeRowsAsync);
        Assert.Equal(["Produits", "Charges"], Labels(grid));
        Assert.Empty(reported);
    }

    [Fact]
    public void HostKeys_OpenTheRows()
    {
        var grid = RenderGrid(parameters => parameters.Add(g => g.TreeExpandedKeys, ["C", "C60"]));

        Assert.Equal(["Produits", "Charges", "Achats", "Marchandises"], Labels(grid));
    }

    [Fact]
    public async Task Sorting_OrdersSiblingsWithinTheirParent()
    {
        var grid = RenderGrid(parameters => parameters.Add(g => g.TreeExpandedKeys, ["P", "P70"]));

        grid.Find("th[data-omni-col='Amount'] .omni-data-grid__sort").Click();
        await Task.Yield();

        Assert.Equal(["Charges", "Produits", "Autres produits", "Ventes", "Produits finis", "Prestations"], Labels(grid));
    }

    [Fact]
    public async Task Filter_KeepsTheAncestorsOfAMatch_ShownOpen()
    {
        var grid = RenderGrid();

        await grid.InvokeAsync(() => grid.Instance.SetFiltersAsync(new Dictionary<string, string?> { [nameof(Line.Label)] = "Marchand" }));

        Assert.Equal(["Charges", "Achats", "Marchandises"], Labels(grid));
    }

    [Fact]
    public void FooterAggregate_CountsTheRootsOnly()
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<OmniDataGridColumn<Line>>(0);
            builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Line>.Property), nameof(Line.Amount));
            builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Line>.Title), "Montant");
            builder.AddComponentParameter(3, nameof(OmniDataGridColumn<Line>.Aggregate), OmniDataGridAggregate.Count);
            builder.CloseComponent();
        };
        var grid = Render<OmniDataGrid<Line>>(parameters => parameters
            .Add(g => g.Items, Statement).Add(g => g.Columns, columns).Add(g => g.KeyOf, line => line.Id)
            .Add(g => g.ChildrenOf, line => line.Children).Add(g => g.TreeExpandedKeys, ["P"]));

        Assert.Equal("2", grid.Find("tfoot td[data-omni-col='Amount']").TextContent.Trim());
    }

    [Theory]
    [InlineData("Amount", "Amount")]
    [InlineData("Absent", "Label")]
    public void TreeColumnKey_PutsTheChevronInThatColumn_OrInTheFirstWhenItIsNotShown(string key, string expected)
    {
        var grid = RenderGrid(parameters => parameters.Add(g => g.TreeColumnKey, key));

        var toggle = Assert.Single(grid.FindAll("tbody tr[data-omni-row-index]")[0].QuerySelectorAll(".omni-data-grid__tree-toggle"));
        Assert.Equal(expected, toggle.Closest("td")!.GetAttribute("data-omni-col"));
    }

    [Fact]
    public void Groups_AreIgnoredByATreeGrid()
    {
        var grid = RenderGrid(parameters => parameters
            .Add(g => g.AllowGrouping, true)
            .Add(g => g.Groups, [new OmniDataGridGroup(nameof(Line.Label))]));

        Assert.Empty(grid.FindAll("tbody tr.omni-data-grid__group"));
        Assert.Equal(["Produits", "Charges"], Labels(grid));
    }

    [Fact]
    public void TreeRows_WithADetailTemplate_OpenTheirDetail()
    {
        RenderFragment<Line> detail = line => builder => builder.AddContent(0, $"Détail {line.Label}");
        var grid = RenderGrid(parameters => parameters.Add(g => g.DetailTemplate, detail));

        grid.Find("tbody td[data-omni-control='expand'] button").Click();

        Assert.Contains("Détail Produits", grid.Find(".omni-data-grid__detail").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void CsvExport_WritesEveryRowAtEveryDepth_HoweverFewAreOpen()
    {
        var download = JSInterop.SetupModule(Internal.OmniModules.DocumentEditor);
        download.SetupVoid("download", _ => true).SetVoidResult();
        var grid = RenderGrid(parameters => parameters.Add(g => g.ExportFormats, [OmniTableExportFormat.Csv]));
        Assert.Equal(["Produits", "Charges"], Labels(grid));

        grid.Find(".omni-data-grid__export-button").Click();

        var csv = System.Text.Encoding.UTF8.GetString((byte[])Assert.Single(download.Invocations["download"]).Arguments[2]!);
        foreach (var label in new[] { "Produits", "Ventes", "Prestations", "Produits finis", "Autres produits", "Charges", "Achats", "Marchandises" })
        {
            Assert.Contains(label, csv, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void RemoteLoad_IsRefused()
    {
        Assert.Throws<InvalidOperationException>(() => Render<OmniDataGrid<Line>>(parameters => parameters
            .Add(g => g.Load, _ => Task.FromResult(new OmniDataGridResult<Line>([], 0)))
            .Add(g => g.ChildrenOf, line => line.Children)));
    }
}
