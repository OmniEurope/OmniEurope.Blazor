using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// What the grid scripts can send to .NET and what the grid does with it: widths that are not finite, an
/// endless fit to content, a scope the browser refuses, a block size that changes, a column that registers
/// late, and the shared way a cell value becomes text.
/// </summary>
public sealed class DataGridRobustnessTests : OmniBunitContext
{
    private sealed record Row(int Id, string Name, int Amount);

    private static readonly Row[] Rows = [new(1, "Alice", 10), new(2, "Bob", 20)];

    private static RenderFragment Columns(string? formatString = null) => builder =>
    {
        builder.OpenComponent<OmniDataGridColumn<Row>>(0);
        builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Name));
        builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Title), "Nom");
        builder.CloseComponent();
        builder.OpenComponent<OmniDataGridColumn<Row>>(3);
        builder.AddComponentParameter(4, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Amount));
        builder.AddComponentParameter(5, nameof(OmniDataGridColumn<Row>.Title), "Montant");
        builder.AddComponentParameter(6, nameof(OmniDataGridColumn<Row>.FormatString), formatString);
        builder.CloseComponent();
    };

    // ---- widths reported by the script -------------------------------------------------------------

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public async Task AWidthThatIsNotFinite_IsNeitherAppliedNorAnnouncedNorSaved(double width)
    {
        var resized = new List<OmniDataGridColumnWidthChange>();
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.Columns, Columns())
            .Add(component => component.AllowColumnAutoFit, true)
            .Add(component => component.OnColumnResize, change => resized.Add(change)));

        await grid.InvokeAsync(() => grid.Instance.OnColumnResizedAsync("Name", width));
        await grid.InvokeAsync(() => grid.Instance.OnColumnAutoFitAsync("Name", width));

        Assert.Empty(resized);
    }

    [Fact]
    public async Task AHugeWidth_IsBoundedAndAGoodOneKept()
    {
        var resized = new List<OmniDataGridColumnWidthChange>();
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.Columns, Columns())
            .Add(component => component.OnColumnResize, change => resized.Add(change)));

        await grid.InvokeAsync(() => grid.Instance.OnColumnResizedAsync("Name", 1e12));
        await grid.InvokeAsync(() => grid.Instance.OnColumnResizedAsync("Name", 300d));

        Assert.Equal(["4000px", "300px"], resized.Select(change => change.Width));
    }

    // ---- the texts of a fit to content --------------------------------------------------------------

    [Fact]
    public void TheFitToContent_ReceivesAtMostTheCapOfDistinctTexts_KeepingTheLongest()
    {
        var rows = Enumerable.Range(0, GridColumnLayout<Row>.MaximumFitTexts + 1_000)
            .Select(index => new DataGridColumnAutoFitTests.Row(index, $"Ligne {index}"))
            .ToList();
        rows[5_500] = rows[5_500] with { Name = new string('x', 200) };
        var rendered = Render<DataGridColumnAutoFitTests.Host>(parameters => parameters
            .Add(host => host.GridAutoFit, true)
            .Add(host => host.AllowResize, true)
            .Add(host => host.Rows, rows)
            .Add(host => host.Virtualized, true));

        var texts = rendered.Instance.GetColumnAutoFitTexts("name");

        Assert.NotNull(texts);
        Assert.Equal(GridColumnLayout<Row>.MaximumFitTexts, texts.Length);
        Assert.Contains(new string('x', 200), texts);
    }

    [Fact]
    public void TheFitToContent_CutsAVeryLongTextToTheCap()
    {
        var rendered = Render<DataGridColumnAutoFitTests.Host>(parameters => parameters
            .Add(host => host.GridAutoFit, true)
            .Add(host => host.AllowResize, true)
            .Add(host => host.Rows, [new DataGridColumnAutoFitTests.Row(1, new string('y', 100_000))]));

        var texts = rendered.Instance.GetColumnAutoFitTexts("name");

        Assert.Equal(GridColumnLayout<Row>.MaximumFitTextLength, Assert.Single(texts!).Length);
    }

    // ---- WheelScrollScope ------------------------------------------------------------------------------

    [Fact]
    public async Task AWheelScopeTheBrowserRefuses_DoesNotFailEveryRender()
    {
        var module = JSInterop.SetupModule(OmniModules.Grid);
        module.Mode = JSRuntimeMode.Loose;
        module.SetupVoid("attachWheelScope", _ => true).SetException(new JSException("'!!' is not a valid selector"));

        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.Columns, Columns())
            .Add(component => component.WheelScrollScope, "!!"));
        await grid.InvokeAsync(() => { });
        grid.Render(parameters => parameters.Add(component => component.Caption, "Une légende"));
        grid.Render(parameters => parameters.Add(component => component.Caption, "Une autre"));
        await grid.InvokeAsync(() => { });

        Assert.Single(module.Invocations["attachWheelScope"]);
    }

    // ---- disposal -----------------------------------------------------------------------------------------

    [Fact]
    public async Task ADetachThatFails_DoesNotKeepTheOthersAttachedNorTheModuleLoaded()
    {
        var runtime = new ManualJSRuntime();
        runtime.Module.CallFailures["detachResize"] = new JSException("detach failed");
        Services.AddSingleton<IJSRuntime>(runtime);
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.Columns, Columns())
            .Add(component => component.AllowColumnResize, true)
            .Add(component => component.AllowColumnReorder, true));
        await grid.InvokeAsync(() => { });

        await grid.Instance.DisposeAsync();

        Assert.Contains("detachResize", runtime.Module.Calls);
        Assert.Contains("detachReorder", runtime.Module.Calls);
        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
    }

    // ---- a block size that changes -----------------------------------------------------------------------

    [Fact]
    public async Task AVirtualBlockSizeThatChanges_DropsTheBlocksCutForTheOldSize()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var requests = new List<OmniDataGridLoadRequest>();
        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.ScrollMode, OmniDataGridScrollMode.Virtual)
            .Add(component => component.EstimatedRowHeight, 40d)
            .Add(component => component.VirtualBlockSize, 50)
            .Add(component => component.Load, request =>
            {
                requests.Add(request);
                return Task.FromResult(new OmniDataGridResult<int>(Enumerable.Range(request.Skip, request.Top).ToArray(), 5_000));
            }));
        Assert.Equal(50, Assert.Single(requests).Top);

        grid.Render(parameters => parameters.Add(component => component.VirtualBlockSize, 100));
        await grid.InvokeAsync(() => grid.Instance.OnViewportChangedAsync(60 * 40d, 400d));

        // Block 0 of the new size covers rows 0 to 99: the 50 rows held under the old size are not it.
        Assert.Contains(requests, request => request.Top == 100 && request.Skip == 0);
        Assert.Contains(">60</td>", grid.Markup, StringComparison.Ordinal);
    }

    // ---- a column that registers late ---------------------------------------------------------------------

    [Fact]
    public async Task AColumnRemovedFromALoadGrid_ReloadsAndAFailingErrorHandlerReachesTheRenderer()
    {
        var loads = 0;
        var showSecond = true;
        RenderFragment Declared() => builder =>
        {
            builder.OpenComponent<OmniDataGridColumn<Row>>(0);
            builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Name));
            builder.CloseComponent();
            if (showSecond)
            {
                builder.OpenComponent<OmniDataGridColumn<Row>>(2);
                builder.AddComponentParameter(3, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Amount));
                builder.CloseComponent();
            }
        }
        ;
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Columns, Declared())
            .Add(component => component.OnLoadError, _ => throw new InvalidOperationException("handler failed"))
            .Add(component => component.Load, _ =>
            {
                loads++;
                return loads == 1
                    ? Task.FromResult(new OmniDataGridResult<Row>(Rows, 2))
                    : Task.FromException<OmniDataGridResult<Row>>(new InvalidOperationException("load failed"));
            }));
        Assert.Equal(1, loads);

        showSecond = false;
        var failure = await Record.ExceptionAsync(async () =>
        {
            grid.Render(parameters => parameters.Add(component => component.Columns, Declared()));
            await Task.Delay(200, Xunit.TestContext.Current.CancellationToken);
            grid.Render(parameters => parameters.Add(component => component.Caption, "Après"));
        });

        Assert.Equal(2, loads);
        Assert.NotNull(failure);
        Assert.Contains("handler failed", failure.ToString(), StringComparison.Ordinal);
    }

    // ---- one text for a cell -----------------------------------------------------------------------------

    [Fact]
    public async Task AMalformedFormatString_ShowsTheValueInTheCellAndInTheExport()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            OmniTableExportDocument? exported = null;
            JSInterop.SetupModule(OmniModules.DocumentEditor).SetupVoid("download", _ => true).SetVoidResult();
            var grid = Render<OmniDataGrid<Row>>(parameters => parameters
                .Add(component => component.Items, Rows)
                .Add(component => component.Columns, Columns("{0:Q}"))
                .Add(component => component.ShowFooterBar, true)
                .Add(component => component.ExportMarkdown, false)
                .Add(component => component.ExportExcel, false)
                .Add(component => component.ExportPdf, false)
                .Add(component => component.OnExport, document => exported = document));

            Assert.Contains(">10</td>", grid.Markup, StringComparison.Ordinal);
            await grid.Find(".omni-data-grid__export-button").ClickAsync(new());

            Assert.NotNull(exported);
            Assert.Equal(10m, exported.Rows[0][1].Number);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData("{0:n2} EUR", 1234.5, "1,234.50 EUR")]
    [InlineData("<{0}>", "a", "<a>")]
    [InlineData("{0:Q}", 12, "12")]
    [InlineData("{", 7, "7")]
    [InlineData("", 7, "7")]
    [InlineData(null, 7, "7")]
    [InlineData("{0}", null, "")]
    public void GridCellFormat_IsTheOneWayACellBecomesText(string? pattern, object? value, string expected)
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            Assert.Equal(expected, GridCellFormat.Text(value, pattern));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    // ---- the bridge the scripts call ---------------------------------------------------------------------

    [Fact]
    public async Task TheBridge_ForwardsEachCallbackOfTheScriptsToTheGrid()
    {
        var resized = new List<OmniDataGridColumnWidthChange>();
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.Columns, Columns())
            .Add(component => component.AllowColumnResize, true)
            .Add(component => component.AllowColumnReorder, true)
            .Add(component => component.AllowColumnAutoFit, true)
            .Add(component => component.OnColumnResize, change => resized.Add(change)));
        var bridge = new GridInteropBridge<Row>(grid.Instance);

        await grid.InvokeAsync(async () =>
        {
            await bridge.OnHorizontalScrollChangedAsync(true);
            await bridge.OnContentResizedAsync();
            await bridge.OnViewportChangedAsync(0d, 300d);
            await bridge.OnColumnAutoFitAsync("Name", 120d);
            await bridge.OnColumnResizedAsync("Name", 140d);
            await bridge.OnColumnDroppedAsync("Name", "Amount", true);
        });

        Assert.NotNull(bridge.GetColumnAutoFitTexts("Name"));
        Assert.Contains(resized, change => change.Width == "140px");
    }

    [Fact]
    public async Task TheListAndLogViewerBridges_ForwardTheViewportToTheirComponent()
    {
        var calls = new List<string>();
        await new ListInteropBridge((top, height) =>
        {
            calls.Add($"list {top} {height}");
            return Task.CompletedTask;
        }).OnViewportChangedAsync(1d, 2d);
        await new LogViewerInteropBridge((top, height, atBottom, following) =>
        {
            calls.Add($"log {top} {height} {atBottom} {following}");
            return Task.CompletedTask;
        }).OnViewportChangedAsync(3d, 4d, true, false);

        Assert.Equal(["list 1 2", "log 3 4 True False"], calls);
    }

    [Fact]
    public async Task ADetachOfTheViewportThatFails_DoesNotKeepTheModuleLoaded()
    {
        var runtime = new ManualJSRuntime();
        Services.AddSingleton<IJSRuntime>(runtime);
        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.ScrollMode, OmniDataGridScrollMode.Virtual)
            .Add(component => component.EstimatedRowHeight, 40d)
            .Add(component => component.Load, request =>
                Task.FromResult(new OmniDataGridResult<int>(Enumerable.Range(request.Skip, request.Top).ToArray(), 5_000))));
        await grid.InvokeAsync(() => { });
        Assert.Contains("attach", runtime.Module.Calls);
        runtime.Module.CallFailures["detach"] = new JSException("detach failed");

        await grid.Instance.DisposeAsync();

        Assert.Contains("detach", runtime.Module.Calls);
        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
    }
}
