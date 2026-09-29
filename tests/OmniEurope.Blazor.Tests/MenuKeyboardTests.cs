using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The four menus of the package share one engine: the same <see cref="OmniMenuItem"/>, the same keys
/// (the arrows, Home and End move through the items, Escape closes and gives the focus back to the
/// trigger, Tab closes and lets the focus go on), the same optional control through
/// <c>Open</c>/<c>OpenChanged</c>, and an <c>aria-controls</c> that only names a menu that is there.
/// The focus itself moves in <c>omni-focus.js</c>, which bUnit does not run: these tests hold the calls
/// made to it (<c>openMenu</c>, <c>moveMenuFocus</c>, <c>closeMenu</c> and its restore flag).
/// </summary>
public sealed class MenuKeyboardTests : OmniBunitContext
{
    public static TheoryData<string> Menus => new() { "overflow", "split", "profile", "context" };

    public static TheoryData<string> TriggeredMenus => new() { "overflow", "split", "profile" };

    [Theory]
    [MemberData(nameof(Menus))]
    public void Arrows_HomeAndEnd_MoveThroughTheItemsOfTheOpenMenu(string kind)
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Focus);
        var menu = Render(Menu(kind));

        OpenByKeyboard(menu, kind);
        menu.WaitForAssertion(() => Assert.Single(module.Invocations["openMenu"]));
        Assert.Equal("m-menu", module.Invocations["openMenu"][0].Arguments[0]);

        foreach (var key in new[] { "ArrowDown", "ArrowUp", "Home", "End" })
        {
            menu.Find("#m-menu").KeyDown(key);
        }

        menu.WaitForAssertion(() => Assert.Equal(4, module.Invocations["moveMenuFocus"].Count));
        Assert.All(module.Invocations["moveMenuFocus"], call => Assert.Equal("m-menu", call.Arguments[0]));
        Assert.Equal(["ArrowDown", "ArrowUp", "Home", "End"], module.Invocations["moveMenuFocus"].Select(call => (string)call.Arguments[1]!));
        Assert.NotNull(menu.Find("#m-menu"));
    }

    [Theory]
    [MemberData(nameof(Menus))]
    public void Escape_ClosesTheMenu_AndGivesTheFocusBack(string kind)
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Focus);
        var reported = new List<bool>();
        var menu = Render(Menu(kind, onOpenChanged: reported.Add));
        OpenByKeyboard(menu, kind);
        menu.WaitForAssertion(() => Assert.Single(module.Invocations["openMenu"]));

        menu.Find("#m-menu").KeyDown("Escape");

        Assert.Empty(menu.FindAll("[role=menu]"));
        var close = Assert.Single(module.Invocations["closeMenu"]);
        Assert.Equal(true, close.Arguments[1]);
        Assert.Equal([true, false], reported);
    }

    [Theory]
    [MemberData(nameof(Menus))]
    public void Tab_ClosesTheMenu_WithoutPullingTheFocusBack(string kind)
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Focus);
        var menu = Render(Menu(kind));
        OpenByKeyboard(menu, kind);
        menu.WaitForAssertion(() => Assert.Single(module.Invocations["openMenu"]));

        menu.Find("#m-menu").KeyDown("Tab");

        Assert.Empty(menu.FindAll("[role=menu]"));
        Assert.Equal(false, Assert.Single(module.Invocations["closeMenu"]).Arguments[1]);
    }

    [Theory]
    [MemberData(nameof(TriggeredMenus))]
    public void ArrowUpOnTheTrigger_OpensOnTheLastItem(string kind)
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Focus);
        var menu = Render(Menu(kind));

        menu.Find(TriggerSelector(kind)).KeyDown("ArrowUp");

        menu.WaitForAssertion(() => Assert.Single(module.Invocations["openMenu"]));
        Assert.Equal(true, module.Invocations["openMenu"][0].Arguments[6]);
    }

    [Theory]
    [MemberData(nameof(Menus))]
    public void ChoosingAnItem_ClosesTheMenuWithTheFocusBack_BeforeItsActionRuns(string kind)
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Focus);
        var closesWhenRun = new List<int>();
        var menu = Render(Menu(kind, onItem: () => closesWhenRun.Add(module.Invocations["closeMenu"].Count)));
        OpenByKeyboard(menu, kind);
        menu.WaitForAssertion(() => Assert.Single(module.Invocations["openMenu"]));

        menu.FindAll("[role=menuitem]")[0].Click();

        Assert.Equal([1], closesWhenRun);
        Assert.Equal(true, module.Invocations["closeMenu"][0].Arguments[1]);
        Assert.Empty(menu.FindAll("[role=menu]"));
    }

    [Theory]
    [MemberData(nameof(TriggeredMenus))]
    public void AriaControls_NamesTheMenuOnlyWhileItIsOpen(string kind)
    {
        JSInterop.SetupModule(Internal.OmniModules.Focus);
        var menu = Render(Menu(kind));
        var trigger = menu.Find(TriggerSelector(kind));
        Assert.False(trigger.HasAttribute("aria-controls"));
        Assert.Equal("false", trigger.GetAttribute("aria-expanded"));
        Assert.Equal("menu", trigger.GetAttribute("aria-haspopup"));

        trigger.Click();

        trigger = menu.Find(TriggerSelector(kind));
        Assert.Equal("m-menu", trigger.GetAttribute("aria-controls"));
        Assert.Equal("true", trigger.GetAttribute("aria-expanded"));
        var surface = menu.Find("#m-menu");
        Assert.Equal("menu", surface.GetAttribute("role"));
        Assert.False(string.IsNullOrWhiteSpace(surface.GetAttribute("aria-label")));

        trigger.Click();
        Assert.False(menu.Find(TriggerSelector(kind)).HasAttribute("aria-controls"));
        Assert.Empty(menu.FindAll("[role=menu]"));
    }

    [Theory]
    [MemberData(nameof(Menus))]
    public void Open_ControlsTheMenu_AndOpenChangedReportsTheReadersRequests(string kind)
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Focus);
        var reported = new List<bool>();
        var open = Render(Menu(kind, open: true, onOpenChanged: reported.Add));
        open.WaitForAssertion(() => Assert.Single(module.Invocations["openMenu"]));
        Assert.NotNull(open.Find("#m-menu"));

        // Controlled: Escape asks to close, the menu stays as long as the host keeps it open.
        open.Find("#m-menu").KeyDown("Escape");
        Assert.Equal([false], reported);
        Assert.NotNull(open.Find("#m-menu"));

        var closed = Render(Menu(kind, open: false, onOpenChanged: reported.Add));
        OpenByKeyboard(closed, kind);
        Assert.Equal([false, true], reported);
        Assert.Empty(closed.FindAll("[role=menu]"));
    }

    [Theory]
    [MemberData(nameof(TriggeredMenus))]
    public void MenuLabel_NamesTheSurface_AndLabelTheTrigger(string kind)
    {
        JSInterop.SetupModule(Internal.OmniModules.Focus);
        var menu = Render(Menu(kind, label: "Déclencheur", menuLabel: "Surface"));

        Assert.Equal(kind == "split" ? "Surface" : "Déclencheur", menu.Find(TriggerSelector(kind)).GetAttribute("aria-label"));
        menu.Find(TriggerSelector(kind)).Click();
        Assert.Equal("Surface", menu.Find("#m-menu").GetAttribute("aria-label"));
    }

    private static string TriggerSelector(string kind) => kind switch
    {
        "overflow" => ".omni-overflow-menu__trigger",
        "split" => ".omni-split-button__toggle",
        "profile" => ".omni-profile-menu__trigger",
        _ => ".omni-context-menu"
    };

    private static void OpenByKeyboard(IRenderedComponent<ContainerFragment> menu, string kind)
    {
        if (kind == "context")
        {
            menu.Find(".omni-context-menu").KeyDown(new KeyboardEventArgs { Key = "F10", ShiftKey = true });
        }
        else
        {
            menu.Find(TriggerSelector(kind)).KeyDown("ArrowDown");
        }
    }

    private RenderFragment Menu(
        string kind,
        bool? open = null,
        Action<bool>? onOpenChanged = null,
        Action? onItem = null,
        string? label = null,
        string? menuLabel = null) => builder =>
    {
        var type = kind switch
        {
            "overflow" => typeof(OmniOverflowMenu),
            "split" => typeof(OmniSplitButton),
            "profile" => typeof(OmniProfileMenu),
            _ => typeof(OmniContextMenu)
        };
        builder.OpenComponent(0, type);
        builder.AddComponentParameter(1, "Id", "m");
        if (open is not null)
        {
            builder.AddComponentParameter(2, "Open", open);
        }

        if (onOpenChanged is not null)
        {
            builder.AddComponentParameter(3, "OpenChanged", EventCallback.Factory.Create<bool>(this, onOpenChanged));
        }

        if (kind == "split")
        {
            builder.AddComponentParameter(4, nameof(OmniSplitButton.Text), "Enregistrer");
        }

        if (kind == "context")
        {
            builder.AddComponentParameter(5, nameof(OmniContextMenu.TriggerContent), (RenderFragment)(content => content.AddContent(0, "Cible")));
        }

        if (label is not null)
        {
            builder.AddComponentParameter(6, kind == "split" ? nameof(OmniSplitButton.Label) : "Label", label);
        }

        if (menuLabel is not null)
        {
            builder.AddComponentParameter(7, "MenuLabel", menuLabel);
        }

        builder.AddComponentParameter(8, "ChildContent", (RenderFragment)(items =>
        {
            foreach (var (text, index) in new[] { "Modifier", "Dupliquer", "Supprimer" }.Select((text, index) => (text, index)))
            {
                items.OpenComponent<OmniMenuItem>(index * 10);
                items.AddComponentParameter(index * 10 + 1, nameof(OmniMenuItem.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, () => onItem?.Invoke()));
                items.AddComponentParameter(index * 10 + 2, nameof(OmniMenuItem.ChildContent), (RenderFragment)(label => label.AddContent(0, text)));
                items.CloseComponent();
            }
        }));
        builder.CloseComponent();
    };
}
