using System.Text;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;
using Row = OmniEurope.Blazor.Tests.DataGridExportTestHost.Row;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The copy button of a short export (<see cref="OmniDataGrid{TItem}.CopyMaxRows"/>): offered beside the
/// Markdown export when the rows are known and few, it puts on the clipboard the very Markdown document of
/// the <c>.md</c> file, without a download, and says how it went.
/// </summary>
public sealed class DataGridExportCopyTests : OmniBunitContext
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 9, 30, 0, TimeSpan.Zero);

    private static readonly Row[] Rows =
    [
        DataGridExportTestHost.Delta,
        DataGridExportTestHost.Alpha,
        new(3, "Bravo | \"cité\"", 0.5m, new DateOnly(2026, 9, 3), true),
        new(4, "Charlie", 7m, new DateOnly(2026, 9, 4), true),
        new(5, "Echo", 3m, new DateOnly(2026, 9, 5), false)
    ];

    private readonly BunitJSModuleInterop _download;
    private readonly BunitJSModuleInterop _clipboard;

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    public DataGridExportCopyTests()
    {
        Services.AddSingleton<TimeProvider>(new FixedClock());
        _download = JSInterop.SetupModule(Internal.OmniModules.DocumentEditor);
        _download.SetupVoid("download", _ => true).SetVoidResult();
        _clipboard = JSInterop.SetupModule(Internal.OmniModules.Interop);
    }

    private static AngleSharp.Dom.IElement? CopyButton(IRenderedComponent<DataGridExportTestHost> host) =>
        host.FindAll(".omni-data-grid__export-copy").SingleOrDefault();

    [Fact]
    public async Task AShortExport_CopiesTheMarkdownOfTheMdFile_WithoutADownload()
    {
        _clipboard.Setup<bool>("copyText", _ => true).SetResult(true);
        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.Formats, [OmniTableExportFormat.Markdown, OmniTableExportFormat.Csv]));

        var copy = CopyButton(host)!;
        Assert.Equal("Copier en Markdown", copy.GetAttribute("aria-label"));
        Assert.Equal("Copier en Markdown", copy.GetAttribute("title"));
        // After the format buttons, and not one of them: the formats keep their own class.
        Assert.Equal(["Exporter en Markdown", "Exporter en CSV"], host.FindAll(".omni-data-grid__export-button").Select(button => button.GetAttribute("aria-label")));

        await copy.ClickAsync(new());

        var copied = (string)Assert.Single(_clipboard.Invocations["copyText"]).Arguments[0]!;
        Assert.Empty(_download.Invocations["download"]);
        // A copy is no file: OnExport is left to the downloads.
        Assert.Null(host.Instance.Exported);
        await host.FindAll(".omni-data-grid__export-button").First(button => button.GetAttribute("aria-label") == "Exporter en Markdown").ClickAsync(new());
        var file = Encoding.UTF8.GetString((byte[])Assert.Single(_download.Invocations["download"]).Arguments[2]!);
        Assert.Equal(file, copied);
        Assert.All(["Delta", "Alpha", "Charlie", "Echo"], name => Assert.Contains(name, copied, StringComparison.Ordinal));
        Assert.Equal(5, host.Instance.Exported!.RowCount);
    }

    [Theory]
    [InlineData(true, "Copié")]
    [InlineData(false, "La copie a échoué")]
    public async Task TheButton_SaysHowTheCopyWent_AndAnnouncesIt(bool accepted, string outcome)
    {
        _clipboard.Setup<bool>("copyText", _ => true).SetResult(accepted);
        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.Formats, [OmniTableExportFormat.Markdown]));
        Assert.Equal(string.Empty, host.Find(".omni-data-grid__export .omni-visually-hidden[role='status']").TextContent);

        await CopyButton(host)!.ClickAsync(new());

        host.WaitForAssertion(() => Assert.Equal(outcome, CopyButton(host)!.GetAttribute("aria-label")));
        Assert.Equal(outcome, host.Find(".omni-data-grid__export .omni-visually-hidden[role='status']").TextContent);
        Assert.Empty(host.FindAll(".omni-data-grid__export-message--error"));
        Assert.Null(host.Instance.Failure);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(0)]
    [InlineData(-1)]
    public void MoreRowsThanTheThreshold_OrNoThreshold_OffersNoCopy(int threshold)
    {
        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.CopyMaxRows, threshold)
            .Add(component => component.Formats, [OmniTableExportFormat.Markdown]));

        Assert.Null(CopyButton(host));
        Assert.Single(host.FindAll(".omni-data-grid__export-button"));
    }

    [Fact]
    public void TheThreshold_CountsTheRowsTheFiltersKeep()
    {
        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.CopyMaxRows, 1)
            .Add(component => component.NameFilter, "Delta")
            .Add(component => component.Formats, [OmniTableExportFormat.Markdown]));

        Assert.NotNull(CopyButton(host));
    }

    [Fact]
    public void WithoutTheMarkdownExport_OrWithoutRows_ThereIsNothingToCopy()
    {
        var csvOnly = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.Formats, [OmniTableExportFormat.Csv]));
        var empty = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Items, Array.Empty<Row>())
            .Add(component => component.Formats, [OmniTableExportFormat.Markdown]));

        Assert.Null(CopyButton(csvOnly));
        Assert.Null(CopyButton(empty));
    }

    [Fact]
    public async Task ARemoteGrid_CopiesWhenItsTotalIsShort_AndNotWhenItAnnouncesNone()
    {
        _clipboard.Setup<bool>("copyText", _ => true).SetResult(true);
        var known = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Load, request => Task.FromResult(new OmniDataGridResult<Row>(Rows.Skip(request.Skip).Take(request.Top).ToArray(), Rows.Length)))
            .Add(component => component.Formats, [OmniTableExportFormat.Markdown]));
        var unknown = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Load, request => Task.FromResult(new OmniDataGridResult<Row>(Rows.Take(2).ToArray(), 0)))
            .Add(component => component.Formats, [OmniTableExportFormat.Markdown]));
        known.WaitForAssertion(() => Assert.Equal(2, known.FindAll("tbody tr").Count));
        unknown.WaitForAssertion(() => Assert.Equal(2, unknown.FindAll("tbody tr").Count));

        Assert.Null(CopyButton(unknown));
        await CopyButton(known)!.ClickAsync(new());

        // Every row the filters select, not the page of two on screen.
        var copied = (string)Assert.Single(_clipboard.Invocations["copyText"]).Arguments[0]!;
        Assert.All(Rows, row => Assert.Contains(row.Name.Replace("|", "\\|", StringComparison.Ordinal), copied, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ADocumentThatCannotBeBuilt_FailsLikeAnExport_AndCopiesNothing()
    {
        _clipboard.Setup<bool>("copyText", _ => true).SetResult(true);
        var host = Render<DataGridExportTestHost>(parameters => parameters
            .Add(component => component.Load, request => request.Skip == 0 && request.Top == 2
                ? Task.FromResult(new OmniDataGridResult<Row>(Rows.Take(2).ToArray(), Rows.Length))
                : Task.FromException<OmniDataGridResult<Row>>(new InvalidOperationException("source down")))
            .Add(component => component.Formats, [OmniTableExportFormat.Markdown]));
        host.WaitForAssertion(() => Assert.Equal(2, host.FindAll("tbody tr").Count));

        await CopyButton(host)!.ClickAsync(new());

        Assert.Empty(_clipboard.Invocations["copyText"]);
        Assert.Equal("source down", host.Instance.Failure!.Message);
        Assert.NotEmpty(host.FindAll(".omni-data-grid__export-message--error"));
    }
}
