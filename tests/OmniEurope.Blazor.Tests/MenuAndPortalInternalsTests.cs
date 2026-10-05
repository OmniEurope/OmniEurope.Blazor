using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The menu script controller and the overlay portal read on their own: a menu closed before it was
/// ever shown, a script lost while closing or releasing, a controller disposed while its script loads
/// or before it syncs, a coordinator nobody listens to, and a portal slot that follows only its owner
/// and changes coordinator.
/// </summary>
public sealed class MenuAndPortalInternalsTests : OmniBunitContext
{
    private static Task Open(OmniMenuController controller) =>
        controller.SyncAsync(true, "menu", default, OmniMenuPlacement.Start);

    [Fact]
    public async Task MenuClosedBeforeItWasShown_TellsTheScriptNothing()
    {
        var runtime = new ManualJSRuntime();
        var controller = new OmniMenuController(runtime, _ => Task.CompletedTask);

        await controller.SetOpenAsync(false, null, default, () => { });

        Assert.Empty(runtime.Module.Calls);
        await controller.DisposeAsync();
    }

    [Fact]
    public async Task MenuScriptLostWhileClosingOrReleasing_IsQuiet()
    {
        var runtime = new ManualJSRuntime { Module = new RecordingModule { DisposeFailure = new JSDisconnectedException("perdu") } };
        runtime.Module.CallFailures["closeMenu"] = new JSDisconnectedException("perdu");
        var controller = new OmniMenuController(runtime, _ => Task.CompletedTask);
        await Open(controller);

        await controller.SetOpenAsync(false, null, default, () => { });
        await Open(controller);
        await controller.DisposeAsync();

        Assert.Equal(["openMenu", "closeMenu", "openMenu", "closeMenu"], runtime.Module.Calls);
    }

    [Fact]
    public async Task MenuDisposedWhileItsScriptLoads_OrBeforeItSyncs_OpensNothing()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        var controller = new OmniMenuController(runtime, _ => Task.CompletedTask);

        var opening = Open(controller);
        await controller.DisposeAsync();
        runtime.PendingImport.SetResult(runtime.Module);
        await opening;
        await Open(controller);

        Assert.Empty(runtime.Module.Calls);
    }

    [Fact]
    public void Coordinator_WithNobodyListening_RegistersUpdatesAndUnregistersQuietly()
    {
        var coordinator = new OmniOverlayCoordinator();
        var owner = new object();

        coordinator.Register(owner, OmniPortalKind.ContextMenu, builder => builder.AddContent(0, "a"), () => Task.CompletedTask);
        coordinator.Register(owner, OmniPortalKind.ContextMenu, builder => builder.AddContent(0, "b"), () => Task.CompletedTask);
        coordinator.Unregister(owner);

        Assert.Empty(coordinator.Entries);
    }

    [Fact]
    public void PortalSlot_DisposedBeforeItsFirstParameters_HasNothingToLeave()
    {
        var slot = new OmniPortalSlot();

        slot.Dispose();

        Assert.Null(slot.Coordinator);
    }

    [Fact]
    public void PortalSlot_FollowsOnlyItsOwner_AndLeavesACoordinatorItNoLongerShows()
    {
        var first = new OmniOverlayCoordinator();
        var second = new OmniOverlayCoordinator();
        var owner = new object();
        var other = new object();
        var slot = Render<OmniPortalSlot>(parameters => parameters
            .Add(component => component.Coordinator, first)
            .Add(component => component.Owner, owner));
        Assert.Equal(string.Empty, slot.Markup);

        first.Register(owner, OmniPortalKind.ContextMenu, builder => builder.AddContent(0, "un"), () => Task.CompletedTask);
        first.Register(other, OmniPortalKind.ContextMenu, builder => builder.AddContent(0, "autre"), () => Task.CompletedTask);
        first.Register(other, OmniPortalKind.ContextMenu, builder => builder.AddContent(0, "autre 2"), () => Task.CompletedTask);
        first.Register(owner, OmniPortalKind.ContextMenu, builder => builder.AddContent(0, "deux"), () => Task.CompletedTask);
        slot.WaitForAssertion(() => Assert.Equal("deux", slot.Markup));

        slot.Render(parameters => parameters.Add(component => component.Coordinator, second));
        Assert.Equal(string.Empty, slot.Markup);
        first.Register(owner, OmniPortalKind.ContextMenu, builder => builder.AddContent(0, "trois"), () => Task.CompletedTask);
        Assert.Equal(string.Empty, slot.Markup);
    }
}
