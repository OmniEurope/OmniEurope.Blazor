using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The circuit is gone while <see cref="OmniConnectionOverlay"/> loads or calls its focus script (which is
/// exactly when the overlay shows): the overlay keeps showing, nothing escapes as an unhandled error, and the
/// focus handling is tried again on the next render.
/// </summary>
public sealed class ConnectionOverlayLostCircuitTests : OmniBunitContext
{
    private ManualJSRuntime UseRuntime(ManualJSRuntime runtime)
    {
        Services.AddSingleton<IJSRuntime>(runtime);
        return runtime;
    }

    /// <summary>The overlay, reconnecting, over a runtime whose <paramref name="call"/> meets a lost circuit.</summary>
    private (ManualJSRuntime Runtime, IRenderedComponent<OmniConnectionOverlay> Overlay) RenderReconnectingWhileLosing(string call)
    {
        var runtime = UseRuntime(new ManualJSRuntime());
        runtime.Module.CallFailures[call] = new JSDisconnectedException("lost");
        var overlay = Render<OmniConnectionOverlay>(parameters => parameters
            .Add(component => component.State, OmniConnectionState.Reconnecting));
        return (runtime, overlay);
    }

    [Fact]
    public async Task ALostCircuit_WhileLoadingTheFocusScript_StillShowsTheOverlay_AndCallsNothing()
    {
        var runtime = UseRuntime(new ManualJSRuntime { ImportFailure = new JSDisconnectedException("lost") });

        var overlay = Render<OmniConnectionOverlay>(parameters => parameters
            .Add(component => component.State, OmniConnectionState.Reconnecting));
        overlay.Render(parameters => parameters.Add(component => component.TimeUntilRetry, TimeSpan.FromSeconds(5)));
        await overlay.InvokeAsync(() => { });

        Assert.NotEmpty(overlay.FindAll(".omni-connection-overlay__card--reconnecting"));
        Assert.Empty(runtime.Module.Calls);
    }

    [Fact]
    public async Task ALostCircuit_WhileActivatingTheFocusTrap_StillShowsTheOverlay_AndTriesAgainOnTheNextRender()
    {
        var (runtime, overlay) = RenderReconnectingWhileLosing("activateDialog");

        Assert.Equal(1, runtime.Module.Calls.Count(call => call == "activateDialog"));
        Assert.NotEmpty(overlay.FindAll(".omni-connection-overlay__card--reconnecting"));

        // The trap was not set, so it is asked for again; no restore is asked for what was never trapped.
        overlay.Render(parameters => parameters.Add(component => component.TimeUntilRetry, TimeSpan.FromSeconds(5)));
        await overlay.InvokeAsync(() => { });

        Assert.Equal(2, runtime.Module.Calls.Count(call => call == "activateDialog"));
        overlay.Render(parameters => parameters.Add(component => component.State, OmniConnectionState.Connected));
        Assert.DoesNotContain("restoreFocus", runtime.Module.Calls);
        Assert.Equal(string.Empty, overlay.Markup.Trim());
    }

    [Fact]
    public async Task ALostCircuit_WhileRestoringTheFocus_StillHidesTheOverlay()
    {
        var (runtime, overlay) = RenderReconnectingWhileLosing("restoreFocus");
        Assert.Equal(1, runtime.Module.Calls.Count(call => call == "activateDialog"));

        overlay.Render(parameters => parameters.Add(component => component.State, OmniConnectionState.Connected));
        await overlay.InvokeAsync(() => { });

        Assert.Contains("restoreFocus", runtime.Module.Calls);
        Assert.Equal(string.Empty, overlay.Markup.Trim());
    }
}
