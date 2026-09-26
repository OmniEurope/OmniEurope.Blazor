using System.Globalization;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The Markdown table export: every announced row read through the page provider (not the page on
/// screen), a header that says what the rows answer to and whether the table holds all of them, and
/// cells that stay on their table row.
/// </summary>
public sealed class MarkdownTableExportTests : OmniBunitContext
{
    private const string DownloadModulePath = "./_content/OmniEurope.Blazor/omni-document-editor.js";
    private static readonly DateTimeOffset Now = new(2026, 9, 26, 14, 5, 9, TimeSpan.Zero);

    private sealed record Row(int Id, string Name, string? Note = null);

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    public MarkdownTableExportTests() => Services.AddSingleton<TimeProvider>(new FixedClock());

    private OmniMarkdownTableExporter Exporter => Services.GetRequiredService<OmniMarkdownTableExporter>();

    /// <summary>A source of <paramref name="count"/> rows served by pages, recording every page asked for.</summary>
    private static OmniMarkdownTableExport<Row> Export(int count, List<OmniMarkdownTablePageRequest> requests,
        int rowLimit = 5000, int pageSize = 200, int? announced = null) => new()
    {
        Title = "Erreurs de Boutique",
        Columns = [new("Id", row => row.Id.ToString(CultureInfo.InvariantCulture)), new("Nom", row => row.Name), new("Note", row => row.Note)],
        Fields = [new("Période", "dernières 24 heures"), new("Filtres", "aucun")],
        RowLimit = rowLimit,
        PageSize = pageSize,
        LoadPage = request =>
        {
            requests.Add(request);
            var items = Enumerable.Range(request.Skip + 1, Math.Max(0, Math.Min(request.PageSize, count - request.Skip)))
                .Select(id => new Row(id, $"ligne {id}"))
                .ToList();
            return Task.FromResult(new OmniDataGridResult<Row>(items, announced ?? count));
        }
    };

    private static int TableRows(string markdown) =>
        markdown.Split('\n').Count(line => line.StartsWith("| ", StringComparison.Ordinal)
            && !line.StartsWith("| Id", StringComparison.Ordinal) && !line.StartsWith("| ---", StringComparison.Ordinal));

    [Fact]
    public async Task Export_ReadsEveryAnnouncedPage_AndWritesTheHeader()
    {
        var requests = new List<OmniMarkdownTablePageRequest>();

        var document = await Exporter.ExportAsync(Export(450, requests), Xunit.TestContext.Current.CancellationToken);

        Assert.Equal([1, 2, 3], requests.Select(request => request.Page));
        Assert.All(requests, request => Assert.Equal(200, request.PageSize));
        Assert.Equal(450, document.RowCount);
        Assert.Equal(450, document.TotalCount);
        Assert.True(document.IsComplete);
        Assert.False(document.Truncated);
        Assert.Equal(Now, document.GeneratedAt);
        var markdown = document.Markdown;
        Assert.StartsWith("# Erreurs de Boutique\n", markdown.ReplaceLineEndings("\n"), StringComparison.Ordinal);
        Assert.Contains("- Période : dernières 24 heures", markdown, StringComparison.Ordinal);
        Assert.Contains("- Filtres : aucun", markdown, StringComparison.Ordinal);
        Assert.Contains("- Limite : 5 000 lignes au plus", markdown, StringComparison.Ordinal);
        Assert.Contains("- Généré le (UTC) : 2026-09-26T14:05:09Z", markdown, StringComparison.Ordinal);
        Assert.Contains("- Lignes exportées : 450 sur 450 annoncées", markdown, StringComparison.Ordinal);
        Assert.DoesNotContain("> ", markdown, StringComparison.Ordinal);
        Assert.Contains("| Id | Nom | Note |", markdown, StringComparison.Ordinal);
        Assert.Contains("| --- | --- | --- |", markdown, StringComparison.Ordinal);
        Assert.Contains("| 1 | ligne 1 |  |", markdown, StringComparison.Ordinal);
        Assert.Contains("| 450 | ligne 450 |  |", markdown, StringComparison.Ordinal);
        Assert.Equal(450, TableRows(markdown));
    }

    [Fact]
    public async Task Export_BeyondTheLimit_StopsAtIt_AndSaysItIsTruncated()
    {
        var requests = new List<OmniMarkdownTablePageRequest>();

        var document = await Exporter.ExportAsync(Export(12, requests, rowLimit: 5, pageSize: 2), Xunit.TestContext.Current.CancellationToken);

        Assert.Equal([1, 2, 3], requests.Select(request => request.Page));
        Assert.Equal(5, document.RowCount);
        Assert.Equal(12, document.TotalCount);
        Assert.True(document.Truncated);
        Assert.False(document.IsComplete);
        Assert.Contains("- Lignes exportées : 5 sur 12 annoncées", document.Markdown, StringComparison.Ordinal);
        Assert.Contains("> Export tronqué : 5 lignes exportées sur 12, la limite de 5 lignes est atteinte.", document.Markdown, StringComparison.Ordinal);
        Assert.Equal(5, TableRows(document.Markdown));
    }

    [Fact]
    public async Task Export_FewerRowsThanAnnouncedUnderTheLimit_SaysTheDataChanged()
    {
        var requests = new List<OmniMarkdownTablePageRequest>();

        var document = await Exporter.ExportAsync(Export(3, requests, announced: 4), Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(3, document.RowCount);
        Assert.False(document.Truncated);
        Assert.Contains("> Export incomplet : 3 lignes exportées sur 4 annoncées", document.Markdown, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Export_WithARowKey_WritesARowReadTwiceOnce()
    {
        var export = Export(0, []) with
        {
            PageSize = 2,
            RowKey = row => row.Id,
            // Data that grows while it is read: the second page repeats row 2.
            LoadPage = request => Task.FromResult(new OmniDataGridResult<Row>(
                request.Page switch
                {
                    1 => [new(1, "a"), new(2, "b")],
                    2 => [new(2, "b"), new(3, "c")],
                    _ => []
                }, 4))
        };

        var document = await Exporter.ExportAsync(export, Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(3, TableRows(document.Markdown));
        Assert.Contains("> Export incomplet : 3 lignes exportées sur 4 annoncées", document.Markdown, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Export_APageThatFails_ProducesNoDocument()
    {
        var export = Export(0, []) with
        {
            PageSize = 2,
            LoadPage = request => request.Page == 1
                ? Task.FromResult(new OmniDataGridResult<Row>([new(1, "a"), new(2, "b")], 4))
                : throw new HttpRequestException("page 2")
        };

        await Assert.ThrowsAsync<HttpRequestException>(() => Exporter.ExportAsync(export, Xunit.TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Build_EscapesPipesAndLineBreaks_AndKeepsTheHeaderOnOneLine()
    {
        var export = Export(0, []) with { Title = "Titre\nsur deux lignes", Fields = [new("Recherche", "a|b\r\nc")] };

        var markdown = Exporter.Build(export, [new Row(7, "x|y", "un\ndeux\rtrois\r\nquatre")], 1, Now);

        Assert.Contains("# Titre sur deux lignes", markdown, StringComparison.Ordinal);
        Assert.Contains("- Recherche : a|b c", markdown, StringComparison.Ordinal);
        Assert.Contains("| 7 | x\\|y | un<br>deux<br>trois<br>quatre |", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_WithoutRows_WritesTheHeaderAndTheEmptyText()
    {
        var markdown = Exporter.Build(Export(0, []), [], 0, Now);

        Assert.Contains("- Lignes exportées : 0 sur 0 annoncées", markdown, StringComparison.Ordinal);
        Assert.Contains("Aucune ligne.", markdown, StringComparison.Ordinal);
        Assert.DoesNotContain("| --- |", markdown, StringComparison.Ordinal);
        Assert.Contains("Rien à signaler.", Exporter.Build(Export(0, []) with { EmptyText = "Rien à signaler." }, [], 0, Now),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Export_RejectsAnExportWithoutColumns()
    {
        var export = Export(1, []) with { Columns = [] };

        await Assert.ThrowsAsync<ArgumentException>(() => Exporter.ExportAsync(export, Xunit.TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Button_DownloadsTheWholeExport_StampedWithTheGenerationTime()
    {
        var module = JSInterop.SetupModule(DownloadModulePath);
        module.SetupVoid("download", _ => true).SetVoidResult();
        var requests = new List<OmniMarkdownTablePageRequest>();
        OmniMarkdownTableDocument? exported = null;
        var button = Render<OmniMarkdownExportButton<Row>>(parameters => parameters
            .Add(component => component.Export, () => Export(250, requests))
            .Add(component => component.FileName, "erreurs-app3")
            .Add(component => component.OnExported, document => exported = document));

        Assert.Contains("Exporter en .md", button.Markup, StringComparison.Ordinal);
        button.Find("button").Click();

        var download = Assert.Single(module.Invocations["download"]);
        Assert.Equal("erreurs-app3-20260926-140509.md", download.Arguments[0]);
        Assert.Equal("text/markdown;charset=utf-8", download.Arguments[1]);
        Assert.Equal(250, TableRows((string)download.Arguments[2]!));
        Assert.Equal(2, requests.Count);
        Assert.NotNull(exported);
        Assert.Null(button.Find("button").GetAttribute("aria-busy"));
    }

    [Fact]
    public void Button_WhenAPageFails_DownloadsNothing_AndReportsTheError()
    {
        var module = JSInterop.SetupModule(DownloadModulePath);
        module.SetupVoid("download", _ => true).SetVoidResult();
        Exception? error = null;
        var button = Render<OmniMarkdownExportButton<Row>>(parameters => parameters
            .Add(component => component.Export, () => Export(0, []) with { LoadPage = _ => throw new HttpRequestException("down") })
            .Add(component => component.Text, "Exporter les erreurs")
            .Add(component => component.OnError, exception => error = exception));

        Assert.Contains("Exporter les erreurs", button.Markup, StringComparison.Ordinal);
        button.Find("button").Click();

        Assert.IsType<HttpRequestException>(error);
        Assert.Empty(module.Invocations["download"]);
        Assert.Null(button.Find("button").GetAttribute("aria-busy"));
    }

    [Fact]
    public void Button_WhileReading_IsBusyNotDisabled()
    {
        var pending = new TaskCompletionSource<OmniDataGridResult<Row>>();
        var button = Render<OmniMarkdownExportButton<Row>>(parameters => parameters
            .Add(component => component.Export, () => Export(0, []) with { LoadPage = _ => pending.Task }));

        button.Find("button").Click();

        var busy = button.Find("button");
        Assert.Equal("true", busy.GetAttribute("aria-busy"));
        Assert.Contains("omni-busy", busy.ClassList);
        Assert.False(busy.HasAttribute("disabled"));
        pending.SetResult(new OmniDataGridResult<Row>([], 0));
        button.WaitForAssertion(() => Assert.Null(button.Find("button").GetAttribute("aria-busy")));
    }
}
