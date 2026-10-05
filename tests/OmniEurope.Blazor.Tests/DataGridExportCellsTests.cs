using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;
using Row = OmniEurope.Blazor.Tests.DataGridExportTestHost.Row;

// The former export bar (ExportFormats) stays supported and under test.
#pragma warning disable CS0618

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The cells of an exported document: each CLR value typed for a sheet (number, date, boolean) or left a
/// text, the kind of each column, and the bar's edges (no exporter, positions, notices, disposal).
/// </summary>
public sealed class DataGridExportCellsTests : OmniBunitContext
{
    private readonly BunitJSModuleInterop _download;
    private readonly CultureInfo _culture = CultureInfo.CurrentCulture;

    public DataGridExportCellsTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        _download = JSInterop.SetupModule(OmniModules.DocumentEditor);
        _download.SetupVoid("download", _ => true).SetVoidResult();
    }

    protected override void Dispose(bool disposing)
    {
        CultureInfo.CurrentCulture = _culture;
        base.Dispose(disposing);
    }

    public enum Level { Low, High }

    public sealed record Typed(
        bool Flag, DateTimeOffset At, DateTime Local, DateTime Unspecified, DateOnly Day,
        int Count, long Big, decimal Price, float Ratio, double Measure, double NotANumber, double Huge,
        Level Level, string Note, object? Mixed,
        bool? NoFlag, DateTime? NoTime, int? NoCount, Level? NoLevel, DateTimeOffset? NoAt, DateOnly? NoDay, string? NoNote);

    private static readonly DateTime LocalTime = new(2026, 10, 4, 9, 30, 0, DateTimeKind.Local);

    private static readonly Typed[] Rows =
    [
        new(true, new DateTimeOffset(2026, 10, 4, 9, 30, 0, TimeSpan.FromHours(2)), LocalTime, new DateTime(2026, 10, 4, 9, 30, 0),
            new DateOnly(2026, 10, 4), 3, 9_000_000_000L, 12.5m, 0.25f, 1.5d, double.NaN, 1e300, Level.High, "texte", 4,
            null, null, null, null, null, null, null),
        new(false, DateTimeOffset.UnixEpoch, LocalTime, new DateTime(2026, 1, 1), new DateOnly(2026, 1, 1), 1, 2L, 3m, 1f, 2d,
            double.PositiveInfinity, -1e300, Level.Low, "autre", "mot",
            null, null, null, null, null, null, null)
    ];

    private async Task<OmniTableExportDocument> ExportAsync()
    {
        OmniTableExportDocument? exported = null;
        RenderFragment columns = builder =>
        {
            var sequence = 0;
            foreach (var property in typeof(Typed).GetProperties())
            {
                builder.OpenComponent<OmniDataGridColumn<Typed>>(sequence++);
                builder.AddComponentParameter(sequence++, nameof(OmniDataGridColumn<Typed>.Property), property.Name);
                builder.AddComponentParameter(sequence++, nameof(OmniDataGridColumn<Typed>.Title), property.Name);
                builder.CloseComponent();
            }

            // A column whose export reads nothing: its kind cannot come from a property.
            builder.OpenComponent<OmniDataGridColumn<Typed>>(sequence++);
            builder.AddComponentParameter(sequence++, nameof(OmniDataGridColumn<Typed>.Title), "Vide");
            builder.AddComponentParameter(sequence++, nameof(OmniDataGridColumn<Typed>.ExportValue), (Func<Typed, object?>)(_ => null));
            builder.CloseComponent();
        };

        var grid = Render<OmniDataGrid<Typed>>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.Columns, columns)
            .Add(component => component.ExportFormats, [OmniTableExportFormat.Csv])
            .Add(component => component.OnExport, document => exported = document));

        await grid.Find(".omni-data-grid__export-button").ClickAsync(new());
        return exported!;
    }

    private static OmniTableExportCell Cell(OmniTableExportDocument document, int row, string column) =>
        document.Rows[row][document.Columns.Select(entry => entry.Title).ToList().IndexOf(column)];

    private static OmniTableExportValueKind Kind(OmniTableExportDocument document, string column) =>
        document.Columns.Single(entry => entry.Title == column).Kind;

    [Fact]
    public async Task Values_AreTypedForASheet_OrKeptAsText()
    {
        var document = await ExportAsync();

        Assert.True(Cell(document, 0, "Flag").Boolean);
        Assert.Equal(new DateTimeOffset(2026, 10, 4, 9, 30, 0, TimeSpan.FromHours(2)), Cell(document, 0, "At").Date);
        Assert.Equal(new DateTimeOffset(LocalTime), Cell(document, 0, "Local").Date);
        // An unspecified time is kept as written, read as UTC.
        Assert.Equal(new DateTimeOffset(2026, 10, 4, 9, 30, 0, TimeSpan.Zero), Cell(document, 0, "Unspecified").Date);
        Assert.Equal(new DateTimeOffset(2026, 10, 4, 0, 0, 0, TimeSpan.Zero), Cell(document, 0, "Day").Date);
        Assert.Equal(3m, Cell(document, 0, "Count").Number);
        Assert.Equal(9_000_000_000m, Cell(document, 0, "Big").Number);
        Assert.Equal(12.5m, Cell(document, 0, "Price").Number);
        Assert.Equal(0.25m, Cell(document, 0, "Ratio").Number);
        Assert.Equal(1.5m, Cell(document, 0, "Measure").Number);
        Assert.Null(Cell(document, 0, "NotANumber").Number);
        Assert.Equal("NaN", Cell(document, 0, "NotANumber").Text);
        Assert.Null(Cell(document, 0, "Huge").Number);
        Assert.Null(Cell(document, 1, "Huge").Number);
        Assert.Null(Cell(document, 0, "Level").Number);
        Assert.Equal("High", Cell(document, 0, "Level").Text);
    }

    [Fact]
    public async Task Columns_TakeTheKindTheirCellsShare_OrTheirPropertyType_OrText()
    {
        var document = await ExportAsync();

        Assert.Equal(OmniTableExportValueKind.Boolean, Kind(document, "Flag"));
        Assert.Equal(OmniTableExportValueKind.Date, Kind(document, "At"));
        Assert.Equal(OmniTableExportValueKind.Number, Kind(document, "Count"));
        // Huge mixes text (out of range) and text: one kind. NotANumber holds NaN and Infinity, texts.
        Assert.Equal(OmniTableExportValueKind.Text, Kind(document, "NotANumber"));
        // Mixed holds a number and a word: two kinds, so text.
        Assert.Equal(OmniTableExportValueKind.Text, Kind(document, "Mixed"));

        // Columns with no filled cell take the kind of the property they read.
        Assert.Equal(OmniTableExportValueKind.Boolean, Kind(document, "NoFlag"));
        Assert.Equal(OmniTableExportValueKind.Date, Kind(document, "NoTime"));
        Assert.Equal(OmniTableExportValueKind.Number, Kind(document, "NoCount"));
        Assert.Equal(OmniTableExportValueKind.Text, Kind(document, "NoLevel"));
        Assert.Equal(OmniTableExportValueKind.Date, Kind(document, "NoAt"));
        Assert.Equal(OmniTableExportValueKind.Date, Kind(document, "NoDay"));
        Assert.Equal(OmniTableExportValueKind.Text, Kind(document, "NoNote"));
        // An export value with no filled cell has no property to read: text.
        Assert.Equal(OmniTableExportValueKind.Text, Kind(document, "Vide"));
    }

    [Fact]
    public void UnknownFormat_TakesTheDownloadIcon()
    {
        Assert.Equal(OmniIconName.Download, GridExport<Typed>.IconOf((OmniTableExportFormat)99));
    }

    [Fact]
    public void WithoutAnExporter_NoFormatIsOffered()
    {
        Services.RemoveAll<OmniTableExporter>();

        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Items, [new Row(1, "Alpha", 1m, new DateOnly(2026, 1, 1), true)])
            .Add(component => component.Formats, [OmniTableExportFormat.Csv]));

        Assert.Empty(host.FindAll(".omni-data-grid__export"));
    }

    [Theory]
    [InlineData(OmniDataGridPosition.Top, 1, 0)]
    [InlineData(OmniDataGridPosition.Bottom, 0, 1)]
    [InlineData(OmniDataGridPosition.TopAndBottom, 1, 1)]
    public void Bars_FollowThePosition(OmniDataGridPosition position, int above, int below)
    {
        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Items, [new Row(1, "Alpha", 1m, new DateOnly(2026, 1, 1), true)])
            .Add(component => component.Position, position)
            .Add(component => component.Formats, [OmniTableExportFormat.Csv]));

        var table = host.Find("table");
        var bars = host.FindAll(".omni-data-grid__export");
        Assert.Equal(above + below, bars.Count);
        Assert.Equal(above, bars.Count(bar => (bar.CompareDocumentPosition(table) & AngleSharp.Dom.DocumentPositions.Following) != 0));
    }

    [Fact]
    public async Task ASourceThatShrinks_DuringTheExport_SaysTheFileIsIncomplete()
    {
        // The source announces ten rows but its first page is short: no limit was reached.
        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Load, request => Task.FromResult(new OmniDataGridResult<Row>(
                request.Page == 1 ? [new Row(1, "a", 1m, new DateOnly(2026, 1, 1), true), new Row(2, "b", 1m, new DateOnly(2026, 1, 1), true)]
                : request.Page == 2 ? [new Row(3, "c", 1m, new DateOnly(2026, 1, 1), true)] : [], 10)))
            .Add(component => component.Formats, [OmniTableExportFormat.Csv]));
        host.WaitForAssertion(() => Assert.Equal(2, host.FindAll("tbody tr").Count));

        await host.Find(".omni-data-grid__export-button").ClickAsync(new());

        var document = host.Instance.Exported!;
        Assert.False(document.Truncated);
        Assert.False(document.IsComplete);
        Assert.Contains("2 lignes exportées sur 10", host.Find(".omni-data-grid__export-message").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CancelWithoutAnExport_DoesNothing_AndDisposeStopsARunningOne()
    {
        var gate = new TaskCompletionSource<OmniDataGridResult<Row>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Items, [new Row(1, "Alpha", 1m, new DateOnly(2026, 1, 1), true)])
            .Add(component => component.ExportLoad, request => gate.Task.WaitAsync(request.CancellationToken))
            .Add(component => component.Formats, [OmniTableExportFormat.Csv]));
        var grid = host.FindComponent<OmniDataGrid<Row>>().Instance;
        grid.Export.Cancel();
        Assert.Null(grid.Export.Running);

        var export = host.Find(".omni-data-grid__export-button").ClickAsync(new());
        Assert.Equal(OmniTableExportFormat.Csv, grid.Export.Running);
        await host.InvokeAsync(() => grid.DisposeAsync().AsTask());
        await export;

        Assert.Empty(_download.Invocations["download"]);
        Assert.Null(host.Instance.Exported);
        Assert.Null(host.Instance.Failure);
    }

    [Fact]
    public async Task ReleasingTheDownloadScript_OnALostCircuit_IsTaken()
    {
        var runtime = new ManualJSRuntime { Module = new RecordingModule { DisposeFailure = new JSDisconnectedException("perdu") } };
        Services.AddSingleton<IJSRuntime>(runtime);
        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Items, [new Row(1, "Alpha", 1m, new DateOnly(2026, 1, 1), true)])
            .Add(component => component.Formats, [OmniTableExportFormat.Csv]));
        await host.Find(".omni-data-grid__export-button").ClickAsync(new());
        Assert.Contains("download", runtime.Module.Calls);

        await host.FindComponent<OmniDataGrid<Row>>().Instance.DisposeAsync();

        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
    }

    [Fact]
    public async Task AValueWithoutText_IsAnEmptyCell()
    {
        OmniTableExportDocument? exported = null;
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<OmniDataGridColumn<Typed>>(0);
            builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Typed>.Title), "Muet");
            builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Typed>.ExportValue), (Func<Typed, object?>)(_ => new Silent()));
            builder.CloseComponent();
        };
        var grid = Render<OmniDataGrid<Typed>>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.Columns, columns)
            .Add(component => component.ExportFormats, [OmniTableExportFormat.Csv])
            .Add(component => component.OnExport, document => exported = document));

        await grid.Find(".omni-data-grid__export-button").ClickAsync(new());

        Assert.Equal(string.Empty, exported!.Rows[0][0].Text);
    }

    // An object whose text is null: Convert.ToString then answers null.
    private sealed class Silent
    {
        public override string? ToString() => null;
    }
}
