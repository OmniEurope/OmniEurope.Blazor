using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The script runtimes the lifecycle suites install: imports held until the test releases them, a Monaco
/// load the test answers, and a Monaco whose release fails as on a lost circuit.
/// </summary>
internal static class ScriptLoadTestSetup
{
    /// <summary>A runtime installed for the test, whose imports wait until <see cref="ReleasedWithoutAttachingAsync"/>.</summary>
    internal static ManualJSRuntime InstallHeldImports(this BunitContext context)
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        context.Services.AddSingleton<IJSRuntime>(runtime);
        return runtime;
    }

    /// <summary>Lets the held import arrive, then proves the module was released without any call.</summary>
    internal static async Task ReleasedWithoutAttachingAsync(this ManualJSRuntime runtime, TimeSpan patience)
    {
        runtime.PendingImport.SetResult(runtime.Module);
        await runtime.Module.Disposal.Task.WaitAsync(patience, Xunit.TestContext.Current.CancellationToken);
        Assert.Empty(runtime.Module.Calls);
    }

    /// <summary>
    /// Lets the held import arrive and waits for the module's release: the module was never asked to
    /// attach, other calls on the same runtime (a tooltip's own script) aside.
    /// </summary>
    internal static async Task ReleasedWithoutTheAttachCallAsync(this ManualJSRuntime runtime, TimeSpan patience)
    {
        runtime.PendingImport.SetResult(runtime.Module);
        await runtime.Module.Disposal.Task.WaitAsync(patience, Xunit.TestContext.Current.CancellationToken);
        Assert.DoesNotContain("attach", runtime.Module.Calls);
    }

    /// <summary>Disposes <paramref name="component"/> while its import waits, then lets the import arrive and waits for both the release and the disposal.</summary>
    internal static async Task DisposeWhileTheImportWaitsAsync(this ManualJSRuntime runtime, IAsyncDisposable component, TimeSpan patience)
    {
        var disposing = component.DisposeAsync().AsTask();
        runtime.PendingImport.SetResult(runtime.Module);
        await runtime.Module.Disposal.Task.WaitAsync(patience, Xunit.TestContext.Current.CancellationToken);
        await disposing;
    }

    /// <summary>The code editor module in loose mode, whose Monaco load waits until the test answers it.</summary>
    internal static (BunitJSModuleInterop Module, JSRuntimeInvocationHandler<bool> Load) SetupHeldMonacoLoad(this BunitJSInterop interop)
    {
        var module = interop.SetupLooseModule(OmniModules.CodeEditor);
        return (module, module.Setup<bool>("load", _ => true));
    }

    /// <summary>Removes <paramref name="gone"/>, switches <paramref name="switched"/> with <paramref name="switchEngine"/>, then lets the held load succeed.</summary>
    internal static async Task RemoveOneSwitchTheOtherThenLoadAsync<TComponent>(
        IRenderedComponent<TComponent> gone,
        IRenderedComponent<TComponent> switched,
        Action<ComponentParameterCollectionBuilder<TComponent>> switchEngine,
        JSRuntimeInvocationHandler<bool> load)
        where TComponent : Microsoft.AspNetCore.Components.IComponent, IAsyncDisposable
    {
        await gone.Instance.DisposeAsync();
        switched.Render(switchEngine);
        load.SetResult(true);
    }

    /// <summary>
    /// A runtime installed for the test whose Monaco loads and mounts through <paramref name="mountCall"/>,
    /// and whose module release fails as on a lost circuit.
    /// </summary>
    internal static ManualJSRuntime InstallMonacoWithALostRelease(this BunitContext context, string mountCall)
    {
        var runtime = new ManualJSRuntime { Module = new RecordingModule { DisposeFailure = new JSDisconnectedException("perdu") } };
        runtime.Module.Answers["load"] = true;
        runtime.Module.Answers[mountCall] = true;
        context.Services.AddSingleton<IJSRuntime>(runtime);
        return runtime;
    }
}
