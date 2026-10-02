using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>The page header and the breadcrumb service it reads its trail and its title from.</summary>
public sealed class PageHeaderTests : OmniBunitContext
{
    private const string InteropModule = Internal.OmniModules.Interop;

    public PageHeaderTests()
    {
        Services.AddSingleton<IOmniBreadcrumbResolver>(new RouteResolver());
    }

    [Fact]
    public void Service_InstallsTheResolvedTrailAndReplacesItOnlyWhenThePathChanges()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("projects/12/overview");
        var service = Services.GetRequiredService<OmniBreadcrumbService>();
        var changes = 0;
        service.Changed += () => changes++;

        Assert.Equal(["Projets", "12", "Overview"], service.Items.Select(entry => entry.Text));
        Assert.Equal("Overview", service.Current?.Text);
        Assert.Equal(["Projets", "12"], service.Ancestors.Select(entry => entry.Text));

        // The page names its entity once loaded; a tab or a filter (query only), a trailing slash or
        // another letter case is still that page, so the name survives.
        service.Replace(1, service.Items[1] with { Text = "Boutique" });
        navigation.NavigateTo("projects/12/overview?tab=logs");
        navigation.NavigateTo("Projects/12/overview/#top");
        Assert.Equal("Boutique", service.Items[1].Text);

