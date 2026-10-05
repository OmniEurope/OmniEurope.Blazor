using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The visual surface of the HTML editor at its edges: what it answers before its script arrives, in the
/// source face or locked, what the caret split reads, and a script released on a lost circuit.
/// </summary>
public sealed class HtmlEditorVisualSurfaceTests : OmniBunitContext
{
    private const string Start = "<p>Bonjour</p>";
    private readonly string _bound = Start;

    private IRenderedComponent<OmniHtmlEditor> RenderEditor(Action<ComponentParameterCollectionBuilder<OmniHtmlEditor>>? extra = null) =>
        Render<OmniHtmlEditor>(parameters =>
        {
            parameters.Add(editor => editor.Value, Start).Add(editor => editor.ValueExpression, () => _bound);
            extra?.Invoke(parameters);
        });

    [Fact]
    public async Task AroundCaret_SplitsTheSanitisedDocument_AtTheCaret()
    {
        var module = JSInterop.SetupModule(OmniModules.HtmlEditor);
        module.Setup<string?>("aroundCaret", _ => true).SetResult("""{"before":"<p>Bon<script>x</script></p>","after":"<p>jour</p>"}""");
        var editor = RenderEditor();

        var split = await editor.InvokeAsync(editor.Instance.GetHtmlAroundCaretAsync);

        Assert.Equal("<p>Bon</p>", split.Before);
        Assert.Equal("<p>jour</p>", split.After);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task AroundCaret_WithoutAnAnswer_PutsTheWholeDocumentBeforeTheCaret(string? answer)
    {
        var module = JSInterop.SetupModule(OmniModules.HtmlEditor);
        module.Setup<string?>("aroundCaret", _ => true).SetResult(answer);
        var editor = RenderEditor();

        var split = await editor.InvokeAsync(editor.Instance.GetHtmlAroundCaretAsync);

        Assert.Equal((Start, string.Empty), (split.Before, split.After));
    }

    [Fact]
    public async Task InTheSourceFace_TheCaretSplitAndTheSelectionComeFromNoScript()
    {
        var module = JSInterop.SetupModule(OmniModules.HtmlEditor);
        var editor = RenderEditor(parameters => parameters.Add(editor => editor.Mode, OmniHtmlEditorMode.Source));

        var split = await editor.InvokeAsync(editor.Instance.GetHtmlAroundCaretAsync);
        var selected = await editor.InvokeAsync(editor.Instance.GetSelectedTextAsync);
        var text = await editor.InvokeAsync(editor.Instance.ExportTextAsync);

        Assert.Equal((Start, string.Empty), (split.Before, split.After));
        Assert.Equal(string.Empty, selected);
        Assert.Equal("Bonjour", text);
        Assert.Empty(module.Invocations["aroundCaret"]);
        Assert.Empty(module.Invocations["read"]);
    }

    [Fact]
    public async Task BeforeItsScriptArrives_TheSurfaceRunsNothing()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        Services.AddSingleton<IJSRuntime>(runtime);
        string? changed = null;
        var editor = RenderEditor(parameters => parameters.Add(editor => editor.ValueChanged, html => changed = html));

        await editor.InvokeAsync(() => editor.Instance.RunBuiltInAsync(OmniHtmlEditorAction.Bold, null));
        var split = await editor.InvokeAsync(editor.Instance.GetHtmlAroundCaretAsync);
        var selected = await editor.InvokeAsync(editor.Instance.GetSelectedTextAsync);
        var text = await editor.InvokeAsync(editor.Instance.ExportTextAsync);

        Assert.Null(changed);
        Assert.Equal("Bonjour", text);
        Assert.Equal(Start, split.Before);
        Assert.Equal(string.Empty, selected);
        Assert.Empty(runtime.Module.Calls);
    }

    [Fact]
    public async Task LockedSurface_RefusesTheHostsChanges()
    {
        var module = JSInterop.SetupModule(OmniModules.HtmlEditor);
        var editor = RenderEditor(parameters => parameters.Add(editor => editor.ReadOnly, true));

        await editor.InvokeAsync(editor.Instance.CommitSurfaceAsync);
        await editor.InvokeAsync(() => editor.Instance.ReplaceActivatedAsync("<b>x</b>"));
        await editor.InvokeAsync(() => editor.Instance.SetActivatedTextAsync("x"));

        Assert.Empty(module.Invocations["read"]);
        Assert.Empty(module.Invocations["replaceActivated"]);
        Assert.Empty(module.Invocations["setActivatedText"]);
    }

    [Fact]
    public async Task CommitDom_WithASurfaceThatAnswersNothing_KeepsTheValue()
    {
        var module = JSInterop.SetupModule(OmniModules.HtmlEditor);
        module.Setup<string?>("read", _ => true).SetResult(null);
        string? changed = null;
        var editor = RenderEditor(parameters => parameters.Add(editor => editor.ValueChanged, html => changed = html));

        await editor.InvokeAsync(editor.Instance.CommitSurfaceAsync);

        Assert.Single(module.Invocations["read"]);
        Assert.Null(changed);
    }

    [Fact]
    public async Task ReleasingTheSurfaceScript_OnALostCircuit_IsTaken()
    {
        var runtime = new ManualJSRuntime { Module = new RecordingModule { DisposeFailure = new JSDisconnectedException("perdu") } };
        Services.AddSingleton<IJSRuntime>(runtime);
        var editor = RenderEditor();
        Assert.Contains("mount", runtime.Module.Calls);

        await editor.Instance.DisposeAsync();

        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
    }


    [Theory]
    [InlineData("mot", "mot")]
    [InlineData(null, "")]
    public async Task SelectedText_IsWhatTheSurfaceSelected_OrEmpty(string? answer, string expected)
    {
        var module = JSInterop.SetupModule(OmniModules.HtmlEditor);
        module.Setup<string?>("selectedText", _ => true).SetResult(answer);
        var editor = RenderEditor();

        Assert.Equal(expected, await editor.InvokeAsync(editor.Instance.GetSelectedTextAsync));
    }
}
