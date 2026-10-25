using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The change navigation of <see cref="OmniDiffViewer"/> (the two public methods and the ShowNavigation bar) and
/// its FillAvailableHeight option. The .NET side only: that Monaco's <c>goToDiff</c> moves the editor, and that the
/// box follows its parent, is a browser matter.
/// </summary>
public sealed class DiffViewerNavigationTests : OmniBunitContext
{
    private const string ModulePath = OmniModules.CodeEditor;

    private BunitJSModuleInterop SetupMonaco()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("load", _ => true).SetResult(true);
        module.Setup<bool>("mountDiff", _ => true).SetResult(true);
        module.SetupVoid("goToDiff", _ => true).SetVoidResult();
        module.SetupVoid("disposeDiff", _ => true).SetVoidResult();
        return module;
    }

    [Fact]
    public async Task GoToNextAndPrevious_OnAMountedEditor_CallTheScriptWithTheHostAndTheDirection()
    {
        var module = SetupMonaco();
        var viewer = Render<OmniDiffViewer>(parameters => parameters.Add(component => component.Original, "a"));
        viewer.WaitForAssertion(() => Assert.Single(module.Invocations["mountDiff"]));
        var host = module.Invocations["mountDiff"][0].Arguments[0];

        await viewer.InvokeAsync(viewer.Instance.GoToNextChangeAsync);
        await viewer.InvokeAsync(viewer.Instance.GoToPreviousChangeAsync);

        var calls = module.Invocations["goToDiff"];
        Assert.Equal(["next", "previous"], calls.Select(call => call.Arguments[1]));
        Assert.All(calls, call => Assert.Equal(host, call.Arguments[0]));
    }

    [Fact]
    public async Task GoToNextAndPrevious_WhileMonacoLoads_DoNothing()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("load", _ => true);
        var viewer = Render<OmniDiffViewer>(parameters => parameters.Add(component => component.Original, "a"));

        await viewer.InvokeAsync(viewer.Instance.GoToNextChangeAsync);
        await viewer.InvokeAsync(viewer.Instance.GoToPreviousChangeAsync);

        Assert.Contains("omni-diff-viewer--loading", viewer.Find(".omni-diff-viewer").ClassList);
        Assert.Empty(module.Invocations["goToDiff"]);
    }

    [Fact]
    public async Task GoToNextAndPrevious_WithThePlainTextEngine_DoNothing_AndLoadNoScript()
    {
        var viewer = Render<OmniDiffViewer>(parameters => parameters.Add(component => component.Engine, OmniCodeEditorEngine.PlainText));

        await viewer.InvokeAsync(viewer.Instance.GoToNextChangeAsync);
        await viewer.InvokeAsync(viewer.Instance.GoToPreviousChangeAsync);

        Assert.Empty(JSInterop.Invocations);
    }

    [Fact]
    public async Task GoToNextAndPrevious_WhenMonacoFailedToLoad_DoNothing()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<bool>("load", _ => true).SetResult(false);
        var viewer = Render<OmniDiffViewer>(parameters => parameters.Add(component => component.Original, "a"));
        viewer.WaitForAssertion(() => Assert.Contains("omni-diff-viewer--failed", viewer.Find(".omni-diff-viewer").ClassList));

        await viewer.InvokeAsync(viewer.Instance.GoToNextChangeAsync);

        Assert.Empty(module.Invocations["goToDiff"]);
    }

    [Fact]
    public async Task GoToNextAndPrevious_AfterTheViewerIsDisposed_DoNothing()
    {
        var module = SetupMonaco();
        var viewer = Render<OmniDiffViewer>(parameters => parameters.Add(component => component.Original, "a"));
        viewer.WaitForAssertion(() => Assert.Single(module.Invocations["mountDiff"]));
        await viewer.Instance.DisposeAsync();

        await viewer.Instance.GoToNextChangeAsync();
        await viewer.Instance.GoToPreviousChangeAsync();

        Assert.Empty(module.Invocations["goToDiff"]);
    }

    [Fact]
    public async Task GoToNext_OnALostCircuit_IsQuiet()
    {
        var runtime = new ManualJSRuntime();
        runtime.Module.Answers["load"] = true;
        runtime.Module.Answers["mountDiff"] = true;
        runtime.Module.CallFailures["goToDiff"] = new JSDisconnectedException("perdu");
        Services.AddSingleton<IJSRuntime>(runtime);
        var viewer = Render<OmniDiffViewer>(parameters => parameters.Add(component => component.Original, "a"));
        viewer.WaitForAssertion(() => Assert.Contains("mountDiff", runtime.Module.Calls));

        await viewer.InvokeAsync(viewer.Instance.GoToNextChangeAsync);

        Assert.Contains("goToDiff", runtime.Module.Calls);
    }

    [Fact]
    public void ShowNavigation_IsOffByDefault_AndDrawsNoBar()
    {
        var viewer = Render<OmniDiffViewer>(parameters => parameters.Add(component => component.Engine, OmniCodeEditorEngine.PlainText));

        Assert.False(viewer.Instance.ShowNavigation);
        Assert.Empty(viewer.FindAll(".omni-diff-viewer__bar"));
        Assert.Empty(viewer.FindAll("button"));
    }

    [Fact]
    public void ShowNavigation_DrawsTwoIconOnlyButtons_NamedAndTitled_BeforeTheComparison()
    {
        var viewer = Render<OmniDiffViewer>(parameters => parameters
            .Add(component => component.Engine, OmniCodeEditorEngine.PlainText)
            .Add(component => component.ShowNavigation, true));

        var bar = viewer.Find(".omni-diff-viewer__bar");
        Assert.Contains("omni-diff-viewer__bar", viewer.Find(".omni-diff-viewer").FirstElementChild!.ClassList);
        var buttons = bar.QuerySelectorAll("button").ToList();
        Assert.Equal(["omni-diff-viewer__previous", "omni-diff-viewer__next"], buttons.Select(button => button.ClassList.First(name => name.StartsWith("omni-diff-viewer__", StringComparison.Ordinal))));
        Assert.Equal(["Modification précédente", "Modification suivante"], buttons.Select(button => button.GetAttribute("aria-label")));
        Assert.Equal(["Modification précédente", "Modification suivante"], buttons.Select(button => button.GetAttribute("title")));
        Assert.All(buttons, button =>
        {
            Assert.Contains("omni-button--icon", button.ClassList);
            Assert.Equal("button", button.GetAttribute("type"));
            Assert.Empty(button.TextContent.Trim());
            Assert.NotNull(button.QuerySelector("svg, .omni-icon"));
        });
        Assert.DoesNotContain("style=", viewer.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShowNavigation_ButtonsAreDisabledUntilMonacoIsMounted_ThenCallTheScript()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var load = module.Setup<bool>("load", _ => true);
        var mount = module.Setup<bool>("mountDiff", _ => true);
        module.SetupVoid("goToDiff", _ => true).SetVoidResult();
        var viewer = Render<OmniDiffViewer>(parameters => parameters
            .Add(component => component.Original, "a")
            .Add(component => component.ShowNavigation, true));

        Assert.All(viewer.FindAll(".omni-diff-viewer__bar button"), button => Assert.True(button.HasAttribute("disabled")));

        load.SetResult(true);
        viewer.WaitForAssertion(() => Assert.Single(module.Invocations["mountDiff"]));
        mount.SetResult(true);
        viewer.WaitForAssertion(() => Assert.All(viewer.FindAll(".omni-diff-viewer__bar button"), button => Assert.False(button.HasAttribute("disabled"))));

        viewer.Find(".omni-diff-viewer__next").Click();
        viewer.Find(".omni-diff-viewer__previous").Click();

        viewer.WaitForAssertion(() => Assert.Equal(["next", "previous"], module.Invocations["goToDiff"].Select(call => call.Arguments[1])));
    }

    [Fact]
    public void ShowNavigation_WithThePlainTextEngine_KeepsTheButtonsDisabled()
    {
        var viewer = Render<OmniDiffViewer>(parameters => parameters
            .Add(component => component.Engine, OmniCodeEditorEngine.PlainText)
            .Add(component => component.ShowNavigation, true));

        Assert.All(viewer.FindAll(".omni-diff-viewer__bar button"), button => Assert.True(button.HasAttribute("disabled")));
        Assert.Empty(JSInterop.Invocations);
    }

    [Fact]
    public void ShowNavigation_ButtonsFollowTheEngine_WhenItIsSwitchedToPlainText()
    {
        var module = SetupMonaco();
        var viewer = Render<OmniDiffViewer>(parameters => parameters
            .Add(component => component.Original, "a")
            .Add(component => component.ShowNavigation, true));
        viewer.WaitForAssertion(() => Assert.All(viewer.FindAll(".omni-diff-viewer__bar button"), button => Assert.False(button.HasAttribute("disabled"))));

        viewer.Render(parameters => parameters.Add(component => component.Engine, OmniCodeEditorEngine.PlainText));

        Assert.All(viewer.FindAll(".omni-diff-viewer__bar button"), button => Assert.True(button.HasAttribute("disabled")));
        Assert.Single(module.Invocations["disposeDiff"]);
    }

    [Fact]
    public void FillAvailableHeight_IsOffByDefault_AndAddsTheFillClassWhenOn()
    {
        var plain = Render<OmniDiffViewer>(parameters => parameters.Add(component => component.Engine, OmniCodeEditorEngine.PlainText));
        var filling = Render<OmniDiffViewer>(parameters => parameters
            .Add(component => component.Engine, OmniCodeEditorEngine.PlainText)
            .Add(component => component.FillAvailableHeight, true));

        Assert.False(plain.Instance.FillAvailableHeight);
        Assert.DoesNotContain("omni-diff-viewer--fill", plain.Find(".omni-diff-viewer").ClassList);
        Assert.Contains("omni-diff-viewer--fill", filling.Find(".omni-diff-viewer").ClassList);
        Assert.Equal(2, filling.FindAll("section.omni-diff-viewer__pane").Count);
        Assert.DoesNotContain("style=", filling.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FillAvailableHeight_KeepsHeightAsTheMinimum_SetThroughTheScript()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var viewer = Render<OmniDiffViewer>(parameters => parameters
            .Add(component => component.Engine, OmniCodeEditorEngine.PlainText)
            .Add(component => component.FillAvailableHeight, true)
            .Add(component => component.Height, "12rem"));

        var height = Assert.Single(module.Invocations["setHeight"]);
        Assert.Equal("12rem", height.Arguments[1]);
        Assert.Contains("omni-diff-viewer--fill", viewer.Find(".omni-diff-viewer").ClassList);
        Assert.DoesNotContain("style=", viewer.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FillAvailableHeight_StillRefusesAnInvalidHeight()
    {
        Assert.Throws<ArgumentException>(() => Render<OmniDiffViewer>(parameters => parameters
            .Add(component => component.Engine, OmniCodeEditorEngine.PlainText)
            .Add(component => component.FillAvailableHeight, true)
            .Add(component => component.Height, "calc(1px)")));
    }

    [Fact]
    public void FillAvailableHeight_WithMonaco_MountsTheSameWay_AndKeepsTheBarOnTop()
    {
        var module = SetupMonaco();
        var viewer = Render<OmniDiffViewer>(parameters => parameters
            .Add(component => component.Original, "a")
            .Add(component => component.FillAvailableHeight, true)
            .Add(component => component.ShowNavigation, true));

        viewer.WaitForAssertion(() => Assert.Single(module.Invocations["mountDiff"]));
        var children = viewer.Find(".omni-diff-viewer").Children.Select(child => child.ClassList.First()).ToList();
        Assert.Equal(["omni-diff-viewer__bar", "omni-diff-viewer__host"], children);
    }
}
