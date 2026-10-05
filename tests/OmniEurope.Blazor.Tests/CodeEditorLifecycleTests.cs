using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// OmniCodeEditor across engines and renders: Monaco that cannot mount, an engine switched back and
/// forth, a load that ends after the editor left or changed engine, the text area locked, and a
/// release on a lost circuit.
/// </summary>
public sealed class CodeEditorLifecycleTests : OmniBunitContext
{
    private readonly string _bound = "code";

    private IRenderedComponent<OmniCodeEditor> RenderEditor(Action<ComponentParameterCollectionBuilder<OmniCodeEditor>>? extra = null, List<string>? changes = null) =>
        Render<OmniCodeEditor>(parameters =>
        {
            parameters.Add(editor => editor.Value, "code").Add(editor => editor.ValueExpression, () => _bound);
            if (changes is not null)
            {
                parameters.Add(editor => editor.ValueChanged, value => changes.Add(value));
            }

            extra?.Invoke(parameters);
        });

    private BunitJSModuleInterop Monaco(bool loads = true, bool mounts = true)
    {
        var module = JSInterop.SetupModule(OmniModules.CodeEditor);
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<bool>("load", _ => true).SetResult(loads);
        module.Setup<bool>("mount", _ => true).SetResult(mounts);
        return module;
    }

    [Fact]
    public void MonacoThatCannotMount_FallsBackToTheTextArea()
    {
        Monaco(mounts: false);

        var editor = RenderEditor();

        editor.WaitForAssertion(() => Assert.NotEmpty(editor.FindAll("textarea")));
        Assert.Contains("omni-code-editor--failed", editor.Find(".omni-code-editor").ClassList);
    }

    [Fact]
    public void Monaco_MountsAnEmptyValue_WithItsLanguage_AndTheStatusBarFollowsThePhase()
    {
        var module = Monaco();
        var editor = Render<OmniCodeEditor>(parameters => parameters
            .Add(component => component.Value, null)
            .Add(component => component.ValueExpression, () => _bound)
            .Add(component => component.Language, "csharp")
            .Add(component => component.ShowStatusBar, true));

        editor.WaitForAssertion(() => Assert.Single(module.Invocations["mount"]));
        Assert.Contains("csharp", editor.Find(".omni-code-editor__status").TextContent, StringComparison.Ordinal);

        var plain = RenderEditor(parameters => parameters.Add(component => component.Engine, OmniCodeEditorEngine.PlainText).Add(component => component.ShowStatusBar, true));
        Assert.Empty(plain.FindAll(".omni-code-editor__status"));
    }

    [Fact]
    public void Monaco_IsGivenTheLabel_AndEachLinkWithItsTooltipOrTheLocalizedHint()
    {
        var module = Monaco();
        var editor = RenderEditor(parameters => parameters
            .Add(editor => editor.Label, "Script")
            .Add(editor => editor.Links, [new OmniCodeEditorLink("file", "x"), new OmniCodeEditorLink("todo", "TODO") { Tooltip = "Tâche" }]));

        editor.WaitForAssertion(() => Assert.Single(module.Invocations["mount"]));
        var options = module.Invocations["mount"][0].Arguments[2]!;
        Assert.Equal("Script", options.GetType().GetProperty("label")!.GetValue(options));
        var links = (Array)options.GetType().GetProperty("links")!.GetValue(options)!;
        var tooltips = links.Cast<object>().Select(link => link.GetType().GetProperty("tooltip")!.GetValue(link)).ToArray();
        Assert.Equal(["Ctrl+clic pour suivre le lien", "Tâche"], tooltips);
    }

