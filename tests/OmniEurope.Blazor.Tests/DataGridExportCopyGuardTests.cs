using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OmniEurope.Blazor.Components;
using Row = OmniEurope.Blazor.Tests.DataGridExportTestHost.Row;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The copy of the Markdown export when it is asked for in a state the buttons never offer it: while another copy
/// or an export runs, when no copy is offered, and when the exporter is gone once the copy was offered. It then does nothing: no
/// document is built, nothing reaches the clipboard, no error is raised.
/// </summary>
public sealed class DataGridExportCopyGuardTests : OmniBunitContext
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    private static readonly Row[] Rows =
    [
        DataGridExportTestHost.Delta,
        DataGridExportTestHost.Alpha
    ];

    private readonly BunitJSModuleInterop _download;
    private readonly BunitJSModuleInterop _clipboard;

    public DataGridExportCopyGuardTests()
    {
        _download = JSInterop.SetupModule(OmniModules.DocumentEditor);
        _clipboard = JSInterop.SetupModule(OmniModules.Interop);
    }

    private IRenderedComponent<DataGridExportTestHost> RenderGrid(int copyMaxRows = 10) => Render<DataGridExportTestHost>(parameters => parameters
        .Add(component => component.Items, Rows)
        .Add(component => component.CopyMaxRows, copyMaxRows)
        .Add(component => component.Formats, [OmniTableExportFormat.Markdown]));

    private static GridExport<Row> ExportOf(IRenderedComponent<DataGridExportTestHost> host) =>
        host.FindComponent<OmniDataGrid<Row>>().Instance.Export;

    [Fact]
    public async Task ASecondCopy_WhileTheFirstRuns_DoesNothing_AndTheFirstStillFinishes()
    {
        var accepted = _clipboard.Setup<bool>("copyText", _ => true);
        var host = RenderGrid();
        var export = ExportOf(host);
        var first = host.InvokeAsync(export.CopyAsync);
        host.WaitForAssertion(() => Assert.Single(_clipboard.Invocations["copyText"]));
        Assert.True(export.Copying);

        await host.InvokeAsync(export.CopyAsync);

        Assert.Single(_clipboard.Invocations["copyText"]);
        Assert.True(export.Copying);
        Assert.False(first.IsCompleted);

        accepted.SetResult(true);
        await first.WaitAsync(Limit, Xunit.TestContext.Current.CancellationToken);
        Assert.False(export.Copying);
        Assert.True(export.CopyResult);
        Assert.Single(_clipboard.Invocations["copyText"]);
    }

    [Fact]
    public async Task ACopy_WhileAnExportRuns_DoesNothing_AndTheExportStillDelivers()
    {
        var download = _download.SetupVoid("download", _ => true);
        var host = RenderGrid();
        var export = ExportOf(host);
        var running = host.InvokeAsync(() => export.RunAsync(OmniTableExportFormat.Markdown));
        host.WaitForAssertion(() => Assert.Single(_download.Invocations["download"]));
        Assert.Equal(OmniTableExportFormat.Markdown, export.Running);

        await host.InvokeAsync(export.CopyAsync);

        Assert.Empty(_clipboard.Invocations);
        Assert.False(export.Copying);
        Assert.Null(export.CopyResult);
        Assert.Equal(OmniTableExportFormat.Markdown, export.Running);

        download.SetVoidResult();
        await running.WaitAsync(Limit, Xunit.TestContext.Current.CancellationToken);
        Assert.Null(export.Running);
        Assert.Equal(2, host.Instance.Exported!.RowCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task ACopy_WhereNoneIsOffered_DoesNothing(int copyMaxRows)
    {
        var host = RenderGrid(copyMaxRows);
        var export = ExportOf(host);
        Assert.False(export.CopyOffered);
        Assert.Empty(host.FindAll(".omni-data-grid__export-copy"));

        await host.InvokeAsync(export.CopyAsync);

        Assert.Empty(_clipboard.Invocations);
        Assert.False(export.Copying);
        Assert.Null(export.CopyResult);
        Assert.Null(host.Instance.Failure);
        Assert.Empty(host.FindAll(".omni-data-grid__export-message--error"));
    }

    [Fact]
    public async Task ACopy_WhoseExporterIsGoneOnceTheCopyWasOffered_DoesNothing()
    {
        // The exporter is looked up at each use: this one answers until the test arms it, then once more
        // (the offer is still made) and never again (the copy finds none).
        var lookups = 0;
        var vanishAfter = int.MaxValue;
        Services.RemoveAll<OmniTableExporter>();
        Services.AddTransient<OmniTableExporter>(provider => Interlocked.Increment(ref lookups) > Volatile.Read(ref vanishAfter)
            ? null!
            : new OmniTableExporter(
                provider.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<OmniEurope.Blazor.Resources.AppStrings>>(),
                TimeProvider.System,
                provider.GetServices<IOmniTableExportRenderer>()));
        var host = RenderGrid();
        var export = ExportOf(host);
        Assert.True(export.CopyOffered);
        Volatile.Write(ref vanishAfter, Volatile.Read(ref lookups) + 1);

        await host.InvokeAsync(export.CopyAsync);

        Assert.True(Volatile.Read(ref lookups) > Volatile.Read(ref vanishAfter));
        Assert.Empty(_clipboard.Invocations);
        Assert.False(export.Copying);
        Assert.Null(export.CopyResult);
        Assert.Null(host.Instance.Failure);
    }
}
