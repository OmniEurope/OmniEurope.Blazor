using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The navigation components at their edges: a menu opened by its host, its settings and languages, a
/// link that already carries its rel, panel menu entries with icon and badge and guarded routes, the
/// profile menu in a portal and from the keyboard, the sidebar's width and route events and lost
/// circuit, items drawn outside their parent, and the tab keys and script bridge.
/// </summary>
public sealed class NavigationEdgeTests : OmniBunitContext
{
    private static RenderFragment Text(string text) => builder => builder.AddContent(0, text);

    // ---- app menu and link ------------------------------------------------------------------------

    [Fact]
    public void AppMenu_OpenedByItsHost_LeadsToTheSettings_AndChangesTheLanguage()
    {
        var settings = 0;
        var languages = new List<string>();
        var opened = new List<bool>();
        var menu = Render<OmniAppMenu>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.OpenChanged, open => opened.Add(open))
            .Add(component => component.Languages, [new OmniAppMenuLanguage("fr", "Français"), new OmniAppMenuLanguage("en", "English")])
            .Add(component => component.Language, "fr")
            .Add(component => component.LanguageChanged, code => languages.Add(code))
            .Add(component => component.OnSettings, () => settings++));

        menu.Find(".omni-app-menu select").Change("1");
        menu.Find(".omni-app-menu__settings").Click();

        Assert.Equal(["en"], languages);
        Assert.Equal(1, settings);
        Assert.Equal([false], opened);
    }

    [Fact]
    public void LinkOpeningElsewhere_KeepsARelTheHostAlreadyGave()
    {
        var link = Render<OmniLink>(parameters => parameters
            .Add(component => component.NewTab, true)
            .AddUnmatched("href", "https://example.org")
            .AddUnmatched("rel", "noopener author")
            .AddChildContent("Ailleurs"));

        Assert.Equal("noopener author noreferrer", link.Find("a").GetAttribute("rel"));
    }

    // ---- panel menu -------------------------------------------------------------------------------

    [Fact]
    public void PanelMenuEntries_DrawTheirIconAndBadge_InEveryShape()
    {
        var menu = Render<OmniPanelMenu>(parameters => parameters.AddChildContent(builder =>
        {
            void Item(int sequence, string text, string? href, bool group, bool clickable)
            {
                builder.OpenComponent<OmniPanelMenuItem>(sequence);
                builder.AddComponentParameter(sequence + 1, nameof(OmniPanelMenuItem.Text), text);
                builder.AddComponentParameter(sequence + 2, nameof(OmniPanelMenuItem.Href), href);
                builder.AddComponentParameter(sequence + 3, nameof(OmniPanelMenuItem.Icon), Text("◆"));
                builder.AddComponentParameter(sequence + 4, nameof(OmniPanelMenuItem.Badge), Text("3"));
                if (clickable)
                {
                    builder.AddComponentParameter(sequence + 5, nameof(OmniPanelMenuItem.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, () => { }));
                }

                if (group)
                {
                    builder.AddComponentParameter(sequence + 6, nameof(OmniPanelMenuItem.ChildContent), Text("enfant"));
                }

                builder.CloseComponent();
            }

            Item(0, "Section", "/section", group: true, clickable: false);
            Item(10, "Groupe", null, group: true, clickable: false);
            Item(20, "Action", null, group: false, clickable: true);
            Item(30, "Étiquette", null, group: false, clickable: false);
        }));

        Assert.Equal(4, menu.FindAll(".omni-panel-menu__icon").Count);
        Assert.Equal(4, menu.FindAll(".omni-panel-menu__badge").Count);
    }

    [Theory]
    [InlineData("mailto:equipe@example.org")]
    [InlineData("/")]
    public void PanelMenuEntryOutsideTheApplicationOrAtItsRoot_IsCurrentOnlyAtTheRoot(string href)
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/autre");

        var menu = Render<OmniPanelMenu>(parameters => parameters.AddChildContent<OmniPanelMenuItem>(item => item
            .Add(component => component.Text, "Entrée")
            .Add(component => component.Href, href)));

        Assert.Null(menu.Find("a").GetAttribute("aria-current"));
    }

    [Theory]
    [InlineData("https://ailleurs.example.org/autre", "/autre", null)]
    [InlineData("/", "/", "page")]
    public void PanelMenuEntry_OnAnotherHost_IsNeverCurrent_AndTheRootIsCurrentAtTheRoot(string href, string at, string? current)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(at);

        var menu = Render<OmniPanelMenu>(parameters => parameters.AddChildContent<OmniPanelMenuItem>(item => item
            .Add(component => component.Text, "Entrée")
            .Add(component => component.Href, href)));

        Assert.Equal(current, menu.Find("a").GetAttribute("aria-current"));
    }

    [Fact]
    public void GroupOutsideAMenu_StaysOpenWhileTheRouteMovesInsideIt_AndItsLinkLeavesNavigationToTheBrowser()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/rapports/mensuel");
        var group = Render<OmniPanelMenuItem>(parameters => parameters
            .Add(component => component.Text, "Rapports")
            .Add(component => component.Href, "/rapports")
            .AddChildContent("enfant"));
        Assert.Contains("omni-panel-menu__group--open", group.Find(".omni-panel-menu__group").ClassList);

        group.Find(".omni-panel-menu__toggle").Click();
        navigation.NavigateTo("/rapports/annuel");
        group.WaitForAssertion(() => Assert.Contains("omni-panel-menu__group--closed", group.Find(".omni-panel-menu__group").ClassList));

        // Without a menu there is no guard: the link click leaves the navigation to the browser.
        group.Find(".omni-panel-menu__summary a").Click();
        Assert.EndsWith("/rapports/annuel", navigation.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void GroupWithoutLinkOutsideAMenu_UnfoldsOnItsButton()
    {
        var group = Render<OmniPanelMenuItem>(parameters => parameters
            .Add(component => component.Text, "Rapports")
            .AddChildContent("enfant"));

        group.Find("button[aria-controls]").Click();

        Assert.Equal("true", group.Find("button[aria-controls]").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void ClosedSidebar_IgnoresNavigation()
    {
        JSInterop.SetupModule(OmniModules.Focus).Mode = JSRuntimeMode.Loose;
        var closed = 0;
        Render<OmniSidebar>(parameters => parameters
            .Add(component => component.Reveal, OmniSidebarReveal.Overlay)
            .Add(component => component.OpenChanged, open => closed += open ? 0 : 1)
            .AddChildContent("Menu"));

        Services.GetRequiredService<NavigationManager>().NavigateTo("/page");

        Assert.Equal(0, closed);
    }

    [Fact]
    public void StepOutsideItsParent_WithItsOwnId_CountsFromOne_AndNamesItsPanelAfterIt()
    {
        var step = Render<OmniStepsItem>(parameters => parameters
            .Add(component => component.Title, "Adresse")
            .Add(component => component.Id, "adresse"));

        Assert.Equal("1", step.Find(".omni-steps__number").TextContent);
        Assert.Equal("adresse-panel", step.Find("section").Id);
    }

    [Fact]
    public async Task GuardedGroupLink_NavigatesOnlyWhenTheHostAgrees()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        var answers = new Queue<bool>([false, true]);
        var menu = Render<OmniPanelMenu>(parameters => parameters
            .Add(component => component.CanNavigate, _ => Task.FromResult(answers.Dequeue()))
            .AddChildContent<OmniPanelMenuItem>(item => item
                .Add(component => component.Text, "Section")
                .Add(component => component.Href, "/section")
                .AddChildContent("enfant")));

        await menu.Find(".omni-panel-menu__summary a").ClickAsync(new MouseEventArgs());
        Assert.DoesNotContain("/section", navigation.Uri, StringComparison.Ordinal);
        await menu.Find(".omni-panel-menu__summary a").ClickAsync(new MouseEventArgs());
        Assert.EndsWith("/section", navigation.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void GroupOutsideAMenu_UnfoldsInPlace_AndReopensWhenTheRouteEntersIt()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        var group = Render<OmniPanelMenuItem>(parameters => parameters
            .Add(component => component.Text, "Rapports")
            .Add(component => component.Href, "/rapports")
            .AddChildContent("enfant"));

        group.Find(".omni-panel-menu__toggle").Click();
        Assert.Contains("omni-panel-menu__group--open", group.Find(".omni-panel-menu__group").ClassList);
        group.Find(".omni-panel-menu__toggle").Click();
        Assert.Contains("omni-panel-menu__group--closed", group.Find(".omni-panel-menu__group").ClassList);

        navigation.NavigateTo("/rapports/mensuel");

        group.WaitForAssertion(() => Assert.Contains("omni-panel-menu__group--open", group.Find(".omni-panel-menu__group").ClassList));
    }

    // ---- profile menu -----------------------------------------------------------------------------

    [Fact]
    public void ProfileMenu_InAHost_OpensInThePortal_AndLeavesItOnClose()
    {
        JSInterop.SetupModule(OmniModules.Focus).Mode = JSRuntimeMode.Loose;
        using var service = new OmniOverlayService();
        var host = Render<OmniComponentsHost>(parameters => parameters
            .Add(component => component.OverlayService, service)
            .AddChildContent<OmniProfileMenu>(menu => menu.Add(component => component.Initials, "AB").AddChildContent("Profil")));

        host.Find(".omni-profile-menu__trigger").Click();
        host.WaitForAssertion(() => Assert.NotEmpty(host.FindAll(".omni-overlay-portal__entry--profilemenu")));
        host.Find(".omni-profile-menu__trigger").Click();

        host.WaitForAssertion(() => Assert.Empty(host.FindAll(".omni-overlay-portal__entry--profilemenu")));
    }

    [Fact]
    public void ProfileMenuTrigger_OpensWithTheArrowsOnly_WhenClosedAndUsable()
    {
        JSInterop.SetupModule(OmniModules.Focus).Mode = JSRuntimeMode.Loose;
        var opened = new List<bool>();
        var menu = Render<OmniProfileMenu>(parameters => parameters
            .Add(component => component.Initials, "AB")
            .Add(component => component.OpenChanged, open => opened.Add(open))
            .AddChildContent("Profil"));

        menu.Find(".omni-profile-menu__trigger").KeyDown("Enter");
        Assert.Empty(opened);
        menu.Find(".omni-profile-menu__trigger").KeyDown("ArrowUp");
        Assert.Equal([true], opened);
        menu.Find(".omni-profile-menu__trigger").KeyDown("ArrowDown");
        Assert.Equal([true], opened);

        var locked = Render<OmniProfileMenu>(parameters => parameters
            .Add(component => component.Disabled, true)
            .Add(component => component.OpenChanged, open => opened.Add(open))
            .AddChildContent("Profil"));
        locked.Find(".omni-profile-menu__trigger").KeyDown("ArrowDown");
        Assert.Equal([true], opened);
    }

    // ---- sidebar ----------------------------------------------------------------------------------

    [Fact]
    public async Task Sidebar_IgnoresTheSameWidth_AndClosesOnlyWhenItFloatsOverThePage()
    {
        JSInterop.SetupModule(OmniModules.Focus).Mode = JSRuntimeMode.Loose;
        var navigation = Services.GetRequiredService<NavigationManager>();
        var closed = 0;
        var sidebar = Render<OmniSidebar>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.OpenChanged, open => closed += open ? 0 : 1)
            .AddChildContent("Menu"));
        var renders = sidebar.RenderCount;

        await sidebar.InvokeAsync(() => sidebar.Instance.SetNarrowAsync(false));
        Assert.Equal(renders, sidebar.RenderCount);
        navigation.NavigateTo("/page");
        Assert.Equal(0, closed);

        sidebar.Render(parameters => parameters.Add(component => component.Reveal, OmniSidebarReveal.Overlay));
        navigation.NavigateTo("/autre");
        sidebar.WaitForAssertion(() => Assert.Equal(1, closed));
    }

    [Fact]
    public async Task Sidebar_OnALostCircuit_AttachesAndLeavesQuietly()
    {
        var runtime = new ManualJSRuntime();
        runtime.Module.CallFailures["configureSidebarOverflow"] = new JSDisconnectedException("perdu");
        runtime.Module.CallFailures["disposeSidebarOverflow"] = new JSDisconnectedException("perdu");
        Services.AddSingleton<IJSRuntime>(runtime);
        var sidebar = Render<OmniSidebar>(parameters => parameters.Add(component => component.Open, true).AddChildContent("Menu"));

        await sidebar.Instance.DisposeAsync();

        Assert.Equal(["configureSidebarOverflow", "disposeSidebarOverflow"], runtime.Module.Calls);
    }

    [Fact]
    public async Task SidebarGoneBeforeItsScript_HasNothingToRelease()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        Services.AddSingleton<IJSRuntime>(runtime);
        var sidebar = Render<OmniSidebar>(parameters => parameters.AddChildContent("Menu"));

        await sidebar.Instance.DisposeAsync();

        Assert.Empty(runtime.Module.Calls);
        runtime.PendingImport.SetResult(runtime.Module);
    }

    [Fact]
    public void SidebarToggle_DrawsTheHostContent()
    {
        var toggle = Render<OmniSidebarToggle>(parameters => parameters.AddChildContent("Menu principal"));

        Assert.Contains("Menu principal", toggle.Markup, StringComparison.Ordinal);
    }

    // ---- items outside their parent ---------------------------------------------------------------

    [Fact]
    public void StepAndTabOutsideTheirParent_DrawThemselves_AndChooseNothing()
    {
        var step = Render<OmniStepsItem>(parameters => parameters.Add(component => component.Title, "Adresse").Add(component => component.Index, 2));
        step.Find(".omni-steps__button").Click();
        Assert.Equal("3", step.Find(".omni-steps__number").TextContent);
        Assert.EndsWith("-panel", step.Find("section").Id, StringComparison.Ordinal);

        var tab = Render<OmniTabsItem>(parameters => parameters.Add(component => component.Title, "General"));
        tab.Find("[role=tab]").Click();
        Assert.Equal("omni-tab-General-tab", tab.Find("[role=tab]").Id);
        Assert.Equal("false", tab.Find("[role=tab]").GetAttribute("aria-selected"));
    }

    // ---- tabs -------------------------------------------------------------------------------------

    private IRenderedComponent<OmniTabs> RenderTabs(List<string?> changes, string? value = "b", Action<ComponentParameterCollectionBuilder<OmniTabs>>? extra = null) =>
        Render<OmniTabs>(parameters =>
        {
            parameters
                .Add(component => component.Value, value)
                .Add(component => component.ValueChanged, next => changes.Add(next))
                .AddChildContent(builder =>
                {
                    foreach (var (key, index) in new[] { ("a", 0), ("b", 1), ("c", 2) })
                    {
                        builder.OpenComponent<OmniTabsItem>(index * 10);
                        builder.AddComponentParameter(index * 10 + 1, nameof(OmniTabsItem.Key), key);
                        builder.AddComponentParameter(index * 10 + 2, nameof(OmniTabsItem.Title), key.ToUpperInvariant());
                        builder.CloseComponent();
                    }
                });
            extra?.Invoke(parameters);
        });

    [Theory]
    [InlineData("Home", "a")]
    [InlineData("ArrowLeft", "a")]
    public void TabKeys_GoHomeAndBack(string key, string expected)
    {
        var changes = new List<string?>();
        var tabs = RenderTabs(changes);

        tabs.Find("[role=tablist]").KeyDown(key);

        Assert.Equal([expected], changes);
    }

    [Fact]
    public void TabKeys_OtherKeysAndATablistWithoutTabs_MoveNothing()
    {
        var changes = new List<string?>();
        RenderTabs(changes).Find("[role=tablist]").KeyDown("Enter");
        Render<OmniTabs>(parameters => parameters.Add(component => component.ValueChanged, next => changes.Add(next))).Find("[role=tablist]").KeyDown("ArrowRight");

        Assert.Empty(changes);
    }

    [Fact]
    public void TabsWithoutValue_ArrowRightStartsFromTheFirst()
    {
        var changes = new List<string?>();
        var tabs = RenderTabs(changes, value: null);

        tabs.Find("[role=tablist]").KeyDown("ArrowRight");

        Assert.Equal(["b"], changes);
    }

    [Fact]
    public async Task Tabs_WheelScopeComesAndGoes_TheScriptSelects_AndALostCircuitIsQuietAtTheEnd()
    {
        var module = JSInterop.SetupModule(OmniModules.Focus);
        module.Mode = JSRuntimeMode.Loose;
        module.SetupVoid("disposeTabsOverflow", _ => true).SetException(new JSDisconnectedException("perdu"));
        var changes = new List<string?>();
        var tabs = RenderTabs(changes, extra: parameters => parameters
            .Add(component => component.ScrollablePanels, true)
            .Add(component => component.WheelScrollScope, " main "));
        Assert.Equal("main", Assert.Single(module.Invocations["attachTabsWheelScope"]).Arguments[1]);

        tabs.Render(parameters => parameters.Add(component => component.WheelScrollScope, null));
        Assert.Single(module.Invocations["detachTabsWheelScope"]);

        var bridge = module.Invocations["configureTabs"][0].Arguments[1]!;
        var select = bridge.GetType().GetProperty("Value")!.GetValue(bridge)!;
        await tabs.InvokeAsync(() => (Task)select.GetType().GetMethod("SelectFromKeyboardAsync")!.Invoke(select, ["c"])!);
        Assert.Equal(["c"], changes);

        await tabs.Instance.DisposeAsync();
    }
}
