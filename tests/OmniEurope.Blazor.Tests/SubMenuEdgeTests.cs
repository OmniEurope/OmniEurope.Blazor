using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The edges of <see cref="OmniSubMenu"/> that <see cref="SubMenuTests"/> does not reach: an entry with no
/// <c>Id</c> and no menu around it, a pen over the entry, an entry disabled while its list is open, a list
/// closed while its script is still loading, a lost circuit at each call to the script, and an entry
/// that is still opening when a sibling opens.
/// </summary>
public sealed class SubMenuEdgeTests : OmniBunitContext
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    private static MouseEventArgs Pointer => new() { Detail = 1 };

    private IRenderedComponent<OmniSubMenu> RenderEntry(string? id = null) => Render<OmniSubMenu>(parameters => parameters
        .Add(component => component.Id, id)
        .Add(component => component.Label, "Insert")
        .AddChildContent("the list"));

    private ManualJSRuntime UseRuntime(ManualJSRuntime? runtime = null)
    {
        runtime ??= new ManualJSRuntime();
        Services.AddSingleton<IJSRuntime>(runtime);
        return runtime;
    }

    [Fact]
    public void AnEntryWithoutAnId_NamesItselfAndItsListFromAGeneratedId()
    {
        var module = JSInterop.SetupModule(OmniModules.Focus);
        var entry = RenderEntry();
        var id = entry.Find("button").Id;
        Assert.StartsWith("omni-submenu-", id, StringComparison.Ordinal);
        Assert.False(entry.Find("button").HasAttribute("aria-controls"));

        entry.Find("button").Click(Pointer);

        var list = entry.Find("[role=menu]");
        Assert.Equal($"{id}-menu", list.Id);
        Assert.Equal(id, list.GetAttribute("aria-labelledby"));
        Assert.Equal($"{id}-menu", entry.Find("button").GetAttribute("aria-controls"));
        Assert.Equal("the list", list.TextContent.Trim());
        var placed = Assert.Single(module.Invocations["openSubMenu"]);
        Assert.Equal([id, $"{id}-menu", false], placed.Arguments.ToArray());
    }

    [Fact]
    public void AnEntryOutsideAnyMenu_OpensAndClosesAlone_AndOpensAgain()
    {
        var module = JSInterop.SetupModule(OmniModules.Focus);
        var entry = RenderEntry("lone");

        entry.Find("#lone").Click(Pointer);
        Assert.Equal("true", entry.Find("#lone").GetAttribute("aria-expanded"));
        entry.Find("#lone").Click(Pointer);

        Assert.Equal("false", entry.Find("#lone").GetAttribute("aria-expanded"));
        Assert.Empty(entry.FindAll("[role=menu]"));
        var left = Assert.Single(module.Invocations["leaveSubMenu"]);
        Assert.Equal(["lone-menu", "lone"], left.Arguments.Select(argument => (string?)argument));

        entry.Find("#lone").Click(Pointer);

        Assert.Equal("true", entry.Find("#lone").GetAttribute("aria-expanded"));
        Assert.Single(entry.FindAll("#lone-menu"));
        Assert.Equal(2, module.Invocations["openSubMenu"].Count);
    }

    [Fact]
    public void APenOverTheEntry_OpensItsListWithoutTheFocus_LikeAMouse()
    {
        var module = JSInterop.SetupModule(OmniModules.Focus);
        var entry = RenderEntry("pen");

        entry.Find("#pen").PointerEnter(new PointerEventArgs { PointerType = "pen" });

        Assert.Equal("true", entry.Find("#pen").GetAttribute("aria-expanded"));
        Assert.Single(entry.FindAll("#pen-menu"));
        var placed = Assert.Single(module.Invocations["openSubMenu"]);
        Assert.Equal(["pen", "pen-menu", false], placed.Arguments.ToArray());
    }

    [Fact]
    public void AnEntryDisabledWhileItsListIsOpen_ClosesIt_WithoutCallingTheScriptToLeave()
    {
        var module = JSInterop.SetupModule(OmniModules.Focus);
        var entry = RenderEntry("lock");
        entry.Find("#lock").Click(Pointer);
        Assert.Single(entry.FindAll("#lock-menu"));

        entry.Render(parameters => parameters.Add(component => component.Disabled, true));

        Assert.Empty(entry.FindAll("[role=menu]"));
        Assert.Equal("false", entry.Find("#lock").GetAttribute("aria-expanded"));
        Assert.True(entry.Find("#lock").HasAttribute("disabled"));
        Assert.False(entry.Find("#lock").HasAttribute("aria-controls"));
        Assert.DoesNotContain(module.Invocations, call => call.Identifier == "leaveSubMenu");
        entry.Render(parameters => parameters.Add(component => component.Disabled, false));
        entry.Find("#lock").Click(Pointer);
        Assert.Single(entry.FindAll("#lock-menu"));
    }

    [Fact]
    public async Task AListClosedWhileTheScriptIsStillLoading_Closes_WithoutAskingTheScriptToLeave()
    {
        var runtime = UseRuntime(new ManualJSRuntime { HoldImports = true });
        var entry = RenderEntry("slow");
        await entry.Find("#slow").ClickAsync(Pointer);
        Assert.Single(entry.FindAll("#slow-menu"));

        await entry.Find("#slow").ClickAsync(Pointer);

        Assert.Empty(entry.FindAll("[role=menu]"));
        Assert.Equal("false", entry.Find("#slow").GetAttribute("aria-expanded"));
        Assert.Empty(runtime.Module.Calls);
    }

    [Fact]
    public async Task ALostCircuit_WhilePlacingTheList_LeavesTheListOpenAndRaisesNothing()
    {
        var runtime = UseRuntime();
        runtime.Module.CallFailures["openSubMenu"] = new JSDisconnectedException("lost");
        var entry = RenderEntry("gone");

        await entry.Find("#gone").ClickAsync(Pointer);

        entry.WaitForAssertion(() => Assert.Contains("openSubMenu", runtime.Module.Calls));
        Assert.Equal("true", entry.Find("#gone").GetAttribute("aria-expanded"));
        Assert.Single(entry.FindAll("#gone-menu"));
        await entry.InvokeAsync(() => { });
    }

    [Fact]
    public async Task ALostCircuit_WhileLeavingTheList_StillClosesIt()
    {
        var runtime = UseRuntime();
        runtime.Module.CallFailures["leaveSubMenu"] = new JSDisconnectedException("lost");
        var entry = RenderEntry("gone");
        await entry.Find("#gone").ClickAsync(Pointer);

        await entry.Find("#gone").ClickAsync(Pointer);

        Assert.Contains("leaveSubMenu", runtime.Module.Calls);
        Assert.Empty(entry.FindAll("[role=menu]"));
        Assert.Equal("false", entry.Find("#gone").GetAttribute("aria-expanded"));
    }

    [Fact]
    public async Task ALostCircuit_WhileReleasingTheScript_DoesNotLetTheDisconnectionEscapeTheDisposal()
    {
        var runtime = UseRuntime(new ManualJSRuntime { Module = new RecordingModule { DisposeFailure = new JSDisconnectedException("lost") } });
        var entry = RenderEntry("gone");
        await entry.Find("#gone").ClickAsync(Pointer);
        entry.WaitForAssertion(() => Assert.Contains("openSubMenu", runtime.Module.Calls));

        await entry.Instance.DisposeAsync();

        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
    }

    [Fact]
    public async Task AnEntryStillOpening_WhenASiblingOpens_IsLeftAlone_AndTheSiblingOpens()
    {
        var runtime = UseRuntime();
        var host = Render<SubMenuLevelTestHost>(parameters => parameters.Add(component => component.Shared, true));
        await host.Find("#x").ClickAsync(Pointer);
        Assert.Equal("true", host.Find("#x").GetAttribute("aria-expanded"));
        // X leaves through the script, and the script is slow: A is registered as the open entry of the level
        // but is not open yet when B opens.
        var leave = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        runtime.Module.Gates["leaveSubMenu"] = leave;
        var openingA = host.Find("#a").ClickAsync(Pointer);
        Assert.False(openingA.IsCompleted);
        Assert.Equal("false", host.Find("#a").GetAttribute("aria-expanded"));

        await host.Find("#b").ClickAsync(Pointer);

        Assert.Equal("true", host.Find("#b").GetAttribute("aria-expanded"));
        Assert.Single(host.FindAll("#b-menu"));
        Assert.Equal("false", host.Find("#a").GetAttribute("aria-expanded"));
        Assert.Equal(1, runtime.Module.Calls.Count(call => call == "leaveSubMenu"));

        leave.SetResult();
        await openingA.WaitAsync(Limit, Xunit.TestContext.Current.CancellationToken);
        Assert.Equal("false", host.Find("#x").GetAttribute("aria-expanded"));
        Assert.Empty(host.FindAll("#x-menu"));
    }

    // The two guards below protect a close that reaches an entry which never loaded its script or whose script
    // was released on arrival. The level forgets an entry the moment it is closed or removed, so no sibling
    // can reach those states: the private close is called directly to hold what the guards do.

    [Fact]
    public async Task ACloseOnAnOpenEntryThatNeverLoadedTheScript_ClosesItAndCallsNothing()
    {
        JSInterop.SetupModule(OmniModules.Focus);
        var entry = RenderEntry("fresh");
        await entry.InvokeAsync(() => SetOpen(entry.Instance, true));
        entry.Render();
        Assert.Equal("true", entry.Find("#fresh").GetAttribute("aria-expanded"));

        await entry.InvokeAsync(() => CloseAsync(entry.Instance));

        Assert.Equal("false", entry.Find("#fresh").GetAttribute("aria-expanded"));
        Assert.Empty(entry.FindAll("[role=menu]"));
        Assert.DoesNotContain(JSInterop.Invocations, call => call.Identifier is "import" or "openSubMenu" or "leaveSubMenu");
    }

    [Fact]
    public async Task ACloseOnAnEntryRemovedWhileItsScriptLoaded_ClosesItAndCallsNothing()
    {
        var runtime = UseRuntime(new ManualJSRuntime { HoldImports = true });
        var host = Render<SubMenuLevelTestHost>();
        await host.Find("#x").ClickAsync(Pointer);
        var removed = host.FindComponents<OmniSubMenu>().First(entry => entry.Instance.Id == "x").Instance;
        host.Render(parameters => parameters.Add(component => component.ShowX, false));
        runtime.PendingImport.SetResult(runtime.Module);
        // The script arrived after the removal: the loader released it and answered null.
        var import = (Task<IJSObjectReference?>)typeof(OmniSubMenu).GetField("_import", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(removed)!;
        Assert.Null(await import.WaitAsync(Limit, Xunit.TestContext.Current.CancellationToken));
        Assert.True(runtime.Module.Disposal.Task.IsCompleted);

        await host.InvokeAsync(() => CloseAsync(removed));

        Assert.False((bool)typeof(OmniSubMenu).GetField("_open", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(removed)!);
        Assert.DoesNotContain("leaveSubMenu", runtime.Module.Calls);
        Assert.DoesNotContain("openSubMenu", runtime.Module.Calls);
    }

    private static void SetOpen(OmniSubMenu entry, bool open) =>
        typeof(OmniSubMenu).GetField("_open", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(entry, open);

    private static Task CloseAsync(OmniSubMenu entry) =>
        (Task)typeof(OmniSubMenu).GetMethod("CloseAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(entry, null)!;
}
