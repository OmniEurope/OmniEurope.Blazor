using Bunit;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The .NET side of <see cref="OmniCodeEditor"/>: when Monaco is asked for, with what, and what
/// happens without it. Monaco itself, served by the host, is exercised in a browser.
/// </summary>
public sealed class CodeEditorComponentTests : OmniBunitContext
{
    private const string ModulePath = "./_content/OmniEurope.Blazor/omni-code-editor.js";

    [Fact]
    public void PlainText_IsATextareaWithNoScriptAtAll()
    {
        var value = "a: 1";
        var editor = Render<OmniCodeEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, updated => value = updated)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Id, "code")
            .Add(component => component.Engine, OmniCodeEditorEngine.PlainText));

        var area = editor.Find("textarea#code");
        Assert.False(area.HasAttribute("hidden"));
        Assert.Equal("off", area.GetAttribute("wrap"));
        Assert.Empty(editor.FindAll(".omni-code-editor__status"));

        area.Input("a: 2");

        Assert.Equal("a: 2", value);
        Assert.Empty(JSInterop.Invocations);
    }

    [Fact]
    public void Monaco_IsLoadedFromTheHostPath_ThenMountedWithTheValueAndTheOptions()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("load", _ => true).SetResult(true);
        module.Setup<bool>("mount", _ => true).SetResult(true);
        var value = "stages:\n  - pipeline: build\n";
        var editor = Render<OmniCodeEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Language, "yaml")
            .Add(component => component.ReadOnly, true)
            .Add(component => component.MonacoPath, "assets/monaco/vs")
            .Add(component => component.Links, [new OmniCodeEditorLink("pipeline", "pipeline:\\s*(\\S+)")]));

        var load = Assert.Single(module.Invocations["load"]);
        Assert.Equal("assets/monaco/vs", load.Arguments[0]);
        Assert.Equal("fr-FR", load.Arguments[1]);
        editor.WaitForAssertion(() => Assert.Single(module.Invocations["mount"]));
        var mount = module.Invocations["mount"][0];
        Assert.IsType<DotNetObjectReference<CodeEditorInteropBridge>>(mount.Arguments[1]);
        var options = System.Text.Json.JsonSerializer.SerializeToElement(mount.Arguments[2]);
        Assert.Equal(value, options.GetProperty("value").GetString());
        Assert.Equal("yaml", options.GetProperty("language").GetString());
        Assert.True(options.GetProperty("readOnly").GetBoolean());
        Assert.Equal("pipeline", options.GetProperty("links")[0].GetProperty("name").GetString());
        Assert.Equal("Ctrl+clic pour suivre le lien", options.GetProperty("links")[0].GetProperty("tooltip").GetString());
        Assert.True(editor.Find("textarea").HasAttribute("hidden"));
        Assert.False(editor.Find(".omni-code-editor__host").HasAttribute("hidden"));
        Assert.Equal("Ligne 1, colonne 1", editor.Find(".omni-code-editor__status span").TextContent);
    }

    [Fact]
    public async Task Monaco_ChangesReachTheValue_AndTheValueReachesMonaco_WithoutAnEcho()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("load", _ => true).SetResult(true);
        module.Setup<bool>("mount", _ => true).SetResult(true);
        module.SetupVoid("setValue", _ => true).SetVoidResult();
        module.SetupVoid("configure", _ => true).SetVoidResult();
        var value = "{}";
        var editor = Render<OmniCodeEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, updated => value = updated)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Language, "json"));
        editor.WaitForAssertion(() => Assert.Single(module.Invocations["mount"]));
        var bridge = new CodeEditorInteropBridge(editor.Instance);

        await editor.InvokeAsync(() => bridge.OnCodeChanged("{ \"a\": 1 }"));
        await editor.InvokeAsync(() => bridge.OnCursorChanged(1, 9));

        Assert.Equal("{ \"a\": 1 }", value);
        Assert.Empty(module.Invocations["setValue"]);
        Assert.Equal("Ligne 1, colonne 9", editor.Find(".omni-code-editor__status span").TextContent);

        editor.Render(parameters => parameters.Add(component => component.Value, "[]").Add(component => component.Language, "yaml"));

        Assert.Equal("[]", Assert.Single(module.Invocations["setValue"]).Arguments[1]);
        var configure = Assert.Single(module.Invocations["configure"]);
        Assert.Equal("yaml", System.Text.Json.JsonSerializer.SerializeToElement(configure.Arguments[1]).GetProperty("language").GetString());
    }

    [Fact]
    public void Monaco_ThatCannotBeLoaded_LeavesAWorkingTextareaAndSaysSo()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("load", _ => true).SetResult(false);
        var value = "SELECT 1";
        var editor = Render<OmniCodeEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, updated => value = updated)
            .Add(component => component.ValueExpression, () => value));

        editor.WaitForAssertion(() => Assert.NotNull(editor.Find(".omni-code-editor__notice")));
        Assert.Empty(module.Invocations["mount"]);
        var area = editor.Find("textarea");
        Assert.False(area.HasAttribute("hidden"));

        area.Input("SELECT 2");

        Assert.Equal("SELECT 2", value);
    }

    [Fact]
    public async Task FollowedLink_IsReportedWithItsTargetAndLine()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("load", _ => true).SetResult(true);
        module.Setup<bool>("mount", _ => true).SetResult(true);
        OmniCodeEditorLinkEventArgs? followed = null;
        var value = "pipeline: build";
        var editor = Render<OmniCodeEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.LinkActivated, args => followed = args));

        await editor.InvokeAsync(() => new CodeEditorInteropBridge(editor.Instance).OnLinkActivated("pipeline", "build", 1));

        Assert.Equal(new OmniCodeEditorLinkEventArgs("pipeline", "build", 1), followed);
    }

    [Fact]
    public void Height_IsSetThroughTheModule_AndMustBeACssLength()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.SetupVoid("setHeight", _ => true).SetVoidResult();
        var value = string.Empty;

        Render<OmniCodeEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Engine, OmniCodeEditorEngine.PlainText)
            .Add(component => component.Height, "24rem"));

        Assert.Equal("24rem", Assert.Single(module.Invocations["setHeight"]).Arguments[1]);
        Assert.Throws<ArgumentException>(() => Render<OmniCodeEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Height, "10rem; color: red")));
    }

    [Theory]
    [InlineData("https://cdn.example.test/monaco/vs")]
    [InlineData("//cdn.example.test/monaco/vs")]
    public void MonacoPath_OnAnotherOrigin_IsRefused(string path)
    {
        var value = string.Empty;

        var exception = Assert.Throws<ArgumentException>(() => Render<OmniCodeEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.MonacoPath, path)));

        Assert.Equal(nameof(OmniCodeEditor.MonacoPath), exception.ParamName);
    }
}
