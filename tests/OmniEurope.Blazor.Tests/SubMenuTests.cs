using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniSubMenu"/> in the four menus of the package: the entry announces its menu, a click
/// toggles it without closing the menu nor raising an action, a leaf at any depth closes the whole menu
/// with the focus back on the trigger and then raises its action, and opening a list closes the one that
/// was open at the same level. The keys, the placement and the focus run in <c>omni-focus.js</c>
/// (tests/js/submenus.test.mjs and the Chromium probe): these tests hold the calls made to it.
/// </summary>
public sealed class SubMenuTests : OmniBunitContext
{
    public static TheoryData<string> Menus => new() { "overflow", "split", "profile", "context" };

    [Theory]
    [MemberData(nameof(Menus))]
    public void TheEntry_AnnouncesItsMenu_AndOwnsANestedMenuNamedByIt(string kind)
    {
        var (host, _) = Open(kind);

        var entry = host.Find("#insert");
        Assert.Equal("button", entry.LocalName);
        Assert.Equal("menuitem", entry.GetAttribute("role"));
        Assert.Equal("menu", entry.GetAttribute("aria-haspopup"));
        Assert.Equal("false", entry.GetAttribute("aria-expanded"));
        Assert.False(entry.HasAttribute("aria-controls"));
        Assert.Empty(host.FindAll("#insert-menu"));
        Assert.Equal("Insert", host.Find("#insert .omni-menu__label").TextContent);
        Assert.NotNull(host.Find("#insert .omni-menu__icon svg"));
        Assert.NotNull(host.Find("#insert .omni-menu__chevron svg"));
        Assert.Equal("true", host.Find("#insert .omni-menu__chevron").GetAttribute("aria-hidden"));

        entry.Click(new MouseEventArgs { Detail = 1 });

        entry = host.Find("#insert");
        Assert.Equal("true", entry.GetAttribute("aria-expanded"));
        Assert.Equal("insert-menu", entry.GetAttribute("aria-controls"));
        var nested = host.Find("#insert-menu");
        Assert.Equal("menu", nested.GetAttribute("role"));
        Assert.Equal("insert", nested.GetAttribute("aria-labelledby"));
        Assert.Equal("-1", nested.GetAttribute("tabindex"));
        Assert.False(nested.HasAttribute("aria-label"));
        Assert.Contains("omni-menu__submenu", nested.ClassName);
        Assert.Equal(["Before", "After", "More"], host.FindAll("#insert-menu [role=menuitem]").Where(item => item.Closest("[role=menu]")!.Id == "insert-menu").Select(item => item.TextContent.Trim()));
    }

    [Theory]
    [MemberData(nameof(Menus))]
    public void AClickOnTheEntry_TogglesItsMenu_WithoutClosingTheMenuNorRaisingAnAction(string kind)
    {
        var (host, module) = Open(kind);

        ClickTheEntryOpenThenClosed(host);

        Assert.Empty(host.FindAll("#insert-menu"));
        Assert.NotNull(host.Find("#m-menu"));
        Assert.DoesNotContain(module.Invocations, call => call.Identifier == "closeMenu");
        Assert.Empty(Chosen(host));
    }

    [Theory]
    [MemberData(nameof(Menus))]
    public void ALeafOfTheMenu_ClosesTheWholeMenuWithTheFocusBack_BeforeItsActionRuns(string kind)
    {
        var module = JSInterop.SetupModule(OmniModules.Focus);
        var closesWhenRun = new List<int>();
        var host = Render<SubMenuTestHost>(parameters => parameters
            .Add(component => component.Kind, kind)
            .Add(component => component.OnChosen, _ => closesWhenRun.Add(module.Invocations["closeMenu"].Count)));
        OpenMenu(host, kind);
        ClickEntry(host, "insert");

        host.Find("#before").Click();

        Assert.Equal([1], closesWhenRun);
        Assert.Equal(true, module.Invocations["closeMenu"][0].Arguments[1]);
        Assert.Empty(host.FindAll("[role=menu]"));
        Assert.Equal(["Before"], Chosen(host));
    }

