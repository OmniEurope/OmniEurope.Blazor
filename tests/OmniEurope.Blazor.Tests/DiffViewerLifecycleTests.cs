using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// OmniDiffViewer across engines and renders: Monaco that cannot mount, an engine switched back and
/// forth, texts and options pushed only when they change, a load or an import that ends after the viewer
/// went, the plain view locked, and a lost circuit.
/// </summary>
public sealed class DiffViewerLifecycleTests : OmniBunitContext
{
    private BunitJSModuleInterop Monaco(bool mounts = true)
    {
        var module = JSInterop.SetupModule(OmniModules.CodeEditor);
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<bool>("load", _ => true).SetResult(true);
        module.Setup<bool>("mountDiff", _ => true).SetResult(mounts);
        return module;
    }

    private IRenderedComponent<OmniDiffViewer> RenderViewer(Action<ComponentParameterCollectionBuilder<OmniDiffViewer>>? extra = null) =>
        Render<OmniDiffViewer>(parameters =>
        {
            parameters.Add(viewer => viewer.Original, "a").Add(viewer => viewer.Modified, "b");
            extra?.Invoke(parameters);
        });

    [Fact]
    public void MonacoThatCannotMount_FallsBackToThePlainView()
    {
        Monaco(mounts: false);

        var viewer = RenderViewer();

        viewer.WaitForAssertion(() => Assert.Contains("omni-diff-viewer--failed", viewer.Find(".omni-diff-viewer").ClassList));
    }

    [Fact]
    public void EngineSwitchedToPlainText_ReleasesMonaco_AndBackLoadsItAgain()
    {
        var module = Monaco();
        var viewer = RenderViewer();
        viewer.WaitForAssertion(() => Assert.Single(module.Invocations["mountDiff"]));

        viewer.Render(parameters => parameters.Add(component => component.Engine, OmniCodeEditorEngine.PlainText));
        Assert.Single(module.Invocations["disposeDiff"]);

        viewer.Render(parameters => parameters.Add(component => component.Engine, OmniCodeEditorEngine.Monaco));
        viewer.WaitForAssertion(() => Assert.Equal(2, module.Invocations["mountDiff"].Count));
    }

    [Fact]
    public void TextsAndOptions_ArePushedOnlyWhenTheyChange_AndNullTextsAreEmpty()
    {
        var module = Monaco();
        var viewer = Render<OmniDiffViewer>(parameters => parameters
            .Add(viewer => viewer.Original, null!)
            .Add(viewer => viewer.Modified, null!)
            .Add(viewer => viewer.Language, " "));
        viewer.WaitForAssertion(() => Assert.Single(module.Invocations["mountDiff"]));
        var mount = module.Invocations["mountDiff"][0].Arguments[2]!;
        Assert.Equal(string.Empty, mount.GetType().GetProperty("original")!.GetValue(mount));
        Assert.Equal("plaintext", mount.GetType().GetProperty("language")!.GetValue(mount));

        viewer.Render();
        Assert.Empty(module.Invocations["setDiff"]);
        Assert.Empty(module.Invocations["configureDiff"]);

        viewer.Render(parameters => parameters.Add(component => component.Modified, "b"));
        Assert.Equal("b", Assert.Single(module.Invocations["setDiff"]).Arguments[2]);
        viewer.Render(parameters => parameters.Add(component => component.Original, "c"));
        Assert.Equal(2, module.Invocations["setDiff"].Count);
        viewer.Render(parameters => parameters.Add(component => component.Inline, true));
        Assert.Single(module.Invocations["configureDiff"]);
    }

    [Fact]
    public async Task LoadEndingAfterTheViewerLeftOrChangedEngine_ChangesNothing()
    {
        var module = JSInterop.SetupModule(OmniModules.CodeEditor);
        module.Mode = JSRuntimeMode.Loose;
        var load = module.Setup<bool>("load", _ => true);
        var gone = RenderViewer();
        var switched = RenderViewer();

        await gone.Instance.DisposeAsync();
        switched.Render(parameters => parameters.Add(component => component.Engine, OmniCodeEditorEngine.PlainText));
        load.SetResult(true);

        Assert.Empty(module.Invocations["mountDiff"]);
        Assert.Contains("omni-diff-viewer--plaintext", switched.Find(".omni-diff-viewer").ClassList);
    }

    [Theory]
    [InlineData("20rem")]
    [InlineData(null)]
    public async Task ViewerGoneWhileItsScriptLoads_ReleasesItOnArrival(string? height)
    {
        // With a height the script is first imported to set it; without, to load Monaco.
        var runtime = new ManualJSRuntime { HoldImports = true };
        Services.AddSingleton<IJSRuntime>(runtime);
        var viewer = RenderViewer(parameters => parameters.Add(viewer => viewer.Height, height));

        await viewer.Instance.DisposeAsync();
        runtime.PendingImport.SetResult(runtime.Module);

        await runtime.Module.Disposal.Task.WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);
        Assert.Empty(runtime.Module.Calls);
    }

    [Fact]
    public void LostCircuitWhileSyncing_IsIgnored()
    {
        var runtime = new ManualJSRuntime();
        runtime.Module.CallFailures["setHeight"] = new JSDisconnectedException("perdu");
        Services.AddSingleton<IJSRuntime>(runtime);

        var viewer = RenderViewer(parameters => parameters.Add(viewer => viewer.Height, "20rem"));

        Assert.Equal(["setHeight"], runtime.Module.Calls);
    }

    [Fact]
    public async Task Dispose_OnALostCircuit_IsQuiet_AndARenderAfterItDoesNothing()
    {
        var runtime = new ManualJSRuntime { Module = new RecordingModule { DisposeFailure = new JSDisconnectedException("perdu") } };
        runtime.Module.Answers["load"] = true;
        runtime.Module.Answers["mountDiff"] = true;
        Services.AddSingleton<IJSRuntime>(runtime);
        var viewer = RenderViewer();
        viewer.WaitForAssertion(() => Assert.Contains("mountDiff", runtime.Module.Calls));

        await viewer.Instance.DisposeAsync();
        var calls = runtime.Module.Calls.Count;
        viewer.Render();

        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
        Assert.Contains("disposeDiff", runtime.Module.Calls);
        Assert.Equal(calls, runtime.Module.Calls.Count);
    }

    [Fact]
    public void PlainView_LockedIgnoresInput_AndANullInputIsEmpty()
    {
        var changes = new List<string>();
        var disabled = RenderViewer(parameters => parameters
            .Add(viewer => viewer.Engine, OmniCodeEditorEngine.PlainText)
            .Add(viewer => viewer.ReadOnly, false)
            .Add(viewer => viewer.Disabled, true)
            .Add(viewer => viewer.ModifiedChanged, value => changes.Add(value)));
        disabled.Find("textarea").Input("x");
        Assert.Empty(changes);

        var open = RenderViewer(parameters => parameters
            .Add(viewer => viewer.Engine, OmniCodeEditorEngine.PlainText)
            .Add(viewer => viewer.ReadOnly, false)
            .Add(viewer => viewer.ModifiedChanged, value => changes.Add(value)));
        open.Find("textarea").Input((object?)null);
        Assert.Equal([string.Empty], changes);
    }
}
