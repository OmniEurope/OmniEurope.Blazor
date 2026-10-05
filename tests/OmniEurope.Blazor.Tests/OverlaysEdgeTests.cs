using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The overlays at their edges: a host that trades its own service for the host's, menus locked or asked
/// with other keys, a notification of a few seconds without a close button, dialogs closed when none is
/// open, a notification held while read, a popover closed by code or dismissed twice, and the tooltip
/// scripts released on a lost circuit or while they load.
/// </summary>
public sealed class OverlaysEdgeTests : OmniBunitContext
{
    [Fact]
    public void Host_KeepsItsOwnService_ThenTakesTheOneGiven()
    {
        var host = Render<OmniComponentsHost>(parameters => parameters.AddChildContent("Application"));
        host.Render(parameters => parameters.AddChildContent("Application encore"));

        using var service = new OmniOverlayService(TimeProvider.System);
        host.Render(parameters => parameters.Add(component => component.OverlayService, service));
        service.Notify("Bonjour");

        host.WaitForAssertion(() => Assert.Single(host.FindAll(".omni-notification")));
    }

    [Fact]
    public void Menus_LockedOrAskedWithOtherKeys_StayClosed()
    {
        JSInterop.SetupModule(OmniModules.Focus).Mode = JSRuntimeMode.Loose;
        var opened = new List<bool>();
        var context = Render<OmniContextMenu>(parameters => parameters
            .Add(component => component.Disabled, true)
            .Add(component => component.OpenChanged, open => opened.Add(open))
            .Add(component => component.TriggerContent, builder => builder.AddContent(0, "zone"))
            .AddChildContent("menu"));
        context.Find(".omni-context-menu").KeyDown(new KeyboardEventArgs { Key = "F10", ShiftKey = true });
        context.Find(".omni-context-menu").KeyDown(new KeyboardEventArgs { Key = "F10" });

        var overflow = Render<OmniOverflowMenu>(parameters => parameters
            .Add(component => component.OpenChanged, open => opened.Add(open))
            .AddChildContent("menu"));
        overflow.Find(".omni-overflow-menu__trigger").KeyDown("Enter");
        overflow.Find(".omni-overflow-menu__trigger").KeyDown("ArrowDown");
        overflow.Find(".omni-overflow-menu__trigger").KeyDown("ArrowDown");

        var locked = Render<OmniOverflowMenu>(parameters => parameters
            .Add(component => component.Disabled, true)
            .Add(component => component.OpenChanged, open => opened.Add(open))
            .AddChildContent("menu"));
        locked.Find(".omni-overflow-menu__trigger").KeyDown("ArrowDown");

        Assert.Equal([true], opened);
    }

    [Fact]
    public void Notification_OfAFewSeconds_WithoutCloseButton_AndAnActionTextAlone()
    {
        var notification = Render<OmniNotification>(parameters => parameters
            .Add(component => component.Duration, TimeSpan.FromSeconds(4))
            .Add(component => component.Dismissible, false)
            .Add(component => component.ActionText, "Annuler")
            .Add(component => component.Message, "Enregistré"));

        Assert.Contains("omni-notification--u4", notification.Find(".omni-notification").ClassList);
        Assert.DoesNotContain(notification.Find(".omni-notification").ClassList, name => System.Text.RegularExpressions.Regex.IsMatch(name, @"^omni-notification--t\d+$"));
        Assert.Empty(notification.FindAll(".omni-notification__dismiss"));
        Assert.Empty(notification.FindAll(".omni-notification__action"));
    }

    [Fact]
    public void Service_ClosingWhenNoDialogIsOpen_ChangesNothing()
    {
        using var service = new OmniOverlayService();
        var changes = 0;
        service.Changed += () => changes++;

        service.CloseDialog();
        service.CloseDialog("résultat");

        Assert.Equal(0, changes);
        Assert.Null(service.Dialog);
    }

