using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The edges of the smaller Data components: a grid column outside a grid or whose key and delegates
/// change, the date range and multiple choice filters, the pager's refused pages and sizes, the export
/// button and downloader on a lost circuit, and the tree's selection toggled off.
/// </summary>
public sealed class DataFamilyEdgeTests : OmniBunitContext
{
    public sealed record Row(string Name);

    private static object? ReadName(Row row) => row.Name;

    private static object? ReadLength(Row row) => row.Name.Length;

    private static RenderFragment Column(params (string Name, object? Value)[] parameters) => builder =>
    {
        builder.OpenComponent<OmniDataGridColumn<Row>>(0);
        foreach (var (name, value) in parameters)
        {
            builder.AddComponentParameter(1, name, value);
        }

        builder.CloseComponent();
    };

    private IRenderedComponent<OmniDataGrid<Row>> RenderGrid(RenderFragment columns) =>
        Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, [new Row("a")])
            .Add(component => component.Columns, columns));

    // ---- grid column ------------------------------------------------------------------------------

    [Fact]
    public void ColumnOutsideAGrid_RegistersNowhere_AndLeavesQuietly()
    {
        var column = Render<OmniDataGridColumn<Row>>(parameters => parameters.Add(component => component.Property, nameof(Row.Name)));

        column.Instance.Dispose();

        Assert.Empty(column.Markup.Trim());
    }

    [Fact]
    public void ColumnWhoseKeyChanges_LeavesItsOldPlace()
    {
        var grid = RenderGrid(Column((nameof(OmniDataGridColumn<Row>.Key), "a"), (nameof(OmniDataGridColumn<Row>.Value), (Func<Row, object?>)ReadName)));

        grid.Render(parameters => parameters.Add(component => component.Columns,
            Column((nameof(OmniDataGridColumn<Row>.Key), "b"), (nameof(OmniDataGridColumn<Row>.Value), (Func<Row, object?>)ReadName))));

        Assert.Equal(["b"], grid.FindAll("thead th[data-omni-col]").Select(header => header.GetAttribute("data-omni-col")));
    }

    [Fact]
    public void ColumnDelegatesAndFilterValues_ThatChange_AreTakenAtEachRender()
    {
        Func<Row, object?> closure = row => row.Name + "!";
        var steps = new (Func<Row, object?>? Value, IReadOnlyList<string>? Filters, string Cell)[]
        {
            (null, null, string.Empty),
            (ReadName, ["a"], "a"),
            (ReadLength, ["a"], "1"),
            (closure, null, "a!"),
            (ReadName, ["a", "b"], "a"),
            (null, null, string.Empty),
        };

        var grid = RenderGrid(Column((nameof(OmniDataGridColumn<Row>.Key), "k")));
        foreach (var (value, filters, cell) in steps)
        {
            grid.Render(parameters => parameters.Add(component => component.Columns, Column(
                (nameof(OmniDataGridColumn<Row>.Key), "k"),
                (nameof(OmniDataGridColumn<Row>.Value), value),
                (nameof(OmniDataGridColumn<Row>.FilterValues), filters))));

            Assert.Equal(cell, grid.Find("tbody td[data-omni-col='k']").TextContent.Trim());
        }
    }

    [Fact]
    public void ColumnFilterListsAndClosures_ChangedAloneEachTime_AreTakenAtEachRender()
    {
        static Func<Row, object?> Suffixed(string suffix) => row => row.Name + suffix;
        var steps = new (Func<Row, object?> Value, IReadOnlyList<string>? Filters, IReadOnlyList<OmniDataGridFilterOperator>? Operators, string Cell)[]
        {
            (Suffixed("!"), null, null, "a!"),
            (Suffixed("?"), null, null, "a?"),
            (Suffixed("?"), ["a"], null, "a?"),
            (Suffixed("?"), ["a"], [OmniDataGridFilterOperator.Equals], "a?"),
            (Suffixed("?"), ["b"], [OmniDataGridFilterOperator.Equals], "a?"),
            (Suffixed("?"), ["b"], null, "a?"),
            (Suffixed("?"), null, null, "a?"),
        };

        var grid = RenderGrid(Column((nameof(OmniDataGridColumn<Row>.Key), "k")));
        foreach (var (value, filters, operators, cell) in steps)
        {
            grid.Render(parameters => parameters.Add(component => component.Columns, Column(
                (nameof(OmniDataGridColumn<Row>.Key), "k"),
                (nameof(OmniDataGridColumn<Row>.Value), value),
                (nameof(OmniDataGridColumn<Row>.FilterValues), filters),
                (nameof(OmniDataGridColumn<Row>.FilterOperators), operators))));

            Assert.Equal(cell, grid.Find("tbody td[data-omni-col='k']").TextContent.Trim());
        }
    }

    // ---- filters ----------------------------------------------------------------------------------

    [Theory]
    [InlineData("2026-01-05T08:30/2026-01-09", false, "2026-01-05", "2026-01-09")]
    [InlineData("2026-01-05/2026-01-09T18:00", true, "2026-01-05T00:00", "2026-01-09T18:00")]
    public void DateRange_ShowsASavedValueInThePickerShape(string value, bool time, string start, string end)
    {
        var range = Render<OmniDataGridFilterDateRange>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.IncludesTime, time));

        Assert.Equal(start, range.Find(".omni-data-grid__date-range-start").GetAttribute("value"));
        Assert.Equal(end, range.Find(".omni-data-grid__date-range-end").GetAttribute("value"));
    }

    [Fact]
    public void DateRange_ClearedPickers_SendEmptySides()
    {
        var changes = new List<string>();
        var range = Render<OmniDataGridFilterDateRange>(parameters => parameters
            .Add(component => component.Value, "2026-01-05/2026-01-09")
            .Add(component => component.ValueChanged, value => changes.Add(value)));

        range.Find(".omni-data-grid__date-range-start").Change((object?)null);
        range.Find(".omni-data-grid__date-range-end").Change((object?)null);

        Assert.Equal([OmniDataGridDateRange.Join(null, "2026-01-09"), OmniDataGridDateRange.Join("2026-01-05", null)], changes);
    }

    [Fact]
    public void MultiSelect_WithoutPlaceholder_ShowsNothing_ReadsANullSearch_AndUnticks()
    {
        var changes = new List<string>();
        var select = Render<OmniDataGridFilterMultiSelect>(parameters => parameters
            .Add(component => component.Suggestions, ["a", "b"])
            .Add(component => component.Filterable, true)
            .Add(component => component.ValueChanged, value => changes.Add(value)));
        Assert.Equal(string.Empty, select.Find(".omni-data-grid__multi-text").TextContent);

        select.Find(".omni-multi-select__search").Input((object?)null);
        Assert.Equal(2, select.FindAll("li[role=option]").Count);

        select.Render(parameters => parameters.Add(component => component.Value, OmniDataGridFilterValues.Join(["a", "b"])));
        select.FindAll(".omni-multi-select__checkbox")[0].Change(false);

        Assert.Equal([OmniDataGridFilterValues.Join(["b"])], changes);
    }

    [Fact]
    public void ListPresentation_DrawsTheListWithoutAFold()
    {
        var select = Render<OmniDataGridFilterMultiSelect>(parameters => parameters
            .Add(component => component.Suggestions, ["a"])
            .Add(component => component.Presentation, OmniMultiSelectPresentation.List));

        Assert.Empty(select.FindAll("details"));
        Assert.Single(select.FindAll(".omni-multi-select li"));
    }

    [Fact]
    public void FilterText_NullWithoutDiacriticFolding_IsEmpty() =>
        Assert.Equal(string.Empty, OmniDataGridFilterText.Normalize(null, ignoreDiacritics: false));

    // ---- pager ------------------------------------------------------------------------------------

    [Fact]
    public void Pager_RefusesThePageItIsOn_PagesOutside_AndSizesThatAreNotSizes()
    {
        var pages = new List<int>();
        var sizes = new List<int>();
        var pager = Render<OmniPager>(parameters => parameters
            .Add(component => component.Page, 1)
            .Add(component => component.PageCount, 1)
            .Add(component => component.NumericPageCount, 3)
            .Add(component => component.ShowFirstLast, true)
            .Add(component => component.PageSize, 20)
            .Add(component => component.PageSizeOptions, [20, 50])
            .Add(component => component.PageChanged, page => pages.Add(page))
            .Add(component => component.PageSizeChanged, size => sizes.Add(size)));

        foreach (var label in new[] { "Première page", "Page précédente", "Page suivante", "Dernière page" })
        {
            pager.Find($"button[aria-label='{label}']").Click();
        }

        pager.Find("select").Change("abc");
        pager.Find("select").Change("0");

        Assert.Empty(pages);
        Assert.Empty(sizes);
        Assert.Equal("omni-pager-page-size", pager.Find("select").Id);
        pager.Render(parameters => parameters.Add(component => component.Id, "lignes"));
        Assert.Equal("lignes-page-size", pager.Find("select").Id);
    }

    [Fact]
    public async Task LocalStorageStore_WritesTheStateUnderItsKey()
    {
        JSInterop.SetupVoid("localStorage.setItem", "grille", "{}").SetVoidResult();
        var store = new OmniLocalStorageDataGridStateStore(JSInterop.JSRuntime);

        await store.SaveAsync("grille", "{}", Xunit.TestContext.Current.CancellationToken);

        Assert.Single(JSInterop.Invocations["localStorage.setItem"]);
    }

    // ---- export -----------------------------------------------------------------------------------

    [Fact]
    public void ExportButton_WithoutAnExport_DoesNothing()
    {
        var exported = 0;
        var button = Render<OmniMarkdownExportButton<Row>>(parameters => parameters.Add(component => component.OnExport, _ => exported++));

        button.Find("button").Click();

        Assert.Equal(0, exported);
    }

    [Fact]
    public async Task ExportButton_ClickedAgainWhileBusy_ReadsOnce_AndReleasesOnALostCircuit()
    {
        var runtime = new ManualJSRuntime { Module = new RecordingModule { DisposeFailure = new JSDisconnectedException("perdu") } };
        Services.AddSingleton<IJSRuntime>(runtime);
        var pending = new TaskCompletionSource<OmniDataGridResult<Row>>();
        var reads = 0;
        var button = Render<OmniMarkdownExportButton<Row>>(parameters => parameters.Add(component => component.Export, () => new OmniMarkdownTableExport<Row>
        {
            Title = "t",
            Columns = [new("Nom", row => row.Name)],
            LoadPage = _ =>
            {
                reads++;
                return pending.Task;
            }
        }));

        button.Find("button").Click();
        button.Find("button").Click();
        pending.SetResult(new OmniDataGridResult<Row>([], 0));
        button.WaitForAssertion(() => Assert.Equal(["download"], runtime.Module.Calls));
        await button.Instance.DisposeAsync();

        Assert.Equal(1, reads);
        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
    }

    [Fact]
    public void MarkdownEscape_OfNull_IsEmpty() =>
        Assert.Equal(string.Empty, OmniMarkdownTableExporter.EscapeInline(null));

    [Fact]
    public async Task Downloader_ReleasedOnALostCircuit_IsQuiet_AndAnUnknownCultureWritesInvariant()
    {
        var runtime = new ManualJSRuntime { Module = new RecordingModule { DisposeFailure = new JSDisconnectedException("perdu") } };
        var downloader = new OmniTableExportDownloader(runtime, Services.GetRequiredService<OmniTableExporter>());
        var document = new OmniTableExportDocument
        {
            Title = "t",
            Columns = [new OmniTableExportColumn("Montant", OmniTableExportValueKind.Number)],
            Rows = [[new OmniTableExportCell("1.5") { Number = 1.5m }]],
            Culture = "pas une culture !"
        };

        var file = await downloader.DownloadAsync(document, OmniTableExportFormat.Csv, "bilan", Xunit.TestContext.Current.CancellationToken);
        await downloader.DisposeAsync();

        Assert.Contains("1.5", System.Text.Encoding.UTF8.GetString(file.Content.ToArray()), StringComparison.Ordinal);
        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
    }

    // ---- tree -------------------------------------------------------------------------------------

    [Fact]
    public async Task TreeSelection_ClickedAgain_IsUnselected_AndALostCircuitAtDisposalIsQuiet()
    {
        var runtime = new ManualJSRuntime();
        runtime.Module.CallFailures["detachTreeDrag"] = new JSDisconnectedException("perdu");
        Services.AddSingleton<IJSRuntime>(runtime);
        var selections = new List<IReadOnlyList<string>>();
        var tree = Render<OmniTree<string>>(parameters => parameters
            .Add(component => component.Multiple, true)
            .Add(component => component.AllowDragDrop, true)
            .Add(component => component.ValueChanged, values => selections.Add(values))
            .AddChildContent<OmniTreeItem<string>>(item => item.Add(c => c.Value, "a").Add(c => c.Text, "A")));

        tree.Find(".omni-tree__select").Click();
        tree.Find(".omni-tree__select").Click();
        await tree.Instance.DisposeAsync();

        Assert.Equal([["a"], []], selections.Select(values => values.ToArray()));
        Assert.Equal(["attachTreeDrag", "detachTreeDrag"], runtime.Module.Calls);
    }
}
