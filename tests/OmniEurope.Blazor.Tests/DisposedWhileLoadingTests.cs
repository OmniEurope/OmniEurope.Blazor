using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A component removed while the script it needs is still loading must leave nothing behind: no
/// document listener attached after its disposal, no .NET reference left to a listener, and the
/// module released when it arrives (L4-07 of the 2026-10-22 audit).
/// </summary>
public sealed class DisposedWhileLoadingTests : OmniBunitContext
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

    private ManualJSRuntime HoldImports()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        Services.AddSingleton<IJSRuntime>(runtime);
        return runtime;
    }

    private static async Task ReleasedWithoutAttachingAsync(ManualJSRuntime runtime)
    {
        runtime.PendingImport.SetResult(runtime.Module);
        await runtime.Module.Disposal.Task.WaitAsync(Patience, Xunit.TestContext.Current.CancellationToken);
        Assert.Empty(runtime.Module.Calls);
    }

    [Fact]
    public async Task Popover_RemovedWhileItsScriptLoads_AttachesNothing()
    {
        var runtime = HoldImports();
        var popover = Render<OmniPopover>(parameters => parameters
            .Add(component => component.Label, "Filtres")
            .Add(component => component.Open, true)
            .AddChildContent("panneau"));

        await popover.Instance.DisposeAsync();

        await ReleasedWithoutAttachingAsync(runtime);
    }

    [Fact]
    public async Task Sidebar_RemovedWhileItsScriptLoads_AttachesNothing()
    {
        var runtime = HoldImports();
        var sidebar = Render<OmniSidebar>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Reveal, OmniSidebarReveal.Overlay)
            .Add(component => component.OpenChanged, EventCallback.Factory.Create<bool>(this, _ => { }))
            .AddChildContent("Navigation"));

        await sidebar.Instance.DisposeAsync();

        await ReleasedWithoutAttachingAsync(runtime);
    }

    [Fact]
    public async Task Dialog_RemovedWhileItsScriptLoads_ActivatesNothing()
    {
        var runtime = HoldImports();
        var dialog = Render<OmniDialog>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Title, "Titre")
            .AddChildContent("Contenu"));

        await dialog.Instance.DisposeAsync();

        await ReleasedWithoutAttachingAsync(runtime);
    }

    [Fact]
    public async Task MenuController_RemovedWhileItsScriptLoads_ReleasesItOnArrival()
    {
        var runtime = HoldImports();
        var controller = new OmniMenuController(runtime, _ => Task.CompletedTask);
        controller.RequestPlacement(focusLast: false);

        var syncing = controller.SyncAsync(true, "menu", default, OmniMenuPlacement.Start);
        await controller.DisposeAsync();
        runtime.PendingImport.SetResult(runtime.Module);
        await syncing;

        await runtime.Module.Disposal.Task.WaitAsync(Patience, Xunit.TestContext.Current.CancellationToken);
        Assert.Empty(runtime.Module.Calls);
    }

    [Fact]
    public async Task MenuController_SyncedTwiceWhileItsScriptLoads_ImportsItOnce()
    {
        var runtime = new CountingImportRuntime();
        var controller = new OmniMenuController(runtime, _ => Task.CompletedTask);
        controller.RequestPlacement(focusLast: false);

        var first = controller.SyncAsync(true, "menu", default, OmniMenuPlacement.Start);
        controller.RequestPlacement(focusLast: false);
        var second = controller.SyncAsync(true, "menu", default, OmniMenuPlacement.Start);
        runtime.Arrive();
        await Task.WhenAll(first, second);
        await controller.DisposeAsync();

        Assert.Equal(1, runtime.Imports);
        Assert.Equal(1, runtime.Module.Disposals);
    }

    private ManualJSRuntime Install(ManualJSRuntime runtime)
    {
        Services.AddSingleton<IJSRuntime>(runtime);
        return runtime;
    }

    private static async Task SettledAsync() => await Task.Delay(200);

    [Fact]
    public async Task Sidebar_RemovedWhileItsChevronsAreConfigured_ListensToNothing()
    {
        var runtime = Install(new ManualJSRuntime());
        var gate = runtime.Module.Gates["configureSidebarOverflow"] = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var sidebar = Render<OmniSidebar>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Reveal, OmniSidebarReveal.Overlay)
            .Add(component => component.OpenChanged, EventCallback.Factory.Create<bool>(this, _ => { }))
            .AddChildContent("Navigation"));

        await sidebar.Instance.DisposeAsync();
        gate.SetResult();
        await SettledAsync();

        Assert.DoesNotContain("attachEscape", runtime.Module.Calls);
        Assert.DoesNotContain("watchNarrow", runtime.Module.Calls);
    }

    [Fact]
    public async Task Dialog_RenderedAgainAfterItsDisposal_ActivatesNothing()
    {
        var runtime = Install(new ManualJSRuntime());
        var dialog = Render<OmniDialog>(parameters => parameters
            .Add(component => component.Title, "Titre")
            .AddChildContent("Contenu"));
        await dialog.Instance.DisposeAsync();
        var before = runtime.Module.Calls.Count;

        dialog.Render(parameters => parameters.Add(component => component.Open, true));
        await SettledAsync();

        Assert.Equal(before, runtime.Module.Calls.Count);
    }

    [Fact]
    public async Task Dialog_RemovedWhileTheScriptOfItsWidthLoads_WritesNoWidth()
    {
        var runtime = Install(new ManualJSRuntime());
        var gate = runtime.ImportGates["omni-dialog.js"] = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var dialog = Render<OmniDialog>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Width, "30rem")
            .Add(component => component.Title, "Titre")
            .AddChildContent("Contenu"));

        await dialog.Instance.DisposeAsync();
        gate.SetResult();
        await SettledAsync();

        Assert.DoesNotContain("setWidth", runtime.Module.Calls);
    }

    [Fact]
    public async Task Dialog_RemovedWhileTheScriptOfItsDraggingLoads_AttachesNothing()
    {
        var runtime = Install(new ManualJSRuntime());
        var gate = runtime.ImportGates["omni-dialog.js"] = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var dialog = Render<OmniDialog>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Draggable, true)
            .Add(component => component.Title, "Titre")
            .AddChildContent("Contenu"));

        await dialog.Instance.DisposeAsync();
        gate.SetResult();
        await SettledAsync();

        Assert.DoesNotContain("attach", runtime.Module.Calls);
    }

    [Fact]
    public async Task Dialog_RenderedTwiceWhileItsScriptLoads_WritesTheWidthOnce()
    {
        var runtime = Install(new ManualJSRuntime());
        var gate = runtime.ImportGates["omni-dialog.js"] = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var dialog = Render<OmniDialog>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Width, "30rem")
            .Add(component => component.Title, "Titre")
            .AddChildContent("Contenu"));
        dialog.Render(parameters => parameters.Add(component => component.Width, "40rem"));

        gate.SetResult();
        await SettledAsync();

        Assert.Contains("setWidth", runtime.Module.Calls);
        Assert.Empty(runtime.Module.Calls.Where(call => call == "setWidth").Skip(2));
    }

    [Fact]
    public async Task Popover_WhoseCircuitIsGoneWhileItAttaches_StaysRendered()
    {
        var runtime = Install(new ManualJSRuntime());
        runtime.Module.CallFailures["attachPopover"] = new JSDisconnectedException("The circuit is gone.");

        var popover = Render<OmniPopover>(parameters => parameters
            .Add(component => component.Label, "Filtres")
            .Add(component => component.Open, true)
            .AddChildContent("panneau"));
        await SettledAsync();

        Assert.Contains("attachPopover", runtime.Module.Calls);
        Assert.Contains("panneau", popover.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MenuController_WhoseFirstImportFails_ImportsAgainOnTheNextSync()
    {
        var runtime = new FlakyImportRuntime();
        var controller = new OmniMenuController(runtime, _ => Task.CompletedTask);
        controller.RequestPlacement(focusLast: false);
        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.SyncAsync(true, "menu", default, OmniMenuPlacement.Start));

        await controller.SyncAsync(true, "menu", default, OmniMenuPlacement.Start);

        Assert.Equal(2, runtime.Imports);
        Assert.Contains("openMenu", runtime.Module.Calls);
        await controller.DisposeAsync();
    }

    [Fact]
    public async Task MenuController_SyncedTwiceThenRemovedWhileItsScriptLoads_ReleasesTheModuleOnce()
    {
        var runtime = new CountingImportRuntime();
        var controller = new OmniMenuController(runtime, _ => Task.CompletedTask);
        controller.RequestPlacement(focusLast: false);
        var first = controller.SyncAsync(true, "menu", default, OmniMenuPlacement.Start);
        controller.RequestPlacement(focusLast: false);
        var second = controller.SyncAsync(true, "menu", default, OmniMenuPlacement.Start);

        await controller.DisposeAsync();
        runtime.Arrive();
        await Task.WhenAll(first, second);

        Assert.Equal(1, runtime.Imports);
        Assert.Equal(1, runtime.Module.Disposals);
    }

    /// <summary>A runtime whose first import fails, as a lost network does, and whose later imports answer a recording module.</summary>
    private sealed class FlakyImportRuntime : IJSRuntime
    {
        public RecordingModule Module { get; } = new();

        public int Imports { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier != "import")
            {
                return ValueTask.FromResult(default(TValue)!);
            }

            return ++Imports == 1
                ? ValueTask.FromException<TValue>(new InvalidOperationException("The script could not be loaded."))
                : ValueTask.FromResult((TValue)(object)Module);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);
    }

    /// <summary>A runtime whose one import is held until <see cref="Arrive"/>, and which counts the imports asked.</summary>
    private sealed class CountingImportRuntime : IJSRuntime
    {
        private readonly TaskCompletionSource<IJSObjectReference> _arrival = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public CountedModule Module { get; } = new();

        public int Imports { get; private set; }

        public void Arrive() => _arrival.SetResult(Module);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier == "import")
            {
                Imports++;
                return new(_arrival.Task.ContinueWith(task => (TValue)task.Result, TaskScheduler.Default));
            }

            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);
    }

    private sealed class CountedModule : IJSObjectReference
    {
        public int Disposals { get; private set; }

        public ValueTask DisposeAsync()
        {
            Disposals++;
            return ValueTask.CompletedTask;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => ValueTask.FromResult(default(TValue)!);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);
    }
}
