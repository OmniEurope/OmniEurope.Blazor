using System.Text.Json;
using Bunit;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The .NET side of <see cref="OmniDiffViewer"/>: the two-pane view it always has to fall back on, and
/// when Monaco's diff editor is asked for, with what, and what happens without it. Monaco itself,
/// served by the host, is exercised in a browser.
/// </summary>
public sealed class DiffViewerComponentTests : OmniBunitContext
{
    private const string ModulePath = "./_content/OmniEurope.Blazor/omni-code-editor.js";

    [Fact]
    public void PlainText_ShowsTheTwoTextsInTwoNamedPanes_WithNoScriptAtAll()
    {
        var viewer = Render<OmniDiffViewer>(parameters => parameters
            .Add(component => component.Original, "a: 1")
            .Add(component => component.Modified, "a: 2")
            .Add(component => component.Engine, OmniCodeEditorEngine.PlainText));

        var root = viewer.Find(".omni-diff-viewer");
        Assert.Equal("group", root.GetAttribute("role"));
        Assert.Equal("Comparaison", root.GetAttribute("aria-label"));
        Assert.Equal("plaintext", root.GetAttribute("data-engine"));
        Assert.Contains("omni-diff-viewer--plaintext", root.ClassList);
        Assert.True(viewer.Find(".omni-diff-viewer__host").HasAttribute("hidden"));
        var panes = viewer.FindAll("section.omni-diff-viewer__pane");
        Assert.Equal(["Avant", "Après"], panes.Select(pane => pane.GetAttribute("aria-label")));
        Assert.All(panes, pane => Assert.Equal("true", pane.QuerySelector(".omni-diff-viewer__pane-title")!.GetAttribute("aria-hidden")));
        Assert.Equal(["a: 1", "a: 2"], viewer.FindAll("pre.omni-diff-viewer__text").Select(text => text.TextContent));
        Assert.All(viewer.FindAll("pre"), text => Assert.Equal("0", text.GetAttribute("tabindex")));
        Assert.Empty(viewer.FindAll("textarea"));
        Assert.Empty(viewer.FindAll(".omni-diff-viewer__notice"));
        Assert.Empty(JSInterop.Invocations);
        Assert.DoesNotContain("style=", viewer.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PlainText_Editable_MakesTheModifiedTextANamedTextarea_ThatReportsItsInput()
    {
        var modified = "a: 2";
        var viewer = Render<OmniDiffViewer>(parameters => parameters
            .Add(component => component.Original, "a: 1")
            .Add(component => component.Modified, modified)
            .Add(component => component.ModifiedChanged, value => modified = value)
            .Add(component => component.ReadOnly, false)
            .Add(component => component.Engine, OmniCodeEditorEngine.PlainText)
            .Add(component => component.ModifiedLabel, "Proposée"));

        var area = viewer.Find("textarea.omni-diff-viewer__input");
        Assert.Equal("Proposée", area.GetAttribute("aria-label"));
        Assert.Equal("false", area.GetAttribute("spellcheck"));
        Assert.Equal("off", area.GetAttribute("wrap"));
        Assert.Single(viewer.FindAll("pre"));

        area.Input("a: 3");

        Assert.Equal("a: 3", modified);
    }

    [Fact]
    public void Labels_Inline_AndId_AreApplied()
    {
        var viewer = Render<OmniDiffViewer>(parameters => parameters
            .Add(component => component.Id, "diff")
            .Add(component => component.Label, "Configuration")
            .Add(component => component.OriginalLabel, "Hier")
            .Add(component => component.ModifiedLabel, "Aujourd'hui")
            .Add(component => component.Inline, true)
            .Add(component => component.Engine, OmniCodeEditorEngine.PlainText));

        var root = viewer.Find("#diff");
        Assert.Equal("Configuration", root.GetAttribute("aria-label"));
        Assert.Contains("omni-diff-viewer--inline", root.ClassList);
        Assert.Equal(["Hier", "Aujourd'hui"], viewer.FindAll("section").Select(pane => pane.GetAttribute("aria-label")));
    }

    [Fact]
    public void Height_IsSetThroughTheScript_NeverAsAStyle_AndMustBeACssLength()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var viewer = Render<OmniDiffViewer>(parameters => parameters
            .Add(component => component.Engine, OmniCodeEditorEngine.PlainText)
            .Add(component => component.Height, "18rem"));

        var height = Assert.Single(module.Invocations["setHeight"]);
        Assert.Equal("18rem", height.Arguments[1]);
        Assert.DoesNotContain("style=", viewer.Markup, StringComparison.OrdinalIgnoreCase);

        Assert.Throws<ArgumentException>(() => Render<OmniDiffViewer>(parameters => parameters
            .Add(component => component.Engine, OmniCodeEditorEngine.PlainText)
            .Add(component => component.Height, "calc(1px)")));
    }

    [Theory]
    [InlineData("https://cdn.example/vs")]
    [InlineData("//cdn.example/vs")]
    public void MonacoPath_OnAnotherOrigin_IsRefused(string path) =>
        Assert.Throws<ArgumentException>(() => Render<OmniDiffViewer>(parameters => parameters.Add(component => component.MonacoPath, path)));

    [Fact]
    public void Monaco_IsLoadedFromTheHostPath_ThenMountedWithBothTextsAndTheOptions()
    {
        var module = SetupMonaco();
        var viewer = Render<OmniDiffViewer>(parameters => parameters
            .Add(component => component.Original, "{}")
            .Add(component => component.Modified, "{ \"a\": 1 }")
            .Add(component => component.Language, "json")
            .Add(component => component.Inline, true)
            .Add(component => component.MonacoPath, "assets/monaco/vs"));

        var load = Assert.Single(module.Invocations["load"]);
        Assert.Equal("assets/monaco/vs", load.Arguments[0]);
        Assert.Equal("fr-FR", load.Arguments[1]);
        viewer.WaitForAssertion(() => Assert.Single(module.Invocations["mountDiff"]));
        var mount = module.Invocations["mountDiff"][0];
        Assert.IsType<DotNetObjectReference<DiffViewerInteropBridge>>(mount.Arguments[1]);
        var options = JsonSerializer.SerializeToElement(mount.Arguments[2]);
        Assert.Equal("{}", options.GetProperty("original").GetString());
        Assert.Equal("{ \"a\": 1 }", options.GetProperty("modified").GetString());
        Assert.Equal("json", options.GetProperty("language").GetString());
        Assert.True(options.GetProperty("readOnly").GetBoolean());
        Assert.True(options.GetProperty("inline").GetBoolean());
        Assert.Equal("Avant", options.GetProperty("originalLabel").GetString());
        Assert.Equal("Après", options.GetProperty("modifiedLabel").GetString());

        Assert.Contains("omni-diff-viewer--ready", viewer.Find(".omni-diff-viewer").ClassList);
        Assert.Equal("monaco", viewer.Find(".omni-diff-viewer").GetAttribute("data-engine"));
        Assert.False(viewer.Find(".omni-diff-viewer__host").HasAttribute("hidden"));
        Assert.Empty(viewer.FindAll(".omni-diff-viewer__panes"));
    }

    [Fact]
    public void Monaco_WhileLoading_ShowsThePanes()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("load", _ => true);
        var viewer = Render<OmniDiffViewer>(parameters => parameters
            .Add(component => component.Original, "a")
            .Add(component => component.Modified, "b"));

        Assert.Contains("omni-diff-viewer--loading", viewer.Find(".omni-diff-viewer").ClassList);
        Assert.Equal(2, viewer.FindAll("section.omni-diff-viewer__pane").Count);
        Assert.Empty(module.Invocations["mountDiff"]);
    }