    [Fact]
    public void ALeafOfADeeperLevel_ClosesEveryLevel_AndRaisesItsAction()
    {
        var (host, module) = Open("overflow");
        ClickEntry(host, "insert");
        ClickEntry(host, "more");
        Assert.Equal(3, host.FindAll("[role=menu]").Count);
        Assert.Equal("more-menu", host.Find("#more").GetAttribute("aria-controls"));
        Assert.NotNull(host.Find("#insert-menu #more-menu"));

        host.Find("#deep").Click();

        Assert.Empty(host.FindAll("[role=menu]"));
        Assert.Equal(["Deep"], Chosen(host));
        Assert.Equal(true, module.Invocations["closeMenu"].Single().Arguments[1]);
    }

    [Fact]
    public void ADisabledEntry_StaysListedDisabled_AndDoesNotOpen()
    {
        var (host, module) = Open("overflow");

        var locked = host.Find("#locked");
        Assert.True(locked.HasAttribute("disabled"));
        locked.Click(new MouseEventArgs { Detail = 1 });
        locked.PointerEnter(new PointerEventArgs { PointerType = "mouse" });

        Assert.Equal("false", host.Find("#locked").GetAttribute("aria-expanded"));
        Assert.Empty(host.FindAll("#locked-menu"));
        Assert.DoesNotContain(module.Invocations, call => call.Identifier == "openSubMenu");
    }

    [Fact]
    public void OpeningAnEntry_ClosesTheSiblingThatWasOpen()
    {
        var (host, module) = Open("overflow");
        ClickEntry(host, "insert");
        Assert.Single(host.FindAll("#insert-menu"));

        ClickEntry(host, "type");

        Assert.Empty(host.FindAll("#insert-menu"));
        Assert.Equal("false", host.Find("#insert").GetAttribute("aria-expanded"));
        Assert.Single(host.FindAll("#type-menu"));
        Assert.Equal("true", host.Find("#type").GetAttribute("aria-expanded"));
        var left = Assert.Single(module.Invocations["leaveSubMenu"]);
        Assert.Equal(["insert-menu", "insert"], left.Arguments.Select(argument => (string?)argument));
    }

    [Fact]
    public void OpeningAnEntryOfADeeperLevel_KeepsTheEntryOfTheLevelAboveOpen()
    {
        var (host, _) = Open("overflow");
        ClickEntry(host, "insert");
        ClickEntry(host, "more");

        Assert.Equal("true", host.Find("#insert").GetAttribute("aria-expanded"));
        Assert.Equal("true", host.Find("#more").GetAttribute("aria-expanded"));
        Assert.Single(host.FindAll("#more-menu"));
    }

    [Fact]
    public void ThePointerHoveringTheEntry_OpensItsMenuWithoutTheFocus_AndTheFollowingClickKeepsItOpen()
    {
        var (host, module) = Open("overflow");

        host.Find("#insert").PointerEnter(new PointerEventArgs { PointerType = "mouse" });

        Assert.Equal("true", host.Find("#insert").GetAttribute("aria-expanded"));
        var placed = module.Invocations["openSubMenu"].Single();
        Assert.Equal(["insert", "insert-menu", false], placed.Arguments.Take(3).ToArray());

        ClickTheEntryOpenThenClosed(host);
    }

    [Theory]
    [InlineData("touch")]
    [InlineData("")]
    public void ATouchOrAnUnknownPointer_DoesNotOpenByHovering(string pointerType)
    {
        var (host, _) = Open("overflow");

        host.Find("#insert").PointerEnter(new PointerEventArgs { PointerType = pointerType });

        Assert.Equal("false", host.Find("#insert").GetAttribute("aria-expanded"));
        Assert.Empty(host.FindAll("#insert-menu"));
    }

    [Fact]
    public void HoveringAnotherEntry_ClosesTheOpenOneAndOpensItself()
    {
        var (host, _) = Open("overflow");
        host.Find("#insert").PointerEnter(new PointerEventArgs { PointerType = "mouse" });

        host.Find("#type").PointerEnter(new PointerEventArgs { PointerType = "mouse" });

        Assert.Empty(host.FindAll("#insert-menu"));
        Assert.Single(host.FindAll("#type-menu"));
    }

