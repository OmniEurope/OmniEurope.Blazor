using Bunit;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The light word processor of <see cref="OmniHtmlEditor"/>: the sheet, the document toolbar, the
/// status bar with its live counts, and the HTML and plain text exports.
/// </summary>
public sealed class HtmlEditorDocumentTests : OmniBunitContext
{
    private const string EditorModulePath = OmniModules.HtmlEditor;
    private const string DocumentModulePath = OmniModules.DocumentEditor;

    private const string Sample =
        "<h1>Rapport annuel</h1><p class=\"omni-align-center\">Un <span class=\"omni-font-size-large\">grand</span> résumé.</p>" +
        "<ul><li>Premier</li><li>Second</li></ul><ol><li>Un</li><li>Deux</li></ol>" +
        "<table><tbody><tr><td>A</td><td>B</td></tr><tr><td>C</td><td>D</td></tr></tbody></table>";

    [Fact]
    public void Document_IsAVisualEditorOnASheetWithTheDocumentToolbarAndLiveCounts()
    {
        var module = JSInterop.SetupModule(EditorModulePath);
        var value = "<p>Bonjour le monde</p><p>Deux mots</p>";

        var document = RenderDocument(value);

        Assert.NotNull(document.Find(".omni-document-editor.omni-document-editor--sheet .omni-html-editor__surface"));
        Assert.Equal("Traitement de texte", document.Find("section.omni-document-editor").GetAttribute("aria-label"));
        Assert.NotNull(document.Find("select[data-command=font-size]"));
        Assert.NotNull(document.Find("button[data-command=insert-table]"));
        Assert.Empty(document.FindAll("button[data-command=toggle-source]"));
        Assert.Equal("<p>Bonjour le monde</p><p>Deux mots</p>", Assert.Single(module.Invocations["mount"]).Arguments[2]);
        Assert.Equal(5, document.Instance.WordCount);
        Assert.Equal("Mots : 5", document.Find("[data-statistic=words]").TextContent);
        Assert.Equal("Caractères : 25", document.Find("[data-statistic=characters]").TextContent);
    }

    [Fact]
    public void WithoutSheetOrStatusBar_TheEditorRendersAloneAsBefore()
    {
        JSInterop.SetupModule(EditorModulePath);
        var value = "<p>Bonjour</p>";

        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        Assert.Empty(editor.FindAll(".omni-document-editor"));
        Assert.Empty(editor.FindAll(".omni-document-editor__status"));
        Assert.Equal("SECTION", editor.Nodes.OfType<AngleSharp.Dom.IElement>().Single().TagName);
        Assert.Contains("omni-html-editor", editor.Nodes.OfType<AngleSharp.Dom.IElement>().Single().ClassList);
    }

    [Fact]
    public async Task Typing_UpdatesTheValueAndTheCounts()
    {
        JSInterop.SetupModule(EditorModulePath);
        var value = "<p>Un</p>";
        var document = RenderDocument(value, updated => value = updated);

        await document.InvokeAsync(() => new HtmlEditorInteropBridge(document.Instance).OnVisualInput("<p>Un deux trois</p>"));

        Assert.Equal("<p>Un deux trois</p>", value);
        document.WaitForAssertion(() => Assert.Equal("Mots : 3", document.Find("[data-statistic=words]").TextContent));
    }

    [Fact]
    public async Task ShortcutsOfTheSurface_UndoRedoAndOpenTheLink()
    {
        JSInterop.SetupModule(EditorModulePath);
        var value = "<p>Un</p>";
        var document = RenderDocument(value, updated => value = updated);
        var bridge = new HtmlEditorInteropBridge(document.Instance);
        await document.InvokeAsync(() => bridge.OnVisualInput("<p>Un deux</p>"));

        await document.InvokeAsync(() => bridge.OnHistoryShortcut(false));
        Assert.Equal("<p>Un</p>", value);
        await document.InvokeAsync(() => bridge.OnHistoryShortcut(true));
        Assert.Equal("<p>Un deux</p>", value);

        await document.InvokeAsync(bridge.OnLinkShortcut);
        document.WaitForAssertion(() => Assert.NotEmpty(document.FindAll(".omni-html-editor__link")));
    }

