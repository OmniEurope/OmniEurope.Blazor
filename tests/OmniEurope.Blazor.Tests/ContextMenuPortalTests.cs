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
    private const string FocusModule = "./_content/OmniEurope.Blazor/omni-focus.js";

    [Fact]
    public void SwitchingDirectlyToAnotherMenu_OpensItByItsOwnPopup()
    {
        var module = JSInterop.SetupModule(FocusModule);
        module.Mode = JSRuntimeMode.Loose;
        var host = Render<ContextMenuSwitchTestHost>();

        host.InvokeAsync(() => host.Instance.Open("a"));
        host.InvokeAsync(() => host.Instance.Open("b"));

        host.WaitForAssertion(() => Assert.Equal(2, module.Invocations["openContextMenu"].Count));
        var popupIds = module.Invocations["openContextMenu"].Select(call => call.Arguments[0]).ToList();
        Assert.Equal(["menu-a-menu", "menu-b-menu"], popupIds);
        // The element reference the old code passed is gone: nothing is handed over that the portal
        // may not have captured.
        Assert.Empty(module.Invocations["activateMenu"]);
        Assert.Single(module.Invocations["closeContextMenu"]);
        Assert.NotNull(host.Find(".omni-overlay-portal #menu-b-menu"));
        Assert.Empty(host.FindAll("#menu-a-menu"));
        Assert.All(module.Invocations["openContextMenu"], call =>
            Assert.False(string.IsNullOrEmpty(((ElementReference)call.Arguments[2]!).Id)));
    }

    [Fact]
    public void RightClick_OpensAtThePointer_AndASecondRightClickMovesIt()
    {
        var module = JSInterop.SetupModule(FocusModule);
        module.Mode = JSRuntimeMode.Loose;
        var host = Render<ContextMenuSwitchTestHost>();

        host.Find("#menu-a").ContextMenu(new MouseEventArgs { ClientX = 120, ClientY = 80 });
        host.WaitForAssertion(() => Assert.Single(module.Invocations["openContextMenu"]));
        Assert.Equal("a", host.Instance.OpenName);
        var first = module.Invocations["openContextMenu"][0].Arguments;
        Assert.Equal(120d, first[3]);
        Assert.Equal(80d, first[4]);

        host.Find("#menu-a").ContextMenu(new MouseEventArgs { ClientX = 300, ClientY = 40 });
        host.WaitForAssertion(() => Assert.Equal(2, module.Invocations["openContextMenu"].Count));
        var second = module.Invocations["openContextMenu"][1].Arguments;
        Assert.Equal(300d, second[3]);
        Assert.Equal(40d, second[4]);
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

        host.WaitForAssertion(() => Assert.Single(module.Invocations["openContextMenu"]));
        var arguments = module.Invocations["openContextMenu"][0].Arguments;
        Assert.Equal("menu-b-menu", arguments[0]);
        Assert.Null(arguments[3]);
        Assert.Null(arguments[4]);
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
        host.WaitForAssertion(() => Assert.Single(module.Invocations["openContextMenu"]));

        var reference = module.Invocations["openContextMenu"][0].Arguments[5]!;
        var interop = reference.GetType().GetProperty("Value")!.GetValue(reference)!;
        var dismiss = interop.GetType().GetMethod("DismissAsync", BindingFlags.Public | BindingFlags.Instance)!;
        Assert.Equal("OmniContextMenu.Dismiss", dismiss.GetCustomAttribute<JSInvokableAttribute>()!.Identifier);
        host.InvokeAsync(() => (Task)dismiss.Invoke(interop, null)!);

        host.WaitForAssertion(() =>
        {
            Assert.Null(host.Instance.OpenName);
            Assert.Empty(host.FindAll(".omni-overlay-portal"));
            Assert.Single(module.Invocations["closeContextMenu"]);
        });
    }

    [Fact]
    public void ItemsThatChangeWhileOpen_AreShownInThePortal()
    {
        var module = JSInterop.SetupModule(FocusModule);
        module.Mode = JSRuntimeMode.Loose;
        var host = Render<ContextMenuSwitchTestHost>();
        host.InvokeAsync(() => host.Instance.Open("a"));
        host.WaitForAssertion(() => Assert.Equal("a 0", host.Find("#menu-a-menu button").TextContent));

        host.InvokeAsync(host.Instance.Bump);

        host.WaitForAssertion(() => Assert.Equal("a 1", host.Find("#menu-a-menu button").TextContent));
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
            .AddChildContent("<button type=\"button\" role=\"menuitem\">Action</button>"));
        menu.Find("#inline").ContextMenu(new MouseEventArgs { ClientX = 5, ClientY = 6 });
        Assert.True(open);
        menu.Render(parameters => parameters.Add(component => component.Open, true));

        menu.Find("#inline-menu").KeyDown("ArrowDown");

        menu.WaitForAssertion(() => Assert.Single(module.Invocations["moveContextMenuFocus"]));
        Assert.Equal("inline-menu", module.Invocations["moveContextMenuFocus"][0].Arguments[0]);
    }
}
