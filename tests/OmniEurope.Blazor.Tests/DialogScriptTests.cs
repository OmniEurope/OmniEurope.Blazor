using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The scripts of OmniDialog: dragging and the frozen scale attached as it opens and detached as it
/// closes, the window without focus sentinels nor close button, and the release on a lost circuit.
/// </summary>
public sealed class DialogScriptTests : OmniBunitContext
{
    private IRenderedComponent<OmniDialog> RenderDialog(Action<ComponentParameterCollectionBuilder<OmniDialog>> parameters) =>
        Render<OmniDialog>(builder =>
        {
            builder.Add(dialog => dialog.Title, "Réglages").Add(dialog => dialog.Open, true);
            parameters(builder);
        });

    [Theory]
    [InlineData(true, false, new[] { "attach" })]
    [InlineData(false, true, new[] { "attach", "freezeScale" })]
    [InlineData(true, true, new[] { "attach", "freezeScale" })]
    public void OpeningDialog_AttachesWhatItAsks_AndClosingDetachesIt(bool draggable, bool freeze, string[] attached)
    {
        var module = JSInterop.SetupModule(OmniModules.Dialog);
        module.Mode = JSRuntimeMode.Loose;
        var dialog = RenderDialog(parameters => parameters.Add(component => component.Draggable, draggable).Add(component => component.FreezeScale, freeze));

        Assert.Equal(attached, module.Invocations.Select(call => call.Identifier));
        dialog.Render(parameters => parameters.Add(component => component.Open, true));
        Assert.Equal(attached.Length, module.Invocations.Count);

        dialog.Render(parameters => parameters.Add(component => component.Open, false));
        Assert.Equal("detach", module.Invocations.Last().Identifier);
        dialog.Render(parameters => parameters.Add(component => component.Open, false));
        Assert.Single(module.Invocations["detach"]);
    }

    [Fact]
    public void ModelessWindowWithoutCloseButton_HasNoSentinelAndNoCloseControl()
    {
        var dialog = RenderDialog(parameters => parameters.Add(component => component.Modal, false).Add(component => component.ShowClose, false));

        Assert.Empty(dialog.FindAll("[data-focus-sentinel]"));
        Assert.Empty(dialog.FindAll(".omni-dialog__close"));
    }

    [Fact]
    public async Task Dispose_ReleasesBothScripts_EvenOnALostCircuit()
    {
        var runtime = new ManualJSRuntime { Module = new RecordingModule { DisposeFailure = new JSDisconnectedException("perdu") } };
        Services.AddSingleton<IJSRuntime>(runtime);
        var dialog = RenderDialog(parameters => parameters.Add(component => component.Draggable, true));
        Assert.Contains("attach", runtime.Module.Calls);

        await dialog.Instance.DisposeAsync();

        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
        Assert.Contains("restoreFocus", runtime.Module.Calls);
        Assert.Contains("detach", runtime.Module.Calls);
    }
}