    [Fact]
    public async Task ExportHtml_IsAStandaloneFile_WithTheClassesTurnedIntoDeclarations()
    {
        JSInterop.SetupModule(EditorModulePath);
        var document = RenderDocument(Sample);

        var html = await document.InvokeAsync(() => document.Instance.ExportHtmlAsync());

        Assert.StartsWith("<!DOCTYPE html>", html, StringComparison.Ordinal);
        Assert.Contains("<html lang=\"fr-FR\">", html, StringComparison.Ordinal);
        Assert.Contains("<title>Rapport annuel</title>", html, StringComparison.Ordinal);
        Assert.Contains("<p style=\"text-align: center\">", html, StringComparison.Ordinal);
        Assert.Contains("<span style=\"font-size: 1.3em\">grand</span>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("omni-", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExportText_ReadsLikeTheDocument()
    {
        JSInterop.SetupModule(EditorModulePath);
        var document = RenderDocument(Sample);

        var text = await document.InvokeAsync(() => document.Instance.ExportTextAsync());

        Assert.Equal("Rapport annuel\n\nUn grand résumé.\n\n- Premier\n- Second\n\n1. Un\n2. Deux\n\nA\tB\nC\tD", text);
    }

    [Fact]
    public void ExportButtons_HandTheFilesToTheBrowser()
    {
        JSInterop.SetupModule(EditorModulePath);
        var download = JSInterop.SetupModule(DocumentModulePath);
        download.SetupVoid("download", _ => true).SetVoidResult();
        var value = "<p>Bonjour</p>";
        var document = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ShowStatusBar, true)
            .Add(component => component.FileName, "compte-rendu")
            .Add(component => component.DocumentTitle, "Compte rendu"));

        document.Find("[data-export=html]").Click();
        document.Find("[data-export=text]").Click();

        var files = download.Invocations["download"];
        Assert.Equal(2, files.Count);
        Assert.Equal("compte-rendu.html", files[0].Arguments[0]);
        Assert.Equal("text/html;charset=utf-8", files[0].Arguments[1]);
        Assert.Contains("<title>Compte rendu</title>", (string)files[0].Arguments[2]!, StringComparison.Ordinal);
        Assert.Equal("compte-rendu.txt", files[1].Arguments[0]);
        Assert.Equal("Bonjour", files[1].Arguments[2]);
    }

    [Fact]
    public void Disabled_LocksThePage_AndTheStatusBarCanBeHidden()
    {
        JSInterop.SetupModule(EditorModulePath);
        var value = "<p>Bonjour</p>";

        var document = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Sheet, true)
            .Add(component => component.Commands, OmniHtmlEditorCommands.Document)
            .Add(component => component.Disabled, true));

        Assert.Equal("false", document.Find(".omni-html-editor__surface").GetAttribute("contenteditable"));
        Assert.NotNull(document.Find(".omni-document-editor--sheet"));
        Assert.Empty(document.FindAll(".omni-document-editor__status"));
        Assert.Empty(document.FindAll("[data-export]"));
    }

    [Theory]
    [InlineData("", 0, 0)]
    [InlineData("<p>L'été, c'est 2026 !</p>", 3, 19)]
    [InlineData("<p>Un</p><p>deux</p>", 2, 6)]
    public void Counts_AreWordsAndCharactersOfTheText(string html, int words, int characters) =>
        Assert.Equal((words, characters), OmniHtmlText.Count(html));

    /// <summary>The editor as the former document editor rendered it: sheet, document toolbar, status bar.</summary>
    private IRenderedComponent<OmniHtmlEditor> RenderDocument(string value, Action<string>? changed = null) =>
        Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, changed ?? (_ => { }))
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Sheet, true)
            .Add(component => component.ShowStatusBar, true)
            .Add(component => component.Commands, OmniHtmlEditorCommands.Document));
}
