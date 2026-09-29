using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The PLAN-007 coherence pass on the navigation and overlay families: a link that opens a new tab
/// keeps its protection whatever <c>rel</c> the host adds, the arrow order of the tabs comes from the
/// tabs themselves, a group of the panel menu reports its hand toggles, and the optional texts fall
/// back to the localized defaults when left null.
/// </summary>
public sealed class NavigationOverlayCoherenceTests : OmniBunitContext
{
    public NavigationOverlayCoherenceTests()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Focus);
        module.SetupVoid("configureTabs", _ => true);
        module.SetupVoid("disposeTabs", _ => true);
    }

    // ---- OmniLink ------------------------------------------------------------------------------

    [Fact]
    public void Link_NewTab_KeepsNoopenerAndNoreferrer_WhenTheHostAddsItsOwnRel()
    {
        var link = Render<OmniLink>(parameters => parameters
            .Add(component => component.Href, "https://example.org")
            .Add(component => component.NewTab, true)
            .AddUnmatched("rel", "nofollow")
            .AddChildContent("Ailleurs"));

        var anchor = link.Find("a");
        Assert.Equal("_blank", anchor.GetAttribute("target"));
        Assert.Equal(["nofollow", "noopener", "noreferrer"], anchor.GetAttribute("rel")!.Split(' '));
    }

    [Fact]
    public void Link_AHostTargetBlank_GetsTheProtectionToo_AndASameTabLinkKeepsTheHostRel()
    {
        var blank = Render<OmniLink>(parameters => parameters
            .Add(component => component.Href, "https://example.org")
            .AddUnmatched("target", "_blank")
            .AddChildContent("Ailleurs"));
        Assert.Equal("_blank", blank.Find("a").GetAttribute("target"));
        Assert.Equal("noopener noreferrer", blank.Find("a").GetAttribute("rel"));

        var same = Render<OmniLink>(parameters => parameters
            .Add(component => component.Href, "documentation")
            .AddUnmatched("rel", "help")
            .AddChildContent("Aide"));
        Assert.Equal("help", same.Find("a").GetAttribute("rel"));
        Assert.False(same.Find("a").HasAttribute("target"));

        var plain = Render<OmniLink>(parameters => parameters
            .Add(component => component.Href, "documentation")
            .AddChildContent("Aide"));
        Assert.False(plain.Find("a").HasAttribute("rel"));
    }

    [Fact]
    public void Link_Label_NamesTheLink_AndTheNewTabNoticeIsLocalized()
    {
        var link = Render<OmniLink>(parameters => parameters
            .Add(component => component.Href, "https://example.org")
            .Add(component => component.NewTab, true)
            .Add(component => component.Label, "Site du projet")
            .AddChildContent("Site"));

        Assert.Equal("Site du projet", link.Find("a").GetAttribute("aria-label"));
        Assert.False(string.IsNullOrWhiteSpace(link.Find(".omni-visually-hidden").TextContent));
    }

    // ---- OmniTabs ------------------------------------------------------------------------------

    [Fact]
    public void Tabs_ArrowOrderComesFromTheTabs_AndSkipsADisabledOne()
    {
        string? value = "a";
        var tabs = Render<OmniTabs>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, next => value = next)
            .AddChildContent(builder =>
            {
                AddTab(builder, 0, "a", "Premier", disabled: false);
                AddTab(builder, 10, "b", "Deuxième", disabled: true);
                AddTab(builder, 20, "c", "Troisième", disabled: false);
            }));

        tabs.Find(".omni-tabs__viewport").KeyDown("ArrowRight");
        Assert.Equal("c", value);

        tabs.Render(parameters => parameters.Add(component => component.Value, value));
        tabs.Find(".omni-tabs__viewport").KeyDown("ArrowRight");
        Assert.Equal("a", value);

        tabs.Render(parameters => parameters.Add(component => component.Value, value));
        tabs.Find(".omni-tabs__viewport").KeyDown("End");
        Assert.Equal("c", value);
    }

    [Fact]
    public void TabsItem_WithoutKey_HandsTheScriptItsTitleAsTheKey()
    {
        var tabs = Render<OmniTabs>(parameters => parameters
            .AddChildContent<OmniTabsItem>(item => item.Add(component => component.Title, "Général").AddChildContent("Contenu")));

        // The script selects a tab by its data-key: an item without Key is registered under its title,
        // so the key it hands over has to be the title, not an empty string.
        Assert.Equal("Général", tabs.Find(".omni-tabs__tab").GetAttribute("data-key"));
    }

    // ---- OmniPanelMenuItem ---------------------------------------------------------------------

    [Fact]
    public void PanelMenuGroup_ReportsItsHandToggles_AndFollowsANewValueFromTheHost()
    {
        var reported = new List<bool>();
        var menu = Render<OmniPanelMenu>(parameters => parameters
            .AddChildContent<OmniPanelMenuItem>(item => item
                .Add(component => component.Text, "Section")
                .Add(component => component.Id, "group")
                .Add(component => component.ExpandedChanged, value => reported.Add(value))
                .AddChildContent<OmniPanelMenuItem>(child => child.Add(component => component.Text, "Entrée"))));

        Assert.Contains("omni-panel-menu__group--closed", menu.Find("#group").ClassName, StringComparison.Ordinal);
        menu.Find("#group .omni-panel-menu__toggle").Click();
        Assert.Contains("omni-panel-menu__group--open", menu.Find("#group").ClassName, StringComparison.Ordinal);
        menu.Find("#group .omni-panel-menu__toggle").Click();
        Assert.Equal([true, false], reported);
    }

    [Fact]
    public void PanelMenuGroup_BoundExpanded_OpensAndClosesWhenTheHostChangesIt()
    {
        var host = Render<PanelMenuExpandedHost>();
        Assert.Contains("omni-panel-menu__group--closed", host.Find("#bound").ClassName, StringComparison.Ordinal);

        host.InvokeAsync(() => host.Instance.Set(true));
        Assert.Contains("omni-panel-menu__group--open", host.Find("#bound").ClassName, StringComparison.Ordinal);

        host.Find("#bound .omni-panel-menu__toggle").Click();
        Assert.False(host.Instance.Expanded);
        Assert.Contains("omni-panel-menu__group--closed", host.Find("#bound").ClassName, StringComparison.Ordinal);
    }

    // ---- Optional texts: null falls back to the localized default ------------------------------

    [Fact]
    public void OptionalLabels_LeftNull_UseTheLocalizedDefaults()
    {
        var breadcrumb = Render<OmniBreadcrumb>();
        var steps = Render<OmniSteps>();
        var panelMenu = Render<OmniPanelMenu>();
        var sidebar = Render<OmniSidebar>(parameters => parameters.AddChildContent("Menu"));
        var toggle = Render<OmniSidebarToggle>(parameters => parameters.Add(component => component.Controls, "nav"));
        var window = Render<OmniWindowControls>(parameters => parameters.Add(component => component.OnClose, () => { }));

        Assert.False(string.IsNullOrWhiteSpace(breadcrumb.Find("nav").GetAttribute("aria-label")));
        Assert.False(string.IsNullOrWhiteSpace(steps.Find(".omni-steps__list").GetAttribute("aria-label")));
        Assert.False(string.IsNullOrWhiteSpace(panelMenu.Find("nav").GetAttribute("aria-label")));
        Assert.False(string.IsNullOrWhiteSpace(sidebar.Find("aside").GetAttribute("aria-label")));
        Assert.False(string.IsNullOrWhiteSpace(toggle.Find("button").GetAttribute("aria-label")));
        Assert.False(string.IsNullOrWhiteSpace(window.Find("[role=group]").GetAttribute("aria-label")));
        Assert.False(string.IsNullOrWhiteSpace(window.Find(".omni-window-controls__button--close").GetAttribute("aria-label")));

        var named = Render<OmniSidebar>(parameters => parameters
            .Add(component => component.Label, "Navigation du dossier")
            .AddChildContent("Menu"));
        Assert.Equal("Navigation du dossier", named.Find("aside").GetAttribute("aria-label"));
        var namedWindow = Render<OmniWindowControls>(parameters => parameters
            .Add(component => component.Label, "Fenêtre")
            .Add(component => component.OnClose, () => { }));
        Assert.Equal("Fenêtre", namedWindow.Find("[role=group]").GetAttribute("aria-label"));
    }

    [Fact]
    public void Notification_CloseLabel_NamesTheCloseButton_ElseTheLocalizedDefault()
    {
        var named = Render<OmniNotification>(parameters => parameters
            .Add(component => component.Message, "Enregistré")
            .Add(component => component.CloseLabel, "Masquer"));
        var close = named.Find(".omni-notification__dismiss");
        Assert.Equal("Masquer", close.GetAttribute("aria-label"));
        Assert.Equal("Masquer", close.GetAttribute("title"));

        var bare = Render<OmniNotification>(parameters => parameters.Add(component => component.Message, "Enregistré"));
        Assert.False(string.IsNullOrWhiteSpace(bare.Find(".omni-notification__dismiss").GetAttribute("aria-label")));
        Assert.NotEqual("Masquer", bare.Find(".omni-notification__dismiss").GetAttribute("aria-label"));
    }

    [Theory]
    [InlineData(OmniSeverity.Info)]
    [InlineData(OmniSeverity.Success)]
    [InlineData(OmniSeverity.Warning)]
    [InlineData(OmniSeverity.Danger)]
    public void Notification_AndAlert_DrawTheSameGlyphForTheSameSeverity(OmniSeverity severity)
    {
        var notification = Render<OmniNotification>(parameters => parameters
            .Add(component => component.Message, "Message")
            .Add(component => component.Severity, severity));
        var alert = Render<OmniAlert>(parameters => parameters
            .Add(component => component.Severity, severity)
            .AddChildContent("Message"));

        Assert.Equal(
            alert.Find(".omni-alert__glyph path").GetAttribute("d"),
            notification.Find(".omni-notification__glyph path").GetAttribute("d"));
    }

    [Fact]
    public void Dialog_CloseLabelLeftNull_IsTheLocalizedClose_AndTheButtonIsAnIconWithATooltip()
    {
        var dialog = Render<OmniDialog>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Title, "Confirmation"));

        var close = dialog.Find(".omni-dialog__close");
        Assert.False(string.IsNullOrWhiteSpace(close.GetAttribute("aria-label")));
        Assert.Equal(close.GetAttribute("aria-label"), close.GetAttribute("title"));
        Assert.NotNull(close.QuerySelector("svg.omni-icon"));
        Assert.True(close.HasAttribute("autofocus"));
    }

    private static void AddTab(RenderTreeBuilder builder, int sequence, string key, string title, bool disabled)
    {
        builder.OpenComponent<OmniTabsItem>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(OmniTabsItem.Key), key);
        builder.AddComponentParameter(sequence + 2, nameof(OmniTabsItem.Title), title);
        builder.AddComponentParameter(sequence + 3, nameof(OmniTabsItem.Disabled), disabled);
        builder.AddComponentParameter(sequence + 4, nameof(OmniTabsItem.ChildContent), (RenderFragment)(content => content.AddContent(0, title)));
        builder.CloseComponent();
    }
}