    [Fact]
    public void BlankLanguage_IsPlainTextForMonaco_AndTheStatusBarCanBeHidden()
    {
        var module = Monaco();
        var editor = RenderEditor(parameters => parameters.Add(editor => editor.Language, " ").Add(editor => editor.ShowStatusBar, false));

        editor.WaitForAssertion(() => Assert.Single(module.Invocations["mount"]));
        var options = module.Invocations["mount"][0].Arguments[2]!;
        Assert.Equal("plaintext", options.GetType().GetProperty("language")!.GetValue(options));
        Assert.Empty(editor.FindAll(".omni-code-editor__status"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(@"scripts\editor.js")]
    public void InteropModulePath_EmptyOrWithABackslash_IsRefused(string path)
    {
        Assert.Throws<ArgumentException>(() => RenderEditor(parameters => parameters.Add(editor => editor.InteropModulePath, path)));
    }

    [Fact]
    public void EngineSwitchedToPlainText_ReleasesMonaco_AndBackLoadsItAgain()
    {
        var module = Monaco();
        var editor = RenderEditor();
        editor.WaitForAssertion(() => Assert.Single(module.Invocations["mount"]));

        editor.Render(parameters => parameters.Add(component => component.Engine, OmniCodeEditorEngine.PlainText));
        Assert.Single(module.Invocations["dispose"]);
        Assert.NotEmpty(editor.FindAll("textarea"));

        editor.Render(parameters => parameters.Add(component => component.Engine, OmniCodeEditorEngine.Monaco));
        editor.WaitForAssertion(() => Assert.Equal(2, module.Invocations["mount"].Count));
    }

    [Fact]
    public void ValueAndOptionsUnchanged_PushNothing_AndANullValueIsSentEmpty()
    {
        var module = Monaco();
        var editor = RenderEditor();
        editor.WaitForAssertion(() => Assert.Single(module.Invocations["mount"]));

        editor.Render();
        Assert.Empty(module.Invocations["setValue"]);
        Assert.Empty(module.Invocations["configure"]);

        editor.Render(parameters => parameters.Add(component => component.Value, null));
        Assert.Equal(string.Empty, Assert.Single(module.Invocations["setValue"]).Arguments[1]);
    }

    [Fact]
    public async Task LoadEndingAfterTheEditorLeftOrChangedEngine_ChangesNothing()
    {
        var module = JSInterop.SetupModule(OmniModules.CodeEditor);
        module.Mode = JSRuntimeMode.Loose;
        var load = module.Setup<bool>("load", _ => true);
        var gone = RenderEditor();
        var switched = RenderEditor();

        await gone.Instance.DisposeAsync();
        switched.Render(parameters => parameters.Add(component => component.Engine, OmniCodeEditorEngine.PlainText));
        load.SetResult(true);

        Assert.Empty(module.Invocations["mount"]);
        Assert.NotEmpty(switched.FindAll("textarea"));
    }

    [Fact]
    public void TextArea_OfALockedEditor_IgnoresInput_AndANullInputIsEmpty()
    {
        var changes = new List<string>();
        var locked = RenderEditor(parameters => parameters.Add(editor => editor.Engine, OmniCodeEditorEngine.PlainText).Add(editor => editor.ReadOnly, true), changes);
        locked.Find("textarea").Input("autre");
        Assert.Empty(changes);

        var open = RenderEditor(parameters => parameters.Add(editor => editor.Engine, OmniCodeEditorEngine.PlainText), changes);
        open.Find("textarea").Input((object?)null);
        Assert.Equal([string.Empty], changes);
    }

    [Fact]
    public void Label_NamesTheEditor_OrTheLocalizedDefault()
    {
        var named = RenderEditor(parameters => parameters.Add(editor => editor.Engine, OmniCodeEditorEngine.PlainText).Add(editor => editor.Label, "Script"));
        Assert.Equal("Script", named.Find("textarea").GetAttribute("aria-label"));
        // Without a label, Monaco is given the localized name.
        var module = Monaco();
        var unnamed = RenderEditor();
        unnamed.WaitForAssertion(() => Assert.Single(module.Invocations["mount"]));
        var options = module.Invocations["mount"][0].Arguments[2]!;
        Assert.Equal("Éditeur de code", options.GetType().GetProperty("label")!.GetValue(options));
    }

    [Fact]
    public async Task Dispose_OnALostCircuit_IsQuiet_AndARenderAfterItDoesNothing()
    {
        var runtime = new ManualJSRuntime { Module = new RecordingModule { DisposeFailure = new JSDisconnectedException("perdu") } };
        runtime.Module.Answers["load"] = true;
        runtime.Module.Answers["mount"] = true;
        Services.AddSingleton<IJSRuntime>(runtime);
        var editor = RenderEditor();
        editor.WaitForAssertion(() => Assert.Contains("mount", runtime.Module.Calls));

        await editor.Instance.DisposeAsync();
        var calls = runtime.Module.Calls.Count;
        editor.Render();

        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
        Assert.Equal(calls, runtime.Module.Calls.Count);
    }

    [Fact]
    public void TextParsing_TakesAnyText_NullAsEmpty()
    {
        var editor = Render<ParsingCodeEditor>(parameters => parameters
            .Add(component => component.Value, "x")
            .Add(component => component.ValueExpression, () => _bound)
            .Add(component => component.Engine, OmniCodeEditorEngine.PlainText));

        Assert.True(editor.Instance.Parse(null, out var result, out var message));
        Assert.Equal(string.Empty, result);
        Assert.Null(message);
        Assert.True(editor.Instance.Parse("let x = 1;", out result, out _));
        Assert.Equal("let x = 1;", result);
    }

    /// <summary>Opens the text parsing of the editor, which no input of the editor goes through.</summary>
    public sealed class ParsingCodeEditor : OmniCodeEditor
    {
        public bool Parse(string? text, out string result, out string message) => TryParseValueFromString(text, out result, out message);
    }
}