    [Fact]
    public void AnActivationWithNoPointer_AsksForTheFocusOnTheFirstItem_AClickDoesNot()
    {
        var (host, module) = Open("overflow");

        host.Find("#insert").Click(new MouseEventArgs { Detail = 0 });
        ClickEntry(host, "type");

        var calls = module.Invocations["openSubMenu"].Select(call => call.Arguments.ToArray()).ToList();
        Assert.Equal(["insert", "insert-menu", true], calls[0].Take(3));
        Assert.Equal(["type", "type-menu", false], calls[1].Take(3));
    }

    [Fact]
    public void ClosingTheWholeMenu_RemovesEveryLevel_AndItReopensClosed()
    {
        var (host, _) = Open("overflow");
        ClickEntry(host, "insert");
        ClickEntry(host, "more");

        host.Find("#m-menu").KeyDown("Tab");

        Assert.Empty(host.FindAll("[role=menu]"));
        host.Find(".omni-overflow-menu__trigger").Click();
        Assert.Equal("false", host.Find("#insert").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void ALeafAtTheTopLevel_StillClosesTheMenuAndRaisesItsAction()
    {
        var (host, module) = Open("overflow");
        ClickEntry(host, "insert");

        host.Find("#top").Click();

        Assert.Empty(host.FindAll("[role=menu]"));
        Assert.Equal(["Top"], Chosen(host));
        Assert.Equal(true, module.Invocations["closeMenu"].Single().Arguments[1]);
    }

    [Fact]
    public void AMenuHoldingSubMenus_ScrollsLikeAPlainMenu_ItsSubMenuLeavingTheClippingBoxByTheTopLayer()
    {
        // Owner decision 2026-11-20: the rule that made such a menu unclipped and unscrollable is gone.
        var rules = ShippedLookTests.Rules();
        Assert.DoesNotContain(rules, rule => rule.Selector.Contains(":has(", StringComparison.Ordinal) && rule.Selector.Contains("omni-menu__sub", StringComparison.Ordinal));
        var menu = rules.Last(rule => rule.Selector == ".omni-menu" && rule.Body.Contains("max-block-size", StringComparison.Ordinal)).Body;
        Assert.Equal("auto", ShippedLookTests.Value(menu, "overflow-y"));
        // The list is drawn as a manual popover, so the browser lifts it out of the scrolling parent.
        var (host, _) = Open("overflow");
        ClickEntry(host, "insert");
        Assert.Equal("manual", host.Find("#insert-menu").GetAttribute("popover"));
        Assert.Equal("0", ShippedLookTests.Value(ShippedLookTests.Body(".omni-menu__submenu"), "margin"));
    }

    [Fact]
    public async Task AScrollOfTheParentList_ClosesTheOpenSubMenu_AndItsDescendants_FocusGoingBackToTheEntry()
    {
        var (host, module) = Open("overflow");
        ClickEntry(host, "insert");
        ClickEntry(host, "more");
        var insert = Scrolled(module, "insert");

        await host.InvokeAsync(() => insert.ScrolledAsync());

        Assert.Empty(host.FindAll("#insert-menu"));
        Assert.Empty(host.FindAll("#more-menu"));
        Assert.Equal("false", host.Find("#insert").GetAttribute("aria-expanded"));
        Assert.NotNull(host.Find("#m-menu"));
        Assert.Contains(module.Invocations["leaveSubMenu"], call => (string?)call.Arguments[0] == "insert-menu" && (string?)call.Arguments[1] == "insert");
        Assert.DoesNotContain(module.Invocations, call => call.Identifier == "closeMenu");
        Assert.Empty(Chosen(host));
        ClickEntry(host, "insert");
        Assert.Single(host.FindAll("#insert-menu"));
    }

    [Fact]
    public async Task AScrollOfASubMenuList_ClosesOnlyTheSubMenuOpenInIt()
    {
        var (host, module) = Open("overflow");
        ClickEntry(host, "insert");
        ClickEntry(host, "more");
        var more = Scrolled(module, "more");

        await host.InvokeAsync(() => more.ScrolledAsync());

        Assert.Empty(host.FindAll("#more-menu"));
        Assert.Single(host.FindAll("#insert-menu"));
        Assert.Equal("true", host.Find("#insert").GetAttribute("aria-expanded"));
    }

    [Fact]
    public async Task AScrollReportedAfterTheSubMenuClosed_ChangesNothing()
    {
        var (host, module) = Open("overflow");
        ClickEntry(host, "insert");
        var insert = Scrolled(module, "insert");
        ClickEntry(host, "insert");
        var leaves = module.Invocations["leaveSubMenu"].Count;

        await host.InvokeAsync(() => insert.ScrolledAsync());

        Assert.Equal(leaves, module.Invocations["leaveSubMenu"].Count);
        Assert.Empty(host.FindAll("#insert-menu"));
    }

    [Fact]
    public void TheScriptIsToldHowToReachTheSubMenu_AndLetsGoOfItWhenItCloses()
    {
        var (host, module) = Open("overflow");
        ClickEntry(host, "insert");
        Assert.IsType<DotNetObjectReference<SubMenuInteropBridge>>(Assert.Single(module.Invocations["openSubMenu"]).Arguments[3]);

        ClickEntry(host, "insert");

        Assert.Equal(["insert-menu", "insert"], Assert.Single(module.Invocations["leaveSubMenu"]).Arguments.Select(argument => (string?)argument));
    }

    [Fact]
    public void AMenuWithoutASubMenu_DrawsNoSubMenuMarkup()
    {
        JSInterop.SetupModule(OmniModules.Focus);
        var menu = Render<OmniOverflowMenu>(parameters => parameters
            .Add(component => component.Open, true)
            .AddChildContent("<button>x</button>"));

        Assert.Empty(menu.FindAll(".omni-menu__sub, .omni-menu__submenu, [aria-haspopup=menu][role=menuitem]"));
    }

    private static SubMenuInteropBridge Scrolled(BunitJSModuleInterop module, string entryId) =>
        ((DotNetObjectReference<SubMenuInteropBridge>)module.Invocations["openSubMenu"].Single(call => (string?)call.Arguments[0] == entryId).Arguments[3]!).Value;

    private static List<string> Chosen(IRenderedComponent<SubMenuTestHost> host) => host.Instance.Chosen;

    /// <summary>A pointer click on the menu item <paramref name="id"/>, as a mouse press sends it (one click, detail 1).</summary>
    private static void ClickEntry(IRenderedComponent<SubMenuTestHost> host, string id) =>
        host.Find($"#{id}").Click(new MouseEventArgs { Detail = 1 });

    /// <summary>A first click on the insert entry leaves its menu open, a second one closes it.</summary>
    private static void ClickTheEntryOpenThenClosed(IRenderedComponent<SubMenuTestHost> host)
    {
        ClickEntry(host, "insert");
        Assert.Equal("true", host.Find("#insert").GetAttribute("aria-expanded"));
        ClickEntry(host, "insert");
        Assert.Equal("false", host.Find("#insert").GetAttribute("aria-expanded"));
    }

    private (IRenderedComponent<SubMenuTestHost> Host, BunitJSModuleInterop Module) Open(string kind)
    {
        var module = JSInterop.SetupModule(OmniModules.Focus);
        var host = Render<SubMenuTestHost>(parameters => parameters.Add(component => component.Kind, kind));
        OpenMenu(host, kind);
        return (host, module);
    }

    private static void OpenMenu(IRenderedComponent<SubMenuTestHost> host, string kind)
    {
        if (kind == "context")
        {
            host.Find(".omni-context-menu").KeyDown(new KeyboardEventArgs { Key = "F10", ShiftKey = true });
        }
        else
        {
            host.Find(kind switch
            {
                "split" => ".omni-split-button__toggle",
                "profile" => ".omni-profile-menu__trigger",
                _ => ".omni-overflow-menu__trigger"
            }).Click();
        }

        host.WaitForAssertion(() => Assert.NotNull(host.Find("#m-menu")));
    }
}
