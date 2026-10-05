using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Parts of OmniDataGrid only some settings draw: the pager and the footer above the table, a custom
/// header, the loading row with its own content, the rows of a remote window still on their way, the tree
/// lead of a row in edit mode, the control cells of group footers; and the members the grid script or the
/// host call directly.
/// </summary>
public sealed class DataGridRenderEdgeTests : OmniBunitContext
{
    public sealed record Row(string Name, string Group, decimal Amount)
    {
        public IReadOnlyList<Row> Children { get; init; } = [];
    }

    private static readonly IReadOnlyList<Row> Rows = [new("a", "g1", 1m), new("b", "g2", 2m), new("c", "g1", 3m)];

    /// <summary>One column per parameter set, each in its own region so a later render keeps them apart.</summary>
    private static RenderFragment Columns(params Dictionary<string, object?>[] columns) => builder =>
    {
        for (var index = 0; index < columns.Length; index++)
        {
            builder.OpenRegion(index);
            builder.OpenComponent<OmniDataGridColumn<Row>>(0);
            foreach (var (name, value) in columns[index])
            {
                builder.AddComponentParameter(1, name, value);
            }

            builder.CloseComponent();
            builder.CloseRegion();
        }
    };

    private static Dictionary<string, object?> Column(string property, params (string Name, object? Value)[] more)
    {
        var parameters = new Dictionary<string, object?> { [nameof(OmniDataGridColumn<Row>.Property)] = property };
        foreach (var (name, value) in more)
        {
            parameters[name] = value;
        }

        return parameters;
    }

    private static RenderFragment Editor => builder => builder.AddContent(0, "édition");