        navigation.NavigateTo("servers");
        Assert.Equal(["Serveurs"], service.Items.Select(entry => entry.Text));
        Assert.Equal(2, changes);
    }

    [Fact]
    public void Service_ParentHrefSkipsTheCurrentPageAndCrumbsWithoutLink()
    {
        var service = Services.GetRequiredService<OmniBreadcrumbService>();

        service.Set(new OmniBreadcrumbEntry("Accueil", "/"), new OmniBreadcrumbEntry("Section"), new OmniBreadcrumbEntry("Page", "/page"));
        Assert.Equal("/", service.ParentHref);

        service.Push(new OmniBreadcrumbEntry("Détail"));
        Assert.Equal("/page", service.ParentHref);
        Assert.Equal("Détail", service.Current?.Text);

        service.Set(new OmniBreadcrumbEntry("Seul", "/seul"));
        Assert.Null(service.ParentHref);
        Assert.Empty(service.Ancestors);

        Assert.Throws<ArgumentOutOfRangeException>(() => service.Replace(3, new OmniBreadcrumbEntry("x")));
    }

    [Fact]
    public void Service_WithoutResolver_StartsEmptyAndResetGoesBackToTheRouteTrail()
    {
        using var context = new BunitContext();
        context.Services.AddLogging();
        context.Services.AddOmniEuropeBlazor();
        var bare = context.Services.GetRequiredService<OmniBreadcrumbService>();
        Assert.Empty(bare.Items);

        var service = Services.GetRequiredService<OmniBreadcrumbService>();
        Services.GetRequiredService<NavigationManager>().NavigateTo("servers");
        service.Set(new OmniBreadcrumbEntry("Autre"));
        service.Reset();
        Assert.Equal(["Serveurs"], service.Items.Select(entry => entry.Text));
    }

    [Fact]
    public void Header_TitlesItselfWithTheLastCrumb_AndTheTrailUnderItEndsWithThatTitle()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("projects/12/overview");

        var header = Render<OmniPageHeader>();

        Assert.Equal("Overview", header.Find("h1.omni-page-header__title").TextContent);
        var trail = header.Find(".omni-page-header__trail nav.omni-breadcrumb");
        Assert.Equal("Fil d'Ariane", trail.GetAttribute("aria-label"));
        Assert.Equal(["Projets", "12", "Overview"], header.FindAll(".omni-breadcrumb__item").Select(item => item.TextContent.Trim()));
        Assert.Equal("/projects", header.Find(".omni-breadcrumb__item a").GetAttribute("href"));
        // The last item is the page itself: the title, not a link, marked as the current page.
        var last = header.FindAll(".omni-breadcrumb__item")[^1];
        Assert.Empty(last.QuerySelectorAll("a"));
        Assert.Equal("page", last.QuerySelector("span")!.GetAttribute("aria-current"));
        Assert.DoesNotContain("style=", header.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Header_Line1IsTheTitleRow_AndLine2UnderItIsTheTrail()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("projects/12/overview");

        var header = Render<OmniPageHeader>(parameters => parameters.Add(component => component.ShowBack, true));

        var frame = header.Find(".omni-page-header__frame");
        Assert.Equal(["omni-page-header__row", "omni-page-header__trail"], frame.Children.Select(child => child.ClassName));
        Assert.NotNull(frame.Children[0].QuerySelector(".omni-page-header__back"));
        Assert.NotNull(frame.Children[0].QuerySelector("h1.omni-page-header__title"));
        Assert.NotNull(frame.Children[1].QuerySelector("nav.omni-breadcrumb"));
    }

    [Fact]
    public void Header_WithoutAncestors_WritesTheSubtitleOnLine2_AndWithAncestorsUnderTheBlock()
    {
        var service = Services.GetRequiredService<OmniBreadcrumbService>();
        service.Set(new OmniBreadcrumbEntry("Tableau de bord"));

        var alone = Render<OmniPageHeader>(parameters => parameters.Add(component => component.Subtitle, "Vue d'ensemble du parc."));
        Assert.Equal("Vue d'ensemble du parc.", alone.Find(".omni-page-header__trail .omni-page-header__trail-text").TextContent);
        Assert.Empty(alone.FindAll(".omni-page-header__subtitle"));
        Assert.Empty(alone.FindAll("nav"));

        service.Set(new OmniBreadcrumbEntry("Serveurs", "/servers"), new OmniBreadcrumbEntry("srv-01"));
        var nested = Render<OmniPageHeader>(parameters => parameters.Add(component => component.Subtitle, "Machine virtuelle."));
        Assert.Empty(nested.FindAll(".omni-page-header__trail-text"));
        Assert.Equal("Machine virtuelle.", nested.Find(".omni-page-header__frame + .omni-page-header__subtitle").TextContent);
    }

    [Fact]
    public void Title_HasAScrollButtonOnEachSide_AndTheHeaderAttachesTheTitleScroll()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.PageHeader);
        module.SetupVoid("attach", _ => true).SetVoidResult();

        var header = Render<OmniPageHeader>(parameters => parameters.Add(component => component.Title, "Un titre bien trop long pour sa ligne"));

        var row = header.Find(".omni-page-header__row");
        var start = row.QuerySelector(".omni-page-header__scroll--start")!;
        var end = row.QuerySelector(".omni-page-header__scroll--end")!;
        Assert.Equal("Faire défiler le titre vers le début", start.GetAttribute("aria-label"));
        Assert.Equal("Faire défiler le titre vers la fin", end.GetAttribute("aria-label"));
        Assert.Same(row.QuerySelector(".omni-page-header__title"), start.NextElementSibling);
        Assert.Same(end, row.QuerySelector(".omni-page-header__title")!.NextElementSibling);
        module.VerifyInvoke("attach");
    }

    [Fact]
    public void MenuContent_PutsAnOverflowMenuAtTheEndOfLine1_OutsideTheFoldedDetails()
    {
        var header = Render<OmniPageHeader>(parameters => parameters
            .Add(component => component.Title, "Serveur")
            .Add(component => component.Actions, builder => builder.AddContent(0, "Modifier"))
            .Add(component => component.MenuContent, builder => builder.AddContent(0, "Dupliquer")));

        var row = header.Find(".omni-page-header__row");
        var menu = row.Children[^1];
        Assert.Contains("omni-page-header__menu", menu.ClassList);
        Assert.Contains("omni-overflow-menu", menu.ClassList);
        Assert.Empty(header.Find(".omni-page-header__details").QuerySelectorAll(".omni-overflow-menu"));

        var bare = Render<OmniPageHeader>(parameters => parameters.Add(component => component.Title, "Sans menu"));
        Assert.Empty(bare.FindAll(".omni-overflow-menu"));
    }

    [Fact]
    public void Header_Plain_IsTheDefault_AndFramedDrawsTheBorderedBlock()
    {
        var plain = Render<OmniPageHeader>(parameters => parameters
            .Add(component => component.Title, "Projets")
            .Add(component => component.ShowTrail, false));
        var framed = Render<OmniPageHeader>(parameters => parameters
            .Add(component => component.Title, "Projets")
            .Add(component => component.ShowTrail, false)
            .Add(component => component.Framed, true));

        Assert.False(framed.Find(".omni-page-header__frame").ClassList.Contains("omni-page-header__frame--plain"));
        Assert.True(plain.Find(".omni-page-header__frame").ClassList.Contains("omni-page-header__frame--plain"));
    }

    [Fact]
    public void Header_Icon_IsDrawnBeforeTheTitle_AndHiddenFromAssistiveTechnology()
    {
        var header = Render<OmniPageHeader>(parameters => parameters
            .Add(component => component.Title, "Tableau de bord")
            .Add(component => component.ShowTrail, false)
            .Add(component => component.Icon, (RenderFragment)(icon =>
            {
                icon.OpenComponent<OmniIcon>(0);
                icon.AddComponentParameter(1, nameof(OmniIcon.Name), OmniIconName.Home);
                icon.CloseComponent();
            })));

        var row = header.Find(".omni-page-header__row");
        Assert.Equal(["omni-page-header__icon", "omni-page-header__scroll", "omni-page-header__title", "omni-page-header__scroll"],
            row.Children.Select(child => child.ClassList.First(name => name.StartsWith("omni-page-header__", StringComparison.Ordinal))));
        Assert.Equal("true", row.Children[0].GetAttribute("aria-hidden"));
        Assert.NotNull(row.Children[0].QuerySelector("svg.omni-icon"));

        var bare = Render<OmniPageHeader>(parameters => parameters.Add(component => component.Title, "Sans icône"));
        Assert.Empty(bare.FindAll(".omni-page-header__icon"));

        // The icon box shares the font and line height of the title, and the icon sits on the middle of
        // the capitals through ex and cap units (review point 95).
        Assert.Contains("omni-heading--h1", row.Children[0].ClassList);
        var icon = ShippedLookTests.Body(".omni-page-header__icon > *");
        Assert.Equal("calc(0.5ex - 0.5cap)", ShippedLookTests.Value(icon, "inset-block-start"));
        Assert.Equal("middle", ShippedLookTests.Value(icon, "vertical-align"));
        Assert.Equal("var(--omni-page-header-line)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-page-header .omni-page-header__icon"), "line-height"));
    }

    [Fact]
    public async Task Header_ExplicitTitleWins_AndARenamedCrumbRedrawsTheHeader()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("projects/12/overview");
        var service = Services.GetRequiredService<OmniBreadcrumbService>();

        var header = Render<OmniPageHeader>(parameters => parameters
            .Add(component => component.Title, "Vue d'ensemble")
            .Add(component => component.Level, OmniHeadingLevel.H2)
            .Add(component => component.Subtitle, "Le projet en un coup d'oeil."));

        Assert.Equal("Vue d'ensemble", header.Find("h2").TextContent);
        Assert.Equal("Le projet en un coup d'oeil.", header.Find(".omni-page-header__subtitle").TextContent);

        await header.InvokeAsync(() => service.Replace(1, service.Items[1] with { Text = "Boutique" }));
        header.WaitForAssertion(() => Assert.Contains("Boutique", header.Find(".omni-page-header__trail").TextContent, StringComparison.Ordinal));
    }

    [Fact]
    public void LoadingCrumb_SitsOnTheLineOfTheOthers()
    {
        // A list item holding the skeleton's grid grew to twice the trail, its slash on top (recette R2-035).
        var crumb = ShippedLookTests.Body(".omni-page-header__crumb-loading");
        Assert.Equal("inline-flex", ShippedLookTests.Value(crumb, "display"));
        Assert.Equal("center", ShippedLookTests.Value(crumb, "align-items"));
    }

    [Fact]
    public async Task Header_CrumbsStillLoading_ShowPlaceholdersInsteadOfTextOrTitle()
    {
        var service = Services.GetRequiredService<OmniBreadcrumbService>();
        service.Set(new OmniBreadcrumbEntry("Serveurs", "/servers"), new OmniBreadcrumbEntry("Serveur", Loading: true), new OmniBreadcrumbEntry("Vue", Loading: true));

        var header = Render<OmniPageHeader>();

        Assert.Empty(header.FindAll("h1"));
        Assert.Equal("status", header.Find(".omni-page-header__title-loading").GetAttribute("role"));
        // The ancestors, then the title as last crumb: both still loading show a placeholder.
        var crumbs = header.FindAll(".omni-breadcrumb__item");
        Assert.Equal(3, crumbs.Count);
        Assert.Contains("omni-page-header__crumb-loading", crumbs[1].ClassName, StringComparison.Ordinal);
        Assert.Contains("omni-page-header__crumb-loading", crumbs[2].ClassName, StringComparison.Ordinal);
        Assert.DoesNotContain("Serveur", crumbs[1].TextContent, StringComparison.Ordinal);

        await header.InvokeAsync(() => service.Replace(2, new OmniBreadcrumbEntry("Vue d'ensemble")));
        header.WaitForAssertion(() => Assert.Equal("Vue d'ensemble", header.Find("h1").TextContent));
    }

    [Fact]
    public void Header_Line2IsAlwaysReserved_WithoutAnEmptyLandmark_AndShowTrailOnlyRemovesTheBreadcrumb()
    {
        var service = Services.GetRequiredService<OmniBreadcrumbService>();
        service.Set(new OmniBreadcrumbEntry("Tableau de bord"));

        var header = Render<OmniPageHeader>();
        Assert.Single(header.FindAll(".omni-page-header__trail"));
        Assert.Empty(header.FindAll("nav"));

        service.Set(new OmniBreadcrumbEntry("Serveurs", "/servers"), new OmniBreadcrumbEntry("srv-01"));
        var bare = Render<OmniPageHeader>(parameters => parameters
            .Add(component => component.ShowTrail, false)
            .Add(component => component.Subtitle, "Machine virtuelle."));
        Assert.Single(bare.FindAll(".omni-page-header__trail"));
        Assert.Empty(bare.FindAll("nav"));
        Assert.Equal("Machine virtuelle.", bare.Find(".omni-page-header__trail-text").TextContent);
    }

    [Fact]
    public void BackButton_GoesToBackHrefThenToTheNearestLinkedAncestor()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("projects/12/overview");

        var explicitBack = Render<OmniPageHeader>(parameters => parameters
            .Add(component => component.ShowBack, true)
            .Add(component => component.BackHref, "/elsewhere"));
        var button = explicitBack.Find(".omni-page-header__back");
        Assert.Equal("Retour", button.GetAttribute("aria-label"));
        button.Click();
        Assert.EndsWith("/elsewhere", navigation.Uri, StringComparison.Ordinal);

        navigation.NavigateTo("projects/12/overview");
        var trailBack = Render<OmniPageHeader>(parameters => parameters
            .Add(component => component.ShowBack, true)
            .Add(component => component.BackLabel, "Revenir au projet"));
        Assert.Equal("Revenir au projet", trailBack.Find(".omni-page-header__back").GetAttribute("title"));
        trailBack.Find(".omni-page-header__back").Click();
        Assert.EndsWith("/projects/12", navigation.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void BackButton_WithNowhereToGo_StepsBackInTheBrowserHistory()
    {
        var module = JSInterop.SetupModule(InteropModule);
        module.SetupVoid("historyBack").SetVoidResult();
        Services.GetRequiredService<OmniBreadcrumbService>().Set(new OmniBreadcrumbEntry("Page"));

        var header = Render<OmniPageHeader>(parameters => parameters.Add(component => component.ShowBack, true));
        header.Find(".omni-page-header__back").Click();

        module.VerifyInvoke("historyBack");
    }

    [Fact]
    public void Details_FoldBehindAToggleThatSaysWhatItControls()
    {
        var header = Render<OmniPageHeader>(parameters => parameters
            .Add(component => component.Title, "Serveur")
            .Add(component => component.Badges, builder => builder.AddContent(0, "En ligne"))
            .Add(component => component.Actions, builder => builder.AddContent(0, "Modifier"))
            .Add(component => component.Filters, builder => builder.AddContent(0, "Filtres")));

        var toggle = header.Find(".omni-page-header__toggle");
        var details = header.Find(".omni-page-header__details");
        Assert.Equal(details.Id, toggle.GetAttribute("aria-controls"));
        Assert.Equal("false", toggle.GetAttribute("aria-expanded"));
        Assert.Equal("Voir plus", toggle.GetAttribute("aria-label"));
        Assert.Equal("En ligne", header.Find(".omni-page-header__badges").TextContent);
        Assert.Equal("Modifier", header.Find(".omni-page-header__actions").TextContent);
        Assert.Equal("Filtres", header.Find(".omni-page-header__filters").TextContent);

        toggle.Click();
        // The name stays "Show more"; aria-expanded alone says the details are now shown.
        Assert.Equal("true", header.Find(".omni-page-header__toggle").GetAttribute("aria-expanded"));
        Assert.Equal("Voir plus", header.Find(".omni-page-header__toggle").GetAttribute("aria-label"));
        Assert.Contains("omni-page-header__details--open", header.Find(".omni-page-header__details").ClassName, StringComparison.Ordinal);

        var plain = Render<OmniPageHeader>(parameters => parameters.Add(component => component.Title, "Sans actions"));
        Assert.Empty(plain.FindAll(".omni-page-header__toggle"));
        Assert.Empty(plain.FindAll(".omni-page-header__details"));
    }

    [Fact]
    public void Details_FoldAtAnyWidthOnceTheBlockIsMarkedCompact_WithEveryRuleOfThePhoneFold()
    {
        var css = ShippedLookTests.Css;
        var phone = System.Text.RegularExpressions.Regex.Matches(css, @"@media \(max-width: 39\.99rem\) \{(?<body>(?:[^{}]*\{[^{}]*\})*)\s*\}")
            .Select(match => match.Groups["body"].Value)
            .Single(body => body.Contains(".omni-page-header__toggle", StringComparison.Ordinal));
        var rules = System.Text.RegularExpressions.Regex.Matches(phone, @"(?<selector>[^{}]+?)\s*\{\s*(?<declarations>[^{}]*?)\s*\}")
            .Select(match => (Selector: match.Groups["selector"].Value.Trim(), Declarations: match.Groups["declarations"].Value))
            .ToList();
        Assert.Contains(rules, rule => rule.Selector == ".omni-page-header__toggle");

        // omni-page-header.js marks the frame data-compact when line 1 has no room for the badges and
        // actions: each rule of the phone fold then applies at any width, keyed on that attribute.
        foreach (var (selector, declarations) in rules)
        {
            var compact = selector.StartsWith(".omni-page-header__frame", StringComparison.Ordinal)
                ? selector.Insert(".omni-page-header__frame".Length, "[data-compact]")
                : ".omni-page-header__frame[data-compact] " + (selector.StartsWith(".omni-page-header .", StringComparison.Ordinal)
                    ? selector[".omni-page-header ".Length..]
                    : selector);
            Assert.Contains($"{compact} {{ {declarations} }}", css, StringComparison.Ordinal);
        }

        // The "⋮" menu is never folded, compact or not.
        Assert.DoesNotMatch(@"\.omni-page-header__menu[^{]*\{[^}]*display: none", css);
    }

    [Fact]
    public void CompactScript_MeasuresLine1Unfolded_AndWritesTheAttributeOnly()
    {
        var script = File.ReadAllText(Path.Combine(ShippedLookTests.RepositoryRoot(), "src", "OmniEurope.Blazor", "wwwroot", "omni-page-header.js"));

        Assert.Contains("frame.removeAttribute('data-compact')", script, StringComparison.Ordinal);
        Assert.Contains("frame.toggleAttribute('data-compact', crowded)", script, StringComparison.Ordinal);
        Assert.Contains("new ResizeObserver(queue)", script, StringComparison.Ordinal);
        // Strict CSP: the module toggles attributes; it writes no style of its own.
        Assert.DoesNotContain(".style", script, StringComparison.Ordinal);
        Assert.DoesNotContain("'style'", script, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Header_Disposed_DetachesTheScriptFromItsFrame()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.PageHeader);
        module.SetupVoid("attach", _ => true).SetVoidResult();
        module.SetupVoid("detach", _ => true).SetVoidResult();
        var header = Render<OmniPageHeader>(parameters => parameters
            .Add(component => component.Title, "Serveur")
            .Add(component => component.Actions, builder => builder.AddContent(0, "Modifier")));
        var attached = module.VerifyInvoke("attach").Arguments[0];

        await DisposeComponentsAsync();

        Assert.Equal(attached, module.VerifyInvoke("detach").Arguments[0]);
    }

    [Theory]
    [InlineData("fr-FR", "Retour", "Voir plus")]
    [InlineData("en-US", "Back", "Show more")]
    public void Header_DefaultsFollowTheUiCulture(string cultureName, string back, string toggle)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            var header = Render<OmniPageHeader>(parameters => parameters
                .Add(component => component.Title, "Page")
                .Add(component => component.ShowBack, true)
                .Add(component => component.Actions, builder => builder.AddContent(0, "Action")));

            Assert.Equal(back, header.Find(".omni-page-header__back").GetAttribute("aria-label"));
            Assert.Equal(toggle, header.Find(".omni-page-header__toggle").GetAttribute("aria-label"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void StepsItem_WithAnExternalPanel_RendersNoPanelOfItsOwnAndControlsThatOne()
    {
        var item = Render<OmniStepsItem>(parameters => parameters
            .Add(component => component.Index, 0)
            .Add(component => component.Title, "Début")
            .AddCascadingValue(new OmniStepsSharedPanel("corps-assistant")));

        Assert.Equal("corps-assistant", item.Find("button").GetAttribute("aria-controls"));
        Assert.Empty(item.FindAll("section"));
    }

    /// <summary>A host resolver: each segment is a crumb linking to its own path, the first ones named.</summary>
    private sealed class RouteResolver : IOmniBreadcrumbResolver
    {
        public IReadOnlyList<OmniBreadcrumbEntry> Resolve(string relativePath)
        {
            if (relativePath.Length == 0)
            {
                return [new OmniBreadcrumbEntry("Accueil")];
            }

            var segments = relativePath.Split('/');
            return
            [
                .. segments.Select((segment, index) => new OmniBreadcrumbEntry(
                    segment switch
                    {
                        "projects" => "Projets",
                        "servers" => "Serveurs",
                        "overview" => "Overview",
                        _ => segment
                    },
                    "/" + string.Join('/', segments.Take(index + 1))))
            ];
        }
    }
}
