using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The context menu as the recette found it: a right-click that showed nothing (the portal placed
/// the menu at 100 % of the viewport height, under its bottom edge) and a crash when a second menu
/// opened where the first had been (the portal reused the element, the new menu's reference was
/// never captured, and the script received an empty object).
/// </summary>
public sealed class ContextMenuPortalTests : OmniBunitContext
{
    private const string FocusModule = Internal.OmniModules.Focus;

    [Fact]
    public void SwitchingDirectlyToAnotherMenu_OpensItByItsOwnPopup()
    {
        var module = JSInterop.SetupModule(FocusModule);
        module.Mode = JSRuntimeMode.Loose;
        var host = Render<ContextMenuSwitchTestHost>();

        host.InvokeAsync(() => host.Instance.Open("a"));
        host.InvokeAsync(() => host.Instance.Open("b"));

        host.WaitForAssertion(() => Assert.Equal(2, module.Invocations["openMenu"].Count));
        var popupIds = module.Invocations["openMenu"].Select(call => call.Arguments[0]).ToList();
        Assert.Equal(["menu-a-menu", "menu-b-menu"], popupIds);
        // The menu is found by id: nothing is handed over that the portal may not have captured.
        Assert.Single(module.Invocations["closeMenu"]);
        Assert.NotNull(host.Find(".omni-overlay-portal #menu-b-menu"));
        Assert.Empty(host.FindAll("#menu-a-menu"));
        Assert.All(module.Invocations["openMenu"], call =>
            Assert.False(string.IsNullOrEmpty(((ElementReference)call.Arguments[2]!).Id)));
    }

    [Fact]
    public void RightClick_OpensAtThePointer_AndASecondRightClickMovesIt()
    {
        var module = JSInterop.SetupModule(FocusModule);
        module.Mode = JSRuntimeMode.Loose;
        var host = Render<ContextMenuSwitchTestHost>();

        host.Find("#menu-a").ContextMenu(new MouseEventArgs { ClientX = 120, ClientY = 80 });
        host.WaitForAssertion(() => Assert.Single(module.Invocations["openMenu"]));
        Assert.Equal("a", host.Instance.OpenName);
        var first = module.Invocations["openMenu"][0].Arguments;
        Assert.Equal("pointer", first[3]);
        Assert.Equal(120d, first[4]);
        Assert.Equal(80d, first[5]);

        host.Find("#menu-a").ContextMenu(new MouseEventArgs { ClientX = 300, ClientY = 40 });
        host.WaitForAssertion(() => Assert.Equal(2, module.Invocations["openMenu"].Count));
        var second = module.Invocations["openMenu"][1].Arguments;
        Assert.Equal(300d, second[4]);
        Assert.Equal(40d, second[5]);
        Assert.Equal("a", host.Instance.OpenName);
        Assert.Single(host.FindAll(".omni-overlay-portal .omni-context-menu__popup"));
    }

    [Theory]
    [InlineData("F10", true)]
    [InlineData("ContextMenu", false)]
    public void Keyboard_OpensUnderTheTrigger(string key, bool shift)
    {
        var module = JSInterop.SetupModule(FocusModule);
        module.Mode = JSRuntimeMode.Loose;
        var host = Render<ContextMenuSwitchTestHost>();

        host.Find("#menu-b").KeyDown(new KeyboardEventArgs { Key = key, ShiftKey = shift });

        host.WaitForAssertion(() => Assert.Single(module.Invocations["openMenu"]));
        var arguments = module.Invocations["openMenu"][0].Arguments;
        Assert.Equal("menu-b-menu", arguments[0]);
        Assert.Null(arguments[4]);
        Assert.Null(arguments[5]);
        Assert.Equal("menu", host.Find("#menu-b-menu").GetAttribute("role"));
        Assert.Null(host.Find("#menu-b-menu").GetAttribute("autofocus"));
    }

    [Fact]
    public void APressOutside_ReportedByTheScript_ClosesTheMenu()
    {
        var module = JSInterop.SetupModule(FocusModule);
        module.Mode = JSRuntimeMode.Loose;
        var host = Render<ContextMenuSwitchTestHost>();
        host.Find("#menu-a").ContextMenu(new MouseEventArgs { ClientX = 10, ClientY = 10 });
        host.WaitForAssertion(() => Assert.Single(module.Invocations["openMenu"]));

        var reference = module.Invocations["openMenu"][0].Arguments[8]!;
        var interop = reference.GetType().GetProperty("Value")!.GetValue(reference)!;
        var dismiss = interop.GetType().GetMethod("DismissAsync", BindingFlags.Public | BindingFlags.Instance)!;
        Assert.Equal("OmniMenu.Dismiss", dismiss.GetCustomAttribute<JSInvokableAttribute>()!.Identifier);
        host.InvokeAsync(() => (Task)dismiss.Invoke(interop, [false])!);

        host.WaitForAssertion(() =>
        {
            Assert.Null(host.Instance.OpenName);
            Assert.Empty(host.FindAll(".omni-overlay-portal"));
            Assert.Single(module.Invocations["closeMenu"]);
        });
    }

    [Fact]
    public void ItemsThatChangeWhileOpen_AreShownInThePortal()
    {
        var module = JSInterop.SetupModule(FocusModule);
        module.Mode = JSRuntimeMode.Loose;
        var host = Render<ContextMenuSwitchTestHost>();
        host.InvokeAsync(() => host.Instance.Open("a"));
        host.WaitForAssertion(() => Assert.Equal("a 0", host.Find("#menu-a-menu .omni-menu__label").TextContent));

        host.InvokeAsync(host.Instance.Bump);

        host.WaitForAssertion(() => Assert.Equal("a 1", host.Find("#menu-a-menu .omni-menu__label").TextContent));
    }

    [Fact]
    public void Trigger_OpensOnShiftF10Only()
    {
        JSInterop.SetupModule(FocusModule).Mode = JSRuntimeMode.Loose;
        var opened = new List<bool>();
        var menu = Render<OmniContextMenu>(parameters => parameters
            .Add(component => component.Id, "cible")
            .Add(component => component.OpenChanged, value => opened.Add(value))
            .Add(component => component.TriggerContent, (RenderFragment)(builder => builder.AddContent(0, "Cible")))
            .AddChildContent<OmniMenuItem>(item => item.AddChildContent("Action")));

        menu.Find("#cible").KeyDown(new KeyboardEventArgs { Key = "F10" });
        menu.Find("#cible").KeyDown(new KeyboardEventArgs { Key = "a", ShiftKey = true });
        Assert.Empty(opened);

        menu.Find("#cible").KeyDown(new KeyboardEventArgs { Key = "F10", ShiftKey = true });
        Assert.Equal([true], opened);
    }

    [Fact]
    public void WithoutAPortal_ThePopupRendersInPlaceAndItsKeysAreNotHandledTwice()
    {
        var module = JSInterop.SetupModule(FocusModule);
        module.Mode = JSRuntimeMode.Loose;
        var open = false;
        var menu = Render<OmniContextMenu>(parameters => parameters
            .Add(component => component.Id, "inline")
            .Add(component => component.Open, open)
            .Add(component => component.OpenChanged, value => open = value)
            .Add(component => component.TriggerContent, (RenderFragment)(builder => builder.AddContent(0, "Cible")))
            .AddChildContent<OmniMenuItem>(item => item.AddChildContent("Action")));
        menu.Find("#inline").ContextMenu(new MouseEventArgs { ClientX = 5, ClientY = 6 });
        Assert.True(open);
        menu.Render(parameters => parameters.Add(component => component.Open, true));

        menu.Find("#inline-menu").KeyDown("ArrowDown");

        menu.WaitForAssertion(() => Assert.Single(module.Invocations["moveMenuFocus"]));
        Assert.Equal("inline-menu", module.Invocations["moveMenuFocus"][0].Arguments[0]);
    }
}