    [Fact]
    public void GroupPanel_WithoutGrouping_IsNotDrawn()
    {
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.Columns, Columns(Column(nameof(Row.Name))))
            .Add(component => component.ShowGroupPanel, true));

        Assert.Empty(grid.FindAll(".omni-data-grid__group-panel"));
    }

    [Fact]
    public void PagerAndFooterOnTop_AndACustomHeader_AreDrawnAboveTheRows()
    {
        RenderFragment header = builder => builder.AddMarkupContent(0, "<em>Nom propre</em>");
        RenderFragment footer = builder => builder.AddContent(0, "pied");
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.Columns, Columns(Column(nameof(Row.Name),
                (nameof(OmniDataGridColumn<Row>.HeaderContent), header),
                (nameof(OmniDataGridColumn<Row>.FooterContent), footer))))
            .Add(component => component.PageSize, 2)
            .Add(component => component.PagerPosition, OmniDataGridPosition.Top)
            .Add(component => component.FooterPosition, OmniDataGridPosition.Top));

        var markup = grid.Markup;
        Assert.True(markup.IndexOf("omni-pager", StringComparison.Ordinal) < markup.IndexOf("<table", StringComparison.Ordinal));
        Assert.Equal("Nom propre", grid.Find("thead em").TextContent);
        Assert.True(markup.IndexOf("pied", StringComparison.Ordinal) < markup.IndexOf("<tbody", StringComparison.Ordinal));
    }

    [Fact]
    public void LoadingRow_ShowsTheHostContent()
    {
        var pending = new TaskCompletionSource<OmniDataGridResult<Row>>();
        RenderFragment loading = builder => builder.AddContent(0, "Patience");
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Load, _ => pending.Task)
            .Add(component => component.Columns, Columns(Column(nameof(Row.Name))))
            .Add(component => component.ShowLoadingBar, false)
            .Add(component => component.LoadingContent, loading));

        Assert.Equal("Patience", grid.Find(".omni-data-grid__state--loading").TextContent);
        pending.SetResult(new OmniDataGridResult<Row>([], 0));
    }

    [Fact]
    public void RemoteWindowStillLoading_DrawsPlaceholderRows()
    {
        var module = JSInterop.SetupModule(OmniModules.Grid);
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<GridViewportSnapshot?>("sync", _ => true).SetResult(new GridViewportSnapshot { ScrollTop = 200_000, ViewportHeight = 400 });
        var later = new TaskCompletionSource<OmniDataGridResult<int>>();
        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Load, request => request.Skip == 0
                ? Task.FromResult(new OmniDataGridResult<int>([.. Enumerable.Range(0, request.Top)], 10_000))
                : later.Task)
            .Add(component => component.ScrollMode, OmniDataGridScrollMode.Virtual)
            .Add(component => component.EstimatedRowHeight, 40d));

        grid.WaitForAssertion(() => Assert.NotEmpty(grid.FindAll("tr.omni-data-grid__row--placeholder")));
        Assert.Equal("Chargement des lignes…", grid.Find("tr.omni-data-grid__row--placeholder .omni-visually-hidden").TextContent);
        later.SetResult(new OmniDataGridResult<int>([], 10_000));
    }

    [Fact]
    public async Task TreeRowInEditMode_KeepsItsLead()
    {
        var root = new Row("racine", "g", 0m) { Children = [new Row("feuille", "g", 1m)] };
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, [root])
            .Add(component => component.Columns, Columns(Column(nameof(Row.Name), (nameof(OmniDataGridColumn<Row>.EditTemplate), (RenderFragment<Row>)(_ => Editor)))))
            .Add(component => component.ChildrenOf, row => row.Children)
            .Add(component => component.KeyOf, row => row.Name));

        await grid.InvokeAsync(() => grid.Instance.EditRowAsync(root));

        var cell = grid.Find("tbody tr td[data-omni-col='Name']");
        Assert.NotNull(cell.QuerySelector(".omni-data-grid__tree"));
        Assert.Contains("édition", cell.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void GroupFooter_KeepsTheDetailAndEditCells()
    {
        RenderFragment<Row> detail = row => builder => builder.AddContent(0, row.Name);
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.Columns, Columns(
                Column(nameof(Row.Group)),
                Column(nameof(Row.Amount),
                    (nameof(OmniDataGridColumn<Row>.Aggregate), OmniDataGridAggregate.Sum),
                    (nameof(OmniDataGridColumn<Row>.EditTemplate), (RenderFragment<Row>)(_ => Editor)))))
            .Add(component => component.AllowGrouping, true)
            .Add(component => component.Groups, [new OmniDataGridGroup(nameof(Row.Group))])
            .Add(component => component.DetailTemplate, detail));

        var footer = grid.FindAll("tr.omni-data-grid__group-footer")[0];
        var cells = footer.QuerySelectorAll("td");
        Assert.Equal("expand", cells[0].GetAttribute("data-omni-control"));
        Assert.Equal(4, cells.Length);
        Assert.Equal(string.Empty, cells[^1].TextContent);
    }

    [Fact]
    public async Task CancelEdit_RaisesTheCancel_AndAResizeFromTheScriptIsReported()
    {
        var cancelled = new List<Row>();
        var resized = new List<OmniDataGridColumnWidthChange>();
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.Columns, Columns(Column(nameof(Row.Name), (nameof(OmniDataGridColumn<Row>.EditTemplate), (RenderFragment<Row>)(_ => Editor)))))
            .Add(component => component.KeyOf, row => row.Name)
            .Add(component => component.OnRowEditCancel, row => cancelled.Add(row))
            .Add(component => component.OnColumnResize, change => resized.Add(change)));

        await grid.InvokeAsync(() => grid.Instance.EditRowAsync(Rows[0]));
        await grid.InvokeAsync(() => grid.Instance.CancelEditAsync(Rows[0]));
        await grid.InvokeAsync(() => grid.Instance.OnColumnResizedAsync("Name", 120));
        // A width for a column the grid does not show, or reported once resizing is off, is ignored.
        await grid.InvokeAsync(() => grid.Instance.OnColumnResizedAsync("Missing", 80));
        grid.Render(parameters => parameters.Add(component => component.AllowColumnResize, false));
        await grid.InvokeAsync(() => grid.Instance.OnColumnResizedAsync("Name", 90));

        Assert.Equal([Rows[0]], cancelled);
        Assert.Equal([new OmniDataGridColumnWidthChange("Name", "120px")], resized);
    }

    [Fact]
    public async Task ContentResized_RendersOnlyAVirtualGrid()
    {
        var paged = Render<OmniDataGrid<int>>(parameters => parameters.Add(component => component.Items, [1, 2]));
        var renders = paged.RenderCount;
        await paged.InvokeAsync(paged.Instance.OnContentResizedAsync);
        Assert.Equal(renders, paged.RenderCount);

        var virtualized = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, [1, 2])
            .Add(component => component.ScrollMode, OmniDataGridScrollMode.Virtual));
        renders = virtualized.RenderCount;
        await virtualized.InvokeAsync(virtualized.Instance.OnContentResizedAsync);
        Assert.Equal(renders + 1, virtualized.RenderCount);
    }

    [Fact]
    public async Task ScrollToIndex_WaitsForAVirtualGridAndItsScript()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        Services.AddSingleton<IJSRuntime>(runtime);
        var paged = Render<OmniDataGrid<int>>(parameters => parameters.Add(component => component.Items, [1, 2]));
        var loading = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, [1, 2])
            .Add(component => component.ScrollMode, OmniDataGridScrollMode.Virtual));

        await paged.InvokeAsync(() => paged.Instance.ScrollToIndexAsync(1));
        await loading.InvokeAsync(() => loading.Instance.ScrollToIndexAsync(1));

        Assert.Empty(runtime.Module.Calls);
        // The grids are released once their script arrives.
        runtime.PendingImport.SetResult(runtime.Module);
    }

    [Fact]
    public void RemoteVirtualGrid_WithGroups_IsRefused()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Load, _ => Task.FromResult(new OmniDataGridResult<Row>([], 0)))
            .Add(component => component.Columns, Columns(Column(nameof(Row.Group))))
            .Add(component => component.ScrollMode, OmniDataGridScrollMode.Virtual)
            .Add(component => component.AllowGrouping, true)
            .Add(component => component.Groups, [new OmniDataGridGroup(nameof(Row.Group))])));

        Assert.Contains("Groups", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RemoteVirtualGrid_WithADetailTemplate_IsRefused()
    {
        RenderFragment<Row> detail = row => builder => builder.AddContent(0, row.Name);
        var error = Assert.Throws<InvalidOperationException>(() => Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Load, _ => Task.FromResult(new OmniDataGridResult<Row>([], 0)))
            .Add(component => component.Columns, Columns(Column(nameof(Row.Group))))
            .Add(component => component.ScrollMode, OmniDataGridScrollMode.Virtual)
            .Add(component => component.DetailTemplate, detail)));

        Assert.Contains("DetailTemplate", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadRequest_NamesItsPrimarySort()
    {
        var sorted = new OmniDataGridLoadRequest(1, 20, [new OmniDataGridSort("Name", true), new OmniDataGridSort("Amount", false)], [], CancellationToken.None);
        var unsorted = new OmniDataGridLoadRequest(1, 20, [], [], CancellationToken.None);

        Assert.Equal(("Name", true), (sorted.SortKey, sorted.SortDescending));
        Assert.Equal((null, false), (unsorted.SortKey, unsorted.SortDescending));
    }
}