    [Fact]
    public void HostedNotification_IsHeldWhileThePointerIsOnIt()
    {
        var clock = new ManualTimeProvider();
        using var service = new OmniOverlayService(clock, defaultNotificationDuration: TimeSpan.FromSeconds(5));
        var host = Render<OmniComponentsHost>(parameters => parameters.Add(component => component.OverlayService, service));
        service.Notify("Lu lentement");
        host.WaitForAssertion(() => Assert.Single(host.FindAll(".omni-notification")));

        host.Find(".omni-notification").MouseEnter();
        clock.Advance(TimeSpan.FromSeconds(10));
        Assert.Single(service.Notifications);
        host.Find(".omni-notification").MouseLeave();
        clock.Advance(TimeSpan.FromSeconds(10));

        host.WaitForAssertion(() => Assert.Empty(service.Notifications));
    }

    [Fact]
    public async Task Popover_ClosedByCode_DismissedTwice_AndReleasedOnALostCircuit()
    {
        var runtime = new ManualJSRuntime();
        runtime.Module.CallFailures["detachPopover"] = new JSDisconnectedException("perdu");
        Services.AddSingleton<IJSRuntime>(runtime);
        var popover = Render<OmniPopover>(parameters => parameters.Add(component => component.Label, "Filtres").AddChildContent("panneau"));

        popover.Find("button").Click();
        Assert.Contains("attachPopover", runtime.Module.Calls);
        await popover.InvokeAsync(popover.Instance.CloseAsync);
        await popover.InvokeAsync(() => popover.Instance.OnDismissRequestedAsync(fromKeyboard: true));
        popover.Find("button").Click();
        await popover.Instance.DisposeAsync();

        Assert.Equal(["attachPopover", "detachPopover", "attachPopover", "detachPopover"], runtime.Module.Calls);
    }

    [Fact]
    public async Task TitleTooltips_RenderedAgain_InstallOnce_AndLeaveQuietlyOnALostCircuit()
    {
        var runtime = new ManualJSRuntime();
        runtime.Module.CallFailures["uninstallTitleTooltips"] = new JSDisconnectedException("perdu");
        Services.AddSingleton<IJSRuntime>(runtime);
        var tooltips = Render<OmniTitleTooltips>();
        tooltips.Render();

        await tooltips.Instance.DisposeAsync();

        Assert.Equal(["installTitleTooltips", "uninstallTitleTooltips"], runtime.Module.Calls);
    }

    [Fact]
    public async Task TitleTooltipsGoneWhileTheirScriptLoads_ReleaseItOnArrival()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        Services.AddSingleton<IJSRuntime>(runtime);
        var tooltips = Render<OmniTitleTooltips>();

        await tooltips.Instance.DisposeAsync();
        runtime.PendingImport.SetResult(runtime.Module);

        await runtime.Module.Disposal.Task.WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);
        Assert.Empty(runtime.Module.Calls);
    }

    [Fact]
    public async Task TooltipScript_ReleasedBeforeInstallOrOnALostCircuit_IsQuiet()
    {
        var runtime = new ManualJSRuntime { Module = new RecordingModule { DisposeFailure = new JSDisconnectedException("perdu") } };
        await new OmniTooltipInterop(runtime).DisposeAsync();

        var installed = new OmniTooltipInterop(runtime);
        await installed.EnsureInstalledAsync();
        await installed.DisposeAsync();

        Assert.Equal(["install"], runtime.Module.Calls);
        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
    }

    [Fact]
    public void Tooltip_PreviewOfAnEarlySpace_IsCutAtTheLimit()
    {
        var tooltip = Render<OmniTooltip>(parameters => parameters
            .Add(component => component.Text, "Un mot-tres-long-sans-espace-qui-depasse")
            .Add(component => component.CompactLength, 12)
            .AddChildContent("?"));

        Assert.Equal("Un mot-tres-…", tooltip.Find(".omni-tooltip__text").TextContent);
    }
}
