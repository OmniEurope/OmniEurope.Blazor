using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Defects of the editor family found by the 2026-10-22 audit: unbounded history, counts and exports that
/// ignored the editor's sanitizer policy, a folded diff file drawn whole, and script modules imported after
/// the component left or on a lost circuit.
/// </summary>
public sealed class EditorDefectFixTests : OmniBunitContext
{
    private sealed class DetailsExtension : OmniHtmlEditorExtension
    {
        public override OmniHtmlSanitizerPolicy? SanitizerPolicy { get; } = new() { AdditionalTags = ["details", "summary"] };
    }

    private IRenderedComponent<OmniHtmlEditor> RenderEditor(string value, IReadOnlyList<OmniHtmlEditorExtension>? extensions = null)
    {
        JSInterop.SetupModule(OmniModules.HtmlEditor).Mode = JSRuntimeMode.Loose;
        return Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Extensions, extensions));
    }

    [Fact]
    public async Task UndoHistory_KeepsTheLatestHundredSteps_AndRedoReturnsToTheEnd()
    {
        var editor = RenderEditor("<p>0</p>");

        for (var step = 1; step <= 150; step++)
        {
            var html = $"<p>{step}</p>";
            await editor.InvokeAsync(() => editor.Instance.CommitAsync(html));
        }

        for (var step = 0; step < 150; step++)
        {
            await editor.InvokeAsync(editor.Instance.UndoAsync);
        }

        // 150 commits, 100 remembered: the oldest 50 steps are gone.
        Assert.Equal("<p>50</p>", editor.Instance.Value);

        for (var step = 0; step < 150; step++)
        {
            await editor.InvokeAsync(editor.Instance.RedoAsync);
        }

        Assert.Equal("<p>150</p>", editor.Instance.Value);
    }

    [Fact]
    public async Task Undo_UnderTheCap_StillReachesTheFirstValue()
    {
        var editor = RenderEditor("<p>0</p>");

        await editor.InvokeAsync(() => editor.Instance.CommitAsync("<p>1</p>"));
        await editor.InvokeAsync(() => editor.Instance.CommitAsync("<p>2</p>"));
        await editor.InvokeAsync(editor.Instance.UndoAsync);
        await editor.InvokeAsync(editor.Instance.UndoAsync);
        await editor.InvokeAsync(editor.Instance.UndoAsync);

        Assert.Equal("<p>0</p>", editor.Instance.Value);
    }

    private const string DetailsHtml = "<details><summary>Titre</summary> Contenu caché</details><p>Fin</p>";

    [Fact]
    public async Task Exports_AndCounts_UseTheEditorsPolicy_NotTheBuiltInList()
    {
        var editor = RenderEditor(DetailsHtml, [new DetailsExtension()]);

        var text = await editor.Instance.ExportTextAsync();
        var html = await editor.Instance.ExportHtmlAsync();

        Assert.Contains("Contenu caché", text, StringComparison.Ordinal);
        Assert.Contains("<details>", html, StringComparison.Ordinal);
        Assert.Equal(4, editor.Instance.WordCount);
    }

    [Fact]
    public async Task Exports_WithoutAPolicy_StillDropAnElementTheBuiltInListRefuses()
    {
        var editor = RenderEditor(DetailsHtml + "<script>alert(1)</script>");

        var text = await editor.Instance.ExportTextAsync();
        var html = await editor.Instance.ExportHtmlAsync();

        Assert.DoesNotContain("Contenu", text, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", html, StringComparison.Ordinal);
        Assert.Equal(1, editor.Instance.WordCount);
    }

    [Fact]
    public void AValueAlreadySanitised_IsNotSanitisedAgain_ForTheTextCounts()
    {
        const string html = "<p>un deux</p><script>trois quatre</script>";

        Assert.Equal((2, 7), OmniHtmlText.Count(html));
        Assert.Equal((2, 7), OmniHtmlText.Count(html, null, isSanitized: false));
        // The caller vouches for the value: nothing is sanitised, so the script text is read as it stands.
        Assert.Equal((4, 19), OmniHtmlText.Count(html, null, isSanitized: true));
    }

    private static string LongDiff() =>
        "diff --git a/a.txt b/a.txt\n--- a/a.txt\n+++ b/a.txt\n@@ -1,2 +1,2 @@\n garde\n-ancien\n+nouveau\n";

    [Fact]
    public void FoldedFile_DrawsNoBody_UntilItIsOpened()
    {
        var diff = Render<OmniUnifiedDiff>(parameters => parameters
            .Add(component => component.Diff, LongDiff())
            .Add(component => component.ExpandedByDefault, false));

        Assert.True(diff.Find(".omni-unified-diff__body").HasAttribute("hidden"));
        Assert.Empty(diff.FindAll(".omni-unified-diff__table"));
        Assert.Empty(diff.FindAll(".omni-unified-diff__code"));
        Assert.DoesNotContain("nouveau", diff.Markup, StringComparison.Ordinal);

        diff.Find(".omni-unified-diff__toggle").Click();

        Assert.False(diff.Find(".omni-unified-diff__body").HasAttribute("hidden"));
        Assert.Contains("nouveau", diff.Find(".omni-unified-diff__table").TextContent, StringComparison.Ordinal);

        diff.Find(".omni-unified-diff__toggle").Click();

        Assert.Empty(diff.FindAll(".omni-unified-diff__table"));
    }

    [Fact]
    public void FoldedFile_KeepsItsBodyElement_ForTheAriaControlsOfItsToggle()
    {
        var diff = Render<OmniUnifiedDiff>(parameters => parameters
            .Add(component => component.Diff, LongDiff())
            .Add(component => component.ExpandedByDefault, false)
            .Add(component => component.Id, "revue"));

        var controls = diff.Find(".omni-unified-diff__toggle").GetAttribute("aria-controls");

        Assert.Equal(controls, diff.Find(".omni-unified-diff__body").Id);
    }

    // ── Script modules: an import ending after the component left, and a lost circuit ──

    private static async Task<RecordingModule> HeldImportAsync(ManualJSRuntime runtime, Func<Task> dispose)
    {
        await dispose();
        runtime.PendingImport.SetResult(runtime.Module);
        await runtime.Module.Disposal.Task.WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);
        return runtime.Module;
    }

    [Fact]
    public async Task CodeEditor_GoneWhileItsHeightScriptLoads_ReleasesTheModuleOnArrival()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        Services.AddSingleton<IJSRuntime>(runtime);
        var value = "x";
        var editor = Render<OmniCodeEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Height, "20rem"));

        var module = await HeldImportAsync(runtime, () => editor.Instance.DisposeAsync().AsTask());

        Assert.Empty(module.Calls);
    }

    [Fact]
    public async Task CodeEditor_GoneWhileMonacoScriptLoads_ReleasesTheModuleOnArrival()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        Services.AddSingleton<IJSRuntime>(runtime);
        var value = "x";
        var editor = Render<OmniCodeEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        var module = await HeldImportAsync(runtime, () => editor.Instance.DisposeAsync().AsTask());

        Assert.Empty(module.Calls);
    }

    [Fact]
    public void CodeEditor_OnALostCircuit_RendersWithoutFailing()
    {
        Services.AddSingleton<IJSRuntime>(new ManualJSRuntime { ImportFailure = new JSDisconnectedException("perdu") });
        var value = "x";

        var editor = Render<OmniCodeEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Height, "20rem"));

        Assert.NotEmpty(editor.FindAll("textarea"));
    }

    [Fact]
    public async Task HtmlEditor_GoneWhileItsSurfaceScriptLoads_ReleasesTheModuleOnArrival()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        Services.AddSingleton<IJSRuntime>(runtime);
        var value = "<p>x</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        var module = await HeldImportAsync(runtime, () => editor.Instance.DisposeAsync().AsTask());

        Assert.DoesNotContain("mount", module.Calls);
    }

    [Fact]
    public void HtmlEditor_OnALostCircuit_RendersWithoutFailing()
    {
        Services.AddSingleton<IJSRuntime>(new ManualJSRuntime { ImportFailure = new JSDisconnectedException("perdu") });
        var value = "<p>x</p>";

        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        Assert.NotNull(editor.Find(".omni-html-editor"));
    }


    [Fact]
    public async Task MailTemplateEditor_GoneWhileItsPreviewScriptLoads_ReleasesTheModuleOnArrival()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        Services.AddSingleton<IJSRuntime>(runtime);
        var value = "<p>Bonjour</p>";
        var editor = Render<OmniMailTemplateEditor>(parameters => parameters
            .Add(component => component.Value, value));

        var module = await HeldImportAsync(runtime, () => editor.Instance.DisposeAsync().AsTask());

        Assert.DoesNotContain("attachTokens", module.Calls);
        Assert.DoesNotContain("render", module.Calls);
    }

    [Fact]
    public void MailTemplateEditor_OnALostCircuit_RendersWithoutFailing()
    {
        Services.AddSingleton<IJSRuntime>(new ManualJSRuntime { ImportFailure = new JSDisconnectedException("perdu") });
        var value = "<p>Bonjour</p>";

        var editor = Render<OmniMailTemplateEditor>(parameters => parameters
            .Add(component => component.Value, value));

        Assert.NotNull(editor.Markup);
    }

    [Fact]
    public async Task Chart_GoneWhileItsScriptLoads_ReleasesTheModuleOnArrival()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        Services.AddSingleton<IJSRuntime>(runtime);
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Label, "Ventes")
            .Add(component => component.MaxHeight, "20rem"));

        var module = await HeldImportAsync(runtime, () => chart.Instance.DisposeAsync().AsTask());

        Assert.Empty(module.Calls);
    }

    // ── Two imports of the same script in flight: the first to arrive is kept, the second is released ──

    /// <summary>A runtime whose imports each answer a module of their own, once the test lets them.</summary>
    private sealed class QueuedJSRuntime : IJSRuntime
    {
        public List<(RecordingModule Module, TaskCompletionSource Gate, string Path)> Imports { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier != "import")
            {
                return ValueTask.FromResult(default(TValue)!);
            }

            var import = (new RecordingModule(), new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously), args?[0]?.ToString() ?? string.Empty);
            Imports.Add(import);
            return new ValueTask<TValue>(import.Item2.Task.ContinueWith(_ => (TValue)(object)import.Item1, TaskScheduler.Default));
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => InvokeAsync<TValue>(identifier, args);
    }

    // How many of the modules were released, once all but one have been (or after about ten seconds).
    private static async Task<int> ReleasedAsync(IEnumerable<RecordingModule> modules)
    {
        var all = modules.ToList();
        for (var wait = 0; wait < 500 && all.Count(module => module.Disposal.Task.IsCompleted) < all.Count - 1; wait++)
        {
            await Task.Delay(20, Xunit.TestContext.Current.CancellationToken);
        }

        return all.Count(module => module.Disposal.Task.IsCompleted);
    }

    [Fact]
    public async Task CodeEditor_TwoImportsInFlight_KeepsTheFirstAndReleasesTheSecond()
    {
        var runtime = new QueuedJSRuntime();
        Services.AddSingleton<IJSRuntime>(runtime);
        var value = "x";
        var editor = Render<OmniCodeEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Height, "20rem"));
        editor.Render(parameters => parameters.Add(component => component.Height, "30rem"));
        Assert.Equal(2, runtime.Imports.Count(import => import.Path == OmniModules.CodeEditor));

        runtime.Imports[0].Gate.SetResult();
        runtime.Imports[1].Gate.SetResult();
        var codeEditor = runtime.Imports.Where(import => import.Path == OmniModules.CodeEditor).ToList();
        // Whichever arrives first is kept; the other is released.
        Assert.Equal(codeEditor.Count - 1, await ReleasedAsync(codeEditor.Select(import => import.Module)));
    }

    [Fact]
    public async Task MailTemplateEditor_TwoImportsInFlight_KeepsTheFirstAndReleasesTheSecond()
    {
        var runtime = new QueuedJSRuntime();
        Services.AddSingleton<IJSRuntime>(runtime);
        var editor = Render<OmniMailTemplateEditor>(parameters => parameters.Add(component => component.Value, "<p>Bonjour</p>"));
        editor.FindAll("[role=tab]")[1].Click();
        editor.Render(parameters => parameters.Add(component => component.Value, "<p>Bonsoir</p>"));
        var previews = runtime.Imports.Where(import => import.Path == OmniModules.MailPreview).ToList();
        Assert.True(previews.Count >= 2);

        foreach (var import in runtime.Imports)
        {
            import.Gate.SetResult();
        }

        // Whichever arrives first is kept; every other one is released.
        Assert.Equal(previews.Count - 1, await ReleasedAsync(previews.Select(import => import.Module)));
    }

    [Fact]
    public async Task CodeEditor_GoneWhileItsScriptLoads_OnALostCircuit_StillStopsQuietly()
    {
        var runtime = new ManualJSRuntime
        {
            HoldImports = true,
            Module = new RecordingModule { DisposeFailure = new JSDisconnectedException("perdu") }
        };
        Services.AddSingleton<IJSRuntime>(runtime);
        var value = "x";
        var editor = Render<OmniCodeEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Height, "20rem"));

        var module = await HeldImportAsync(runtime, () => editor.Instance.DisposeAsync().AsTask());

        Assert.Empty(module.Calls);
    }

    [Fact]
    public void HtmlEditor_WithNoValue_CountsNothing()
    {
        JSInterop.SetupModule(OmniModules.HtmlEditor).Mode = JSRuntimeMode.Loose;
        string? value = null;
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value!));

        Assert.Equal(0, editor.Instance.WordCount);
        Assert.Equal(0, editor.Instance.CharacterCount);
    }

    [Fact]
    public async Task MailTemplateEditor_RenderedAfterItLeft_DrawsNothingMore()
    {
        var preview = JSInterop.SetupModule(OmniModules.MailPreview);
        preview.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule(OmniModules.HtmlEditor).Mode = JSRuntimeMode.Loose;
        var editor = Render<OmniMailTemplateEditor>(parameters => parameters.Add(component => component.Value, "<p>Bonjour</p>"));
        editor.FindAll("[role=tab]")[1].Click();
        editor.WaitForAssertion(() => Assert.NotEmpty(preview.Invocations["render"]));
        var drawn = preview.Invocations["render"].Count;

        await editor.Instance.DisposeAsync();
        editor.Render(parameters => parameters.Add(component => component.Value, "<p>Bonsoir</p>"));

        Assert.Equal(drawn, preview.Invocations["render"].Count);
    }

    [Fact]
    public async Task HtmlEditor_GoneWhileADownloadScriptLoads_ReleasesItAndDownloadsNothing()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        Services.AddSingleton<IJSRuntime>(runtime);
        var value = "<p>x</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ShowStatusBar, true)
            .Add(component => component.Sheet, true));

        var click = editor.Find("[data-export=text]").ClickAsync(new MouseEventArgs());
        var module = await HeldImportAsync(runtime, () => editor.Instance.DisposeAsync().AsTask());
        await click.WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);

        Assert.DoesNotContain("download", module.Calls);
    }

    [Fact]
    public async Task Exports_OfAValueAlreadyCommitted_AreNotSanitisedAgain_AndNoValueExportsEmpty()
    {
        var editor = RenderEditor("<p>0</p>");
        await editor.InvokeAsync(() => editor.Instance.CommitAsync("<p>un <b>deux</b></p>"));

        var html = await editor.Instance.ExportHtmlAsync();
        var text = await editor.Instance.ExportTextAsync();

        Assert.Contains("<p>un <b>deux</b></p>", html, StringComparison.Ordinal);
        Assert.Equal("un deux", text.Trim());

        var empty = RenderEditor(string.Empty);
        Assert.Equal(string.Empty, (await empty.Instance.ExportTextAsync()).Trim());
        Assert.Contains("<body", await empty.Instance.ExportHtmlAsync(), StringComparison.Ordinal);
    }
}