    [Fact]
    public void Monaco_ThatCannotBeLoaded_LeavesThePanesAndSaysSo()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("load", _ => true).SetResult(false);
        var viewer = Render<OmniDiffViewer>(parameters => parameters
            .Add(component => component.Original, "a")
            .Add(component => component.Modified, "b"));

        viewer.WaitForAssertion(() => Assert.NotNull(viewer.Find(".omni-diff-viewer__notice")));
        var notice = viewer.Find(".omni-diff-viewer__notice");
        Assert.Equal("status", notice.GetAttribute("role"));
        Assert.StartsWith("L'éditeur Monaco n'a pas pu être chargé", notice.TextContent, StringComparison.Ordinal);
        Assert.Contains("omni-diff-viewer--failed", viewer.Find(".omni-diff-viewer").ClassList);
        Assert.Equal(2, viewer.FindAll("pre").Count);
        Assert.Empty(module.Invocations["mountDiff"]);
    }

    [Fact]
    public void Monaco_ThatRefusesToMount_FallsBackToThePanes()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("load", _ => true).SetResult(true);
        module.Setup<bool>("mountDiff", _ => true).SetResult(false);
        var viewer = Render<OmniDiffViewer>(parameters => parameters.Add(component => component.Original, "a"));

        viewer.WaitForAssertion(() => Assert.NotNull(viewer.Find(".omni-diff-viewer__notice")));
        Assert.Contains("omni-diff-viewer--failed", viewer.Find(".omni-diff-viewer").ClassList);
    }

    [Fact]
    public async Task Monaco_EditsReachModifiedChanged_OnlyWhenTheViewerIsEditable()
    {
        var module = SetupMonaco();
        var changes = new List<string>();
        var viewer = Render<OmniDiffViewer>(parameters => parameters
            .Add(component => component.Modified, "b")
            .Add(component => component.ModifiedChanged, value => changes.Add(value))
            .Add(component => component.ReadOnly, false));
        viewer.WaitForAssertion(() => Assert.Single(module.Invocations["mountDiff"]));
        var bridge = (DotNetObjectReference<DiffViewerInteropBridge>)module.Invocations["mountDiff"][0].Arguments[1]!;

        await viewer.InvokeAsync(() => bridge.Value.OnModifiedChanged("b2"));
        Assert.Equal(["b2"], changes);

        viewer.Render(parameters => parameters.Add(component => component.ReadOnly, true));
        await viewer.InvokeAsync(() => bridge.Value.OnModifiedChanged("b3"));

        Assert.Equal(["b2"], changes);
    }

    [Fact]
    public void Monaco_NewTextsAndNewOptions_AreSentToTheMountedEditor()
    {
        var module = SetupMonaco();
        var viewer = Render<OmniDiffViewer>(parameters => parameters
            .Add(component => component.Original, "a")
            .Add(component => component.Modified, "b"));
        viewer.WaitForAssertion(() => Assert.Single(module.Invocations["mountDiff"]));

        viewer.Render(parameters => parameters.Add(component => component.Modified, "c"));
        var diff = Assert.Single(module.Invocations["setDiff"]);
        Assert.Equal(("a", "c"), (diff.Arguments[1], diff.Arguments[2]));
        Assert.Empty(module.Invocations["configureDiff"]);

        viewer.Render(parameters => parameters.Add(component => component.Inline, true).Add(component => component.Language, "yaml"));
        var configure = JsonSerializer.SerializeToElement(Assert.Single(module.Invocations["configureDiff"]).Arguments[1]);
        Assert.True(configure.GetProperty("inline").GetBoolean());
        Assert.Equal("yaml", configure.GetProperty("language").GetString());
        Assert.Single(module.Invocations["setDiff"]);
    }

    [Fact]
    public void Monaco_SwitchedToPlainText_DisposesTheEditor_AndShowsThePanes()
    {
        var module = SetupMonaco();
        var viewer = Render<OmniDiffViewer>(parameters => parameters.Add(component => component.Original, "a"));
        viewer.WaitForAssertion(() => Assert.Single(module.Invocations["mountDiff"]));

        viewer.Render(parameters => parameters.Add(component => component.Engine, OmniCodeEditorEngine.PlainText));

        Assert.Single(module.Invocations["disposeDiff"]);
        Assert.Contains("omni-diff-viewer--plaintext", viewer.Find(".omni-diff-viewer").ClassList);
        Assert.Equal(2, viewer.FindAll("section.omni-diff-viewer__pane").Count);
    }

    [Fact]
    public async Task Dispose_DisposesTheEditor_AndReleasesTheBridge()
    {
        var module = SetupMonaco();
        var viewer = Render<OmniDiffViewer>(parameters => parameters.Add(component => component.Original, "a"));
        viewer.WaitForAssertion(() => Assert.Single(module.Invocations["mountDiff"]));
        var bridge = (DotNetObjectReference<DiffViewerInteropBridge>)module.Invocations["mountDiff"][0].Arguments[1]!;

        await DisposeComponentsAsync();

        Assert.Single(module.Invocations["disposeDiff"]);
        Assert.Throws<ObjectDisposedException>(() => bridge.Value);
    }

    private BunitJSModuleInterop SetupMonaco()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("load", _ => true).SetResult(true);
        module.Setup<bool>("mountDiff", _ => true).SetResult(true);
        module.SetupVoid("setDiff", _ => true).SetVoidResult();
        module.SetupVoid("configureDiff", _ => true).SetVoidResult();
        module.SetupVoid("disposeDiff", _ => true).SetVoidResult();
        return module;
    }
}
