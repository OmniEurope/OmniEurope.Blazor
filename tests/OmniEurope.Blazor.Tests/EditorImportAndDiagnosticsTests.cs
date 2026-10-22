using System.Text;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The table import of the HTML editor (blank lines, reported truncation, bounded read), the code viewer
/// facing an invalid link pattern, and the logging of a proofreader that throws.
/// </summary>
public sealed class EditorImportAndDiagnosticsTests : OmniBunitContext
{
    private const string ModulePath = OmniModules.HtmlEditor;

    private readonly List<(LogLevel Level, string Message, Exception? Exception)> _logs = [];

    public EditorImportAndDiagnosticsTests() => Services.AddSingleton<ILoggerProvider>(new ListLoggerProvider(_logs));

    private async Task<IRenderedComponent<OmniHtmlEditor>> OpenTableImportAsync()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Mode = JSRuntimeMode.Loose;
        var value = "<p>A</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Commands, [OmniHtmlEditorCommands.ImportTable]));
        await editor.Find("[data-command=import-table]").ClickAsync(new MouseEventArgs());
        return editor;
    }

    private static string Rows(int count, string separator = "\n", string cells = "a{0},b{0}") =>
        string.Join(separator, Enumerable.Range(1, count).Select(index => string.Format(cells, index)));

    [Fact]
    public async Task Csv_WithBlankLinesBetweenRows_StillKeepsAHundredRealRows()
    {
        var editor = await OpenTableImportAsync();

        // 130 real rows, each followed by a blank line: the blank lines must not eat the limit.
        editor.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText(Rows(130, "\n\n"), "gaps.csv"));

        var summary = editor.WaitForElement(".omni-html-editor__panel-note[role=status]");
        Assert.Contains("100 lignes", summary.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Csv_BeyondTheLimit_SaysItWasTruncated()
    {
        var editor = await OpenTableImportAsync();

        editor.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText(Rows(150), "long.csv"));

        editor.WaitForElement(".omni-html-editor__table-preview");
        Assert.Contains(editor.FindAll(".omni-html-editor__panel-note[role=status]"), note => note.TextContent.Contains("dépasse les limites", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Csv_WithTooManyColumns_SaysItWasTruncated()
    {
        var editor = await OpenTableImportAsync();

        editor.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText(Rows(3, "\n", string.Join(',', Enumerable.Range(1, 60).Select(column => "c" + column))), "wide.csv"));

        editor.WaitForElement(".omni-html-editor__table-preview");
        Assert.Contains(editor.FindAll(".omni-html-editor__panel-note[role=status]"), note => note.TextContent.Contains("dépasse les limites", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Csv_WithinTheLimit_ReportsNoTruncation()
    {
        var editor = await OpenTableImportAsync();

        // A header and exactly a hundred rows is the limit, not beyond it.
        editor.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("Nom,Valeur\n" + Rows(100, "\n", "n{0},v{0}"), "exact.csv"));

        editor.WaitForElement(".omni-html-editor__table-preview");
        Assert.DoesNotContain(editor.FindAll(".omni-html-editor__panel-note[role=status]"), note => note.TextContent.Contains("dépasse les limites", StringComparison.Ordinal));
        Assert.Contains("100 lignes", editor.Find(".omni-html-editor__panel-note[role=status]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReaderTable_BeyondTheLimit_SaysItWasTruncated()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Mode = JSRuntimeMode.Loose;
        var extension = new ReaderExtension(new OmniHtmlEditorTableReader([".xlsx"], (_, _) =>
            Task.FromResult<IReadOnlyList<IReadOnlyList<string>>>([.. Enumerable.Range(1, 130).Select(index => (IReadOnlyList<string>)["a" + index, "b"])])));
        var value = "<p>A</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Commands, [OmniHtmlEditorCommands.ImportTable])
            .Add(component => component.Extensions, [extension]));
        await editor.Find("[data-command=import-table]").ClickAsync(new MouseEventArgs());

        editor.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary([1], "sheet.xlsx"));

        editor.WaitForElement(".omni-html-editor__table-preview");
        Assert.Contains(editor.FindAll(".omni-html-editor__panel-note[role=status]"), note => note.TextContent.Contains("dépasse les limites", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ReadText_OfAHugeFile_StopsAtTheBoundAndCutsAtALine()
    {
        var line = new string('x', 99) + "\n";
        var counting = new CountingStream(new MemoryStream(Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat(line, 60_000)))));

        var (text, truncated) = await HtmlEditorTableFile.ReadTextAsync(counting, Xunit.TestContext.Current.CancellationToken);

        Assert.True(truncated);
        Assert.True(counting.BytesRead <= HtmlEditorTableFile.MaxTextBytes + 81920, $"{counting.BytesRead} bytes read");
        Assert.EndsWith("\n", text, StringComparison.Ordinal);
        Assert.True(text.Length <= HtmlEditorTableFile.MaxTextBytes);
        Assert.All(text.Split('\n', StringSplitOptions.RemoveEmptyEntries), cell => Assert.Equal(99, cell.Length));
    }

    [Fact]
    public async Task ReadText_OfASmallFile_IsWholeAndNotTruncated()
    {
        var (text, truncated) = await HtmlEditorTableFile.ReadTextAsync(new MemoryStream(Encoding.UTF8.GetBytes("a,b\nc,d")), Xunit.TestContext.Current.CancellationToken);

        Assert.False(truncated);
        Assert.Equal("a,b\nc,d", text);
    }

    private sealed class CountingStream(Stream inner) : Stream
    {
        public long BytesRead { get; private set; }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var read = await inner.ReadAsync(buffer, cancellationToken);
            BytesRead += read;
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class ReaderExtension(OmniHtmlEditorTableReader reader) : OmniHtmlEditorExtension
    {
        public override IReadOnlyList<OmniHtmlEditorTableReader> TableReaders => [reader];
    }

    // ── OmniCodeViewer: an invalid link pattern ──

    [Fact]
    public void CodeViewer_InvalidLinkPattern_IsIgnored_AndTheOthersStillDrawn()
    {
        var viewer = Render<OmniCodeViewer>(parameters => parameters
            .Add(component => component.Code, "see Program.cs")
            .Add(component => component.Links,
            [
                new OmniCodeEditorLink("broken", "(unclosed"),
                new OmniCodeEditorLink("empty", ""),
                new OmniCodeEditorLink("file", "see (\\S+)")
            ]));

        Assert.Equal(["Program.cs"], viewer.FindAll(".omni-code-viewer__link").Select(link => link.TextContent));
    }

    // ── Proofreaders that throw are logged ──

    private sealed class ThrowingProofreader : OmniHtmlEditorProofreader
    {
        public override bool CanIgnoreAll => true;

        public override Task<IReadOnlyList<OmniHtmlEditorProofreadingIssue>> CheckAsync(
            IReadOnlyList<OmniHtmlEditorProofreadingText> texts, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("service injoignable");

        public override Task<IReadOnlyList<string>> SuggestAsync(
            OmniHtmlEditorProofreadingText text, OmniHtmlEditorProofreadingIssue issue, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("dictionnaire absent");

        public override Task IgnoreAllAsync(string passage, string? language) => throw new IOException("lecture seule");
    }

    private sealed class ProofreadingExtension(OmniHtmlEditorProofreader proofreader) : OmniHtmlEditorExtension
    {
        public override OmniHtmlEditorProofreader? Proofreader => proofreader;
    }

    private IRenderedComponent<OmniHtmlEditor> RenderProofreadingEditor()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Mode = JSRuntimeMode.Loose;
        var value = "<p>Some speling here.</p>";
        return Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Extensions, [new ProofreadingExtension(new ThrowingProofreader())]));
    }

    [Fact]
    public async Task ProofreaderThatThrowsWhileChecking_IsLoggedAsAWarning_AndTheCheckAnswersNothing()
    {
        var editor = RenderProofreadingEditor();

        var answer = await editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance).OnProofreadRequested(["Some speling here."], [null]));

        Assert.Null(answer);
        var entry = Assert.Single(_logs, log => log.Level == LogLevel.Warning);
        Assert.IsType<InvalidOperationException>(entry.Exception);
        Assert.Contains("ThrowingProofreader", entry.Message, StringComparison.Ordinal);
    }

    private const string Flagged = "{\"p\":0,\"text\":\"Some speling here.\",\"lang\":\"en\",\"s\":5,\"l\":7,\"k\":0,\"m\":null}";

    [Fact]
    public async Task ProofreaderThatThrowsWhileSuggesting_IsLogged_AndTheMenuStillOpens()
    {
        var editor = RenderProofreadingEditor();

        await editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance).OnContextMenu(40, 20, null, Flagged));

        var entry = Assert.Single(_logs, log => log.Level == LogLevel.Warning);
        Assert.Equal("dictionnaire absent", entry.Exception?.Message);
        Assert.NotEmpty(editor.FindAll("[data-proofreading=ignore-all]"));
    }

    [Fact]
    public async Task ProofreaderThatThrowsWhileRecording_IsLogged()
    {
        JSInterop.SetupModule(ModulePath).Mode = JSRuntimeMode.Loose;
        var editor = RenderProofreadingEditor();
        await editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance).OnContextMenu(40, 20, null, Flagged));
        _logs.Clear();

        await editor.Find("[role=menuitem][data-proofreading=ignore-all]").ClickAsync(new MouseEventArgs());

        var entry = Assert.Single(_logs, log => log.Level == LogLevel.Warning);
        Assert.Equal("lecture seule", entry.Exception?.Message);
    }

    [Fact]
    public async Task ProofreaderCancelledByTheEditorLeaving_IsNotLoggedAsAFailure()
    {
        JSInterop.SetupModule(ModulePath).Mode = JSRuntimeMode.Loose;
        var value = "<p>Some speling here.</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Extensions, [new ProofreadingExtension(new WaitingProofreader())]));

        var pending = editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance).OnProofreadRequested(["Some speling here."], [null]));
        await editor.Instance.DisposeAsync();

        Assert.Null(await pending.WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken));
        Assert.DoesNotContain(_logs, log => log.Level >= LogLevel.Warning);
    }

    /// <summary>Waits on the token it is given, as a proofreader on a slow service does.</summary>
    private sealed class WaitingProofreader : OmniHtmlEditorProofreader
    {
        public override async Task<IReadOnlyList<OmniHtmlEditorProofreadingIssue>> CheckAsync(
            IReadOnlyList<OmniHtmlEditorProofreadingText> texts, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return [];
        }
    }

    [Fact]
    public async Task MenuWithoutAPassage_AsksNothingAndRecordsNothing()
    {
        var editor = RenderProofreadingEditor();
        var proofreading = editor.Instance.Proofreading;

        await proofreading.LoadSuggestionsAsync();
        await proofreading.RecordAsync(dictionary: true);

        Assert.Null(proofreading.Issue);
        Assert.Empty(proofreading.Suggestions);
        Assert.DoesNotContain(_logs, log => log.Level >= LogLevel.Warning);
    }

    private sealed class SlowSuggestionProofreader : OmniHtmlEditorProofreader
    {
        public override Task<IReadOnlyList<OmniHtmlEditorProofreadingIssue>> CheckAsync(
            IReadOnlyList<OmniHtmlEditorProofreadingText> texts, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<OmniHtmlEditorProofreadingIssue>>([]);

        public override async Task<IReadOnlyList<string>> SuggestAsync(
            OmniHtmlEditorProofreadingText text, OmniHtmlEditorProofreadingIssue issue, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return [];
        }
    }

    [Fact]
    public async Task MenuClosedWhileCorrectionsLoad_DropsThem_WithoutLoggingAFailure()
    {
        JSInterop.SetupModule(ModulePath).Mode = JSRuntimeMode.Loose;
        var value = "<p>Some speling here.</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Extensions, [new ProofreadingExtension(new SlowSuggestionProofreader())]));
        var proofreading = editor.Instance.Proofreading;
        Assert.True(proofreading.Begin(Flagged));

        var loading = proofreading.LoadSuggestionsAsync();
        Assert.True(proofreading.Loading);
        proofreading.Cancel();
        await loading.WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);

        Assert.Empty(proofreading.Suggestions);
        Assert.DoesNotContain(_logs, log => log.Level >= LogLevel.Warning);
    }

    [Fact]
    public async Task WithoutALoggerInTheContainer_AFailingProofreaderStillDoesNotBreakTheEditor()
    {
        Services.RemoveAll(typeof(ILogger<>));
        var editor = RenderProofreadingEditor();

        var answer = await editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance).OnProofreadRequested(["Some speling here."], [null]));

        Assert.Null(answer);
    }

    private sealed class ListLoggerProvider(List<(LogLevel, string, Exception?)> logs) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new ListLogger(logs);

        public void Dispose()
        {
        }

        private sealed class ListLogger(List<(LogLevel, string, Exception?)> logs) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
                logs.Add((logLevel, formatter(state, exception), exception));
        }
    }
}
