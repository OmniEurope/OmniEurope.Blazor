using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class NavigationComponentTests : OmniBunitContext
{
    public NavigationComponentTests()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Focus);
        module.SetupVoid("configureTabs", _ => true);
        module.SetupVoid("disposeTabs", _ => true);
    }

    [Fact]
    public void StructuredNavigation_RendersSemanticCurrentStates()
    {
        var host = Render<NavigationTestHost>();

        Assert.Equal("NAV", host.Find(".omni-breadcrumb").TagName);
        Assert.Equal("page", host.Find(".omni-breadcrumb [aria-current]").GetAttribute("aria-current"));
        Assert.Equal("page", host.Find(".omni-panel-menu [aria-current]").GetAttribute("aria-current"));
        Assert.Equal("true", host.Find(".omni-tabs__tab").GetAttribute("aria-selected"));
        Assert.Equal("tablist", host.Find(".omni-tabs__viewport").GetAttribute("role"));
        Assert.Equal("list", host.Find(".omni-steps__list").GetAttribute("role"));
        Assert.Equal("step", host.Find(".omni-steps__button").GetAttribute("aria-current"));
        Assert.Equal(host.Find(".omni-steps__button").Id, host.Find(".omni-steps__panel").GetAttribute("aria-labelledby"));
        Assert.DoesNotContain("style=", host.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TabsItem_RendersACustomTitleAndAcceptsADrop()
    {
        var host = Render<NavigationTestHost>();

        // The custom title replaces the text but not the accessible name, which stays the Title.
        Assert.Equal("Second (2)", host.Find(".host-tab-count").TextContent);

        // The drop lands on the wrapper, not on the tab button: a button cannot host a drop.
        var target = host.FindAll(".omni-tabs__item")[1];
        target.Drop();
        Assert.Equal(1, host.Instance.Dropped);

        // The wrapper draws no box (display: contents): the marks go on the tab button. A tab with no
        // handler is not a drop target.
        var buttons = host.FindAll(".omni-tabs__tab");
        Assert.DoesNotContain("omni-tabs__tab--drop-target", buttons[0].ClassList);
        Assert.Contains("omni-tabs__tab--drop-target", buttons[1].ClassList);

        // A drag over the tab lights its button until it leaves, a drag entering a child included.
        host.FindAll(".omni-tabs__item")[1].DragEnter();
        host.FindAll(".omni-tabs__item")[1].DragEnter();
        Assert.Contains("omni-tabs__tab--drag-over", host.FindAll(".omni-tabs__tab")[1].ClassList);
        host.FindAll(".omni-tabs__item")[1].DragLeave();
        Assert.Contains("omni-tabs__tab--drag-over", host.FindAll(".omni-tabs__tab")[1].ClassList);
        host.FindAll(".omni-tabs__item")[1].DragLeave();
        Assert.DoesNotContain("omni-tabs__tab--drag-over", host.FindAll(".omni-tabs__tab")[1].ClassList);
        Assert.Equal("var(--omni-color-accent-subtle)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-tabs__tab--drag-over"), "background"));
    }

    [Fact]
    public void TabsAndSteps_AreControlledAndRespectNavigationValidation()
    {
        var host = Render<NavigationTestHost>();

        host.Find(".omni-tabs__viewport").KeyDown("ArrowRight");
        Assert.Equal("second", host.Instance.Tab);
        Assert.Contains("Contenu 2", host.Find(".omni-tabs__panel:not([hidden])").TextContent, StringComparison.Ordinal);

        host.Instance.AllowStep = false;
        host.FindAll(".omni-steps__button")[1].Click();
        Assert.Equal(0, host.Instance.Step);

        host.Instance.AllowStep = true;
        host.FindAll(".omni-steps__button")[1].Click();
        Assert.Equal(1, host.Instance.Step);
    }

    [Fact]
    public void ProfileMenu_InvokesItsAction()
    {
        var host = Render<NavigationTestHost>();

        host.Find(".omni-profile-menu__trigger").Click();
        Assert.Equal("menu", host.Find(".omni-profile-menu__popup").GetAttribute("role"));
        host.FindAll("[role=menuitem]")[1].Click();

        Assert.True(host.Instance.SignedOut);
        Assert.Empty(host.FindAll("[role=menu]"));
    }

    [Fact]
    public void PanelMenu_CanCancelRouteNavigation()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        var initial = navigation.Uri;
        var item = Render<OmniPanelMenu>(parameters => parameters
            .Add(component => component.CanNavigate, _ => Task.FromResult(false))
            .AddChildContent<OmniPanelMenuItem>(entry => entry
                .Add(component => component.Text, "Protégé")
                .Add(component => component.Href, "/protected")));

        item.Find("a").Click();

        Assert.Equal(initial, navigation.Uri);
    }

    [Fact]
    public void StructuredNavigation_RejectsActiveUriSchemes()
    {
        Assert.Throws<InvalidOperationException>(() => Render<OmniBreadcrumbItem>(parameters => parameters
            .Add(component => component.Href, "javascript:alert(1)")
            .AddChildContent("Unsafe")));
        Assert.Throws<InvalidOperationException>(() => Render<OmniPanelMenuItem>(parameters => parameters
            .Add(component => component.Text, "Unsafe")
            .Add(component => component.Href, "data:text/html,unsafe")));
        Assert.Throws<InvalidOperationException>(() => Render<OmniMenuItem>(parameters => parameters
            .Add(component => component.Href, "vbscript:msgbox(1)")
            .AddChildContent("Unsafe")));
    }

    [Fact]
    public void PanelMenu_NormalizesQueryAndFragmentWhenDetectingTheActiveRoute()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/page?mode=details#section");

        var item = Render<OmniPanelMenuItem>(parameters => parameters
            .Add(component => component.Text, "Page")
            .Add(component => component.Href, "/page?mode=list#top"));

        Assert.Equal("page", item.Find("a").GetAttribute("aria-current"));
    }

    [Fact]
    public void PanelMenu_OnClickWithoutHref_IsAnActionButton_AndALinkIgnoresIt()
    {
        var clicks = 0;
        var action = Render<OmniPanelMenuItem>(parameters => parameters
            .Add(component => component.Text, "Copier")
            .Add(component => component.OnClick, () => clicks++));

        var button = action.Find("button.omni-panel-menu__link");
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal("Copier", button.QuerySelector(".omni-panel-menu__text")!.TextContent);
        Assert.Empty(action.FindAll(".omni-panel-menu__label"));

        button.Click();
        Assert.Equal(1, clicks);

        var link = Render<OmniPanelMenuItem>(parameters => parameters
            .Add(component => component.Text, "Page")
            .Add(component => component.Href, "/page")
            .Add(component => component.OnClick, () => clicks++));
        Assert.NotNull(link.Find("a.omni-panel-menu__link"));
        Assert.Empty(link.FindAll("button"));

        var label = Render<OmniPanelMenuItem>(parameters => parameters.Add(component => component.Text, "Libellé"));
        Assert.NotNull(label.Find("span.omni-panel-menu__label"));
    }

    [Fact]
    public void Sidebar_FloatingOpen_ClosesWhenAnEntryIsChosen()
    {
        var open = true;
        var sidebar = Render<OmniSidebar>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Reveal, OmniSidebarReveal.Overlay)
            .Add(component => component.Backdrop, true)
            .Add(component => component.OpenChanged, value => open = value)
            .AddChildContent("Navigation"));

        Assert.NotNull(sidebar.Find(".omni-sidebar__backdrop"));

        Services.GetRequiredService<NavigationManager>().NavigateTo("/elsewhere");

        Assert.False(open);
    }

    [Fact]
    public async Task Sidebar_FloatingOpen_ClosesOnTheBackdropAndOnEscapeFromAnywhere()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Focus);
        var open = true;
        var sidebar = Render<OmniSidebar>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Reveal, OmniSidebarReveal.Overlay)
            .Add(component => component.Backdrop, true)
            .Add(component => component.OpenChanged, value => open = value)
            .AddChildContent("Navigation"));

        // Escape is heard on the whole document, since the focus usually stays on the toggle.
        var attached = Assert.Single(module.Invocations["attachEscape"]);
        Assert.IsType<string>(attached.Arguments[0]);

        sidebar.Find(".omni-sidebar__backdrop").Click();
        Assert.False(open);

        open = true;
        await sidebar.InvokeAsync(sidebar.Instance.CloseFromEscapeAsync);
        Assert.False(open);

        // Closed, it stops listening; a late Escape does nothing.
        sidebar.Render(parameters => parameters.Add(component => component.Open, false));
        Assert.Single(module.Invocations["detachEscape"]);
        open = true;
        await sidebar.InvokeAsync(sidebar.Instance.CloseFromEscapeAsync);
        Assert.True(open);
    }

    [Fact]
    public void Sidebar_Pushing_NeverListensForEscape()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Focus);
        Render<OmniSidebar>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Reveal, OmniSidebarReveal.Push)
            .AddChildContent("Navigation"));

        Assert.Empty(module.Invocations["attachEscape"]);
    }

    [Fact]
    public async Task Sidebar_Pushing_FloatsWithItsVeilOnAPhone()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Focus);
        var open = true;
        var sidebar = Render<OmniSidebar>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Reveal, OmniSidebarReveal.Push)
            .Add(component => component.OpenChanged, value => open = value)
            .AddChildContent("Navigation"));
        Assert.Single(module.Invocations["watchNarrow"]);

        await sidebar.InvokeAsync(() => sidebar.Instance.SetNarrowAsync(true));
        Assert.Contains("omni-sidebar--overlay", sidebar.Find("aside").ClassList);
        Assert.NotNull(sidebar.Find(".omni-sidebar__backdrop"));
        Assert.Single(module.Invocations["attachEscape"]);

        await sidebar.InvokeAsync(() => sidebar.Instance.SetNarrowAsync(false));
        Assert.Contains("omni-sidebar--push", sidebar.Find("aside").ClassList);
        Assert.Empty(sidebar.FindAll(".omni-sidebar__backdrop"));
        Assert.Single(module.Invocations["detachEscape"]);

        await sidebar.InvokeAsync(() => sidebar.Instance.SetNarrowAsync(true));
        Services.GetRequiredService<NavigationManager>().NavigateTo("/elsewhere");
        Assert.False(open);
    }

    [Fact]
    public async Task Sidebar_StopsWatchingTheWidth_WhenItCanNoLongerFloat_AndWhenDisposed()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Focus);
        var sidebar = Render<OmniSidebar>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Reveal, OmniSidebarReveal.Push)
            .Add(component => component.OpenChanged, _ => { })
            .AddChildContent("Navigation"));
        await sidebar.InvokeAsync(() => sidebar.Instance.SetNarrowAsync(true));
        Assert.Contains("omni-sidebar--overlay", sidebar.Find("aside").ClassList);

        // A host that turns the sidebar to overlay no longer needs the width: the watch is dropped.
        sidebar.Render(parameters => parameters.Add(component => component.Reveal, OmniSidebarReveal.Overlay));
        Assert.Single(module.Invocations["unwatchNarrow"]);

        sidebar.Render(parameters => parameters.Add(component => component.Reveal, OmniSidebarReveal.Push));
        Assert.Equal(2, module.Invocations["watchNarrow"].Count);
        await sidebar.InvokeAsync(() => sidebar.Instance.DisposeAsync().AsTask());
        Assert.Equal(2, module.Invocations["unwatchNarrow"].Count);
    }

    [Fact]
    public async Task Sidebar_PushingThatTheHostNeverCloses_KeepsPushingOnAPhone()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Focus);
        var sidebar = Render<OmniSidebar>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Reveal, OmniSidebarReveal.Push)
            .AddChildContent("Navigation"));

        await sidebar.InvokeAsync(() => sidebar.Instance.SetNarrowAsync(true));

        Assert.Empty(module.Invocations["watchNarrow"]);
        Assert.Contains("omni-sidebar--push", sidebar.Find("aside").ClassList);
        Assert.Empty(sidebar.FindAll(".omni-sidebar__backdrop"));
    }

    [Fact]
    public void Sidebar_Pushing_StaysOpenAcrossNavigation()
    {
        var open = true;
        Render<OmniSidebar>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Reveal, OmniSidebarReveal.Push)
            .Add(component => component.OpenChanged, value => open = value)
            .AddChildContent("Navigation"));

        Services.GetRequiredService<NavigationManager>().NavigateTo("/elsewhere");

        Assert.True(open);
    }

    [Fact]
    public void Sidebar_Header_RendersInsideThePanelAndMarksTheSidebar()
    {
        var sidebar = Render<OmniSidebar>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Reveal, OmniSidebarReveal.Overlay)
            .Add(component => component.Header, (RenderFragment)(builder => builder.AddContent(0, "Brand")))
            .AddChildContent("Navigation"));

        Assert.Contains("omni-sidebar--has-header", sidebar.Find("aside").ClassName, StringComparison.Ordinal);
        Assert.Equal("Brand", sidebar.Find(".omni-sidebar__panel > .omni-sidebar__header").TextContent);
    }

    [Fact]
    public void Sidebar_WithoutHeader_RendersNoHeaderSlot()
    {
        var sidebar = Render<OmniSidebar>(parameters => parameters
            .Add(component => component.Open, true)
            .AddChildContent("Navigation"));

        Assert.Empty(sidebar.FindAll(".omni-sidebar__header"));
        Assert.DoesNotContain("omni-sidebar--has-header", sidebar.Find("aside").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Sidebar_menu_taller_than_the_window_scrolls_under_chevrons_not_a_scrollbar()
    {
        // review point 91 (2026-10-02): no scrollbar beside the menu, a chevron above and below
        // as on a tab strip, hidden until the script finds items hidden that way.
        var sidebar = Render<OmniSidebar>(parameters => parameters
            .Add(component => component.Open, true)
            .AddChildContent("<nav class=\"menu\">Navigation</nav>"));

        var panel = sidebar.Find(".omni-sidebar__panel");
        var children = panel.Children.Select(child => child.ClassName).ToArray();
        Assert.Equal(
            ["omni-sidebar__scroll omni-sidebar__scroll--start", "omni-sidebar__viewport", "omni-sidebar__scroll omni-sidebar__scroll--end"],
            children);
        Assert.NotNull(sidebar.Find(".omni-sidebar__viewport > .menu"));
        foreach (var chevron in sidebar.FindAll(".omni-sidebar__scroll"))
        {
            Assert.True(chevron.HasAttribute("hidden"));
            Assert.Equal("-1", chevron.GetAttribute("tabindex"));
            Assert.Equal("true", chevron.GetAttribute("aria-hidden"));
        }

        var configure = Assert.Single(JSInterop.Invocations, invocation => invocation.Identifier == "configureSidebarOverflow");
        Assert.IsType<ElementReference>(configure.Arguments[0]);

        Assert.Equal("hidden", ShippedLookTests.Value(ShippedLookTests.Body(".omni-sidebar__panel"), "overflow"));
        var viewport = ShippedLookTests.Body(".omni-sidebar__viewport");
        Assert.Equal("auto", ShippedLookTests.Value(viewport, "overflow-y"));
        Assert.Equal("none", ShippedLookTests.Value(viewport, "scrollbar-width"));
        Assert.Equal("none", ShippedLookTests.Value(ShippedLookTests.Body(".omni-sidebar__scroll[hidden]"), "display"));
    }
}
