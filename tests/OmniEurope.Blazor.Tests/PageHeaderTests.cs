using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>The page header and the breadcrumb service it reads its trail and its title from.</summary>
public sealed class PageHeaderTests : OmniBunitContext
{
    private const string InteropModule = "./_content/OmniEurope.Blazor/omniInterop.js";

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
        service.Replace(1, service.Items[1] with { Text = "Aetheus" });
        navigation.NavigateTo("projects/12/overview?tab=logs");
        navigation.NavigateTo("Projects/12/overview/#top");
        Assert.Equal("Aetheus", service.Items[1].Text);

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
    public void Header_TitlesItselfWithTheLastCrumbAndShowsOnlyTheAncestorsInTheTrail()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("projects/12/overview");

        var header = Render<OmniPageHeader>();

        Assert.Equal("Overview", header.Find("h1.omni-page-header__title").TextContent);
        var trail = header.Find(".omni-page-header__trail nav.omni-breadcrumb");
        Assert.Equal("Fil d'Ariane", trail.GetAttribute("aria-label"));
        Assert.Equal(["Projets", "12"], header.FindAll(".omni-breadcrumb__item").Select(item => item.TextContent.Trim()));
        Assert.Equal("/projects", header.Find(".omni-breadcrumb__item a").GetAttribute("href"));
        Assert.DoesNotContain("style=", header.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Header_Framed_IsTheDefault_AndFalseDrawsThePlainVariant()
    {
        var framed = Render<OmniPageHeader>(parameters => parameters
            .Add(component => component.Title, "Projets")
            .Add(component => component.ShowTrail, false));
        var plain = Render<OmniPageHeader>(parameters => parameters
            .Add(component => component.Title, "Projets")
            .Add(component => component.ShowTrail, false)
            .Add(component => component.Framed, false));

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
        Assert.Equal(["omni-page-header__icon", "omni-page-header__title"],
            row.Children.Select(child => child.ClassList.First(name => name.StartsWith("omni-page-header__", StringComparison.Ordinal))));
        Assert.Equal("true", row.Children[0].GetAttribute("aria-hidden"));
        Assert.NotNull(row.Children[0].QuerySelector("svg.omni-icon"));

        var bare = Render<OmniPageHeader>(parameters => parameters.Add(component => component.Title, "Sans icône"));
        Assert.Empty(bare.FindAll(".omni-page-header__icon"));
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

        await header.InvokeAsync(() => service.Replace(1, service.Items[1] with { Text = "Aetheus" }));
        header.WaitForAssertion(() => Assert.Contains("Aetheus", header.Find(".omni-page-header__trail").TextContent, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Header_CrumbsStillLoading_ShowPlaceholdersInsteadOfTextOrTitle()
    {
        var service = Services.GetRequiredService<OmniBreadcrumbService>();
        service.Set(new OmniBreadcrumbEntry("Serveurs", "/servers"), new OmniBreadcrumbEntry("Serveur", Loading: true), new OmniBreadcrumbEntry("Vue", Loading: true));

        var header = Render<OmniPageHeader>();

        Assert.Empty(header.FindAll("h1"));
        Assert.Equal("status", header.Find(".omni-page-header__title-loading").GetAttribute("role"));
        var crumbs = header.FindAll(".omni-breadcrumb__item");
        Assert.Equal(2, crumbs.Count);
        Assert.Contains("omni-page-header__crumb-loading", crumbs[1].ClassName, StringComparison.Ordinal);
        Assert.DoesNotContain("Serveur", crumbs[1].TextContent, StringComparison.Ordinal);

        await header.InvokeAsync(() => service.Replace(2, new OmniBreadcrumbEntry("Vue d'ensemble")));
        header.WaitForAssertion(() => Assert.Equal("Vue d'ensemble", header.Find("h1").TextContent));
    }

    [Fact]
    public void Header_WithoutAncestors_KeepsTheTrailLineWithoutAnEmptyLandmark_AndShowTrailRemovesIt()
    {
        var service = Services.GetRequiredService<OmniBreadcrumbService>();
        service.Set(new OmniBreadcrumbEntry("Tableau de bord"));

        var header = Render<OmniPageHeader>();
        Assert.Single(header.FindAll(".omni-page-header__trail"));
        Assert.Empty(header.FindAll("nav"));

        var bare = Render<OmniPageHeader>(parameters => parameters.Add(component => component.ShowTrail, false));
        Assert.Empty(bare.FindAll(".omni-page-header__trail"));
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
        Assert.Equal("Afficher les badges et les actions", toggle.GetAttribute("aria-label"));
        Assert.Equal("En ligne", header.Find(".omni-page-header__badges").TextContent);
        Assert.Equal("Modifier", header.Find(".omni-page-header__actions").TextContent);
        Assert.Equal("Filtres", header.Find(".omni-page-header__filters").TextContent);

        toggle.Click();
        Assert.Equal("true", header.Find(".omni-page-header__toggle").GetAttribute("aria-expanded"));
        Assert.Equal("Masquer les badges et les actions", header.Find(".omni-page-header__toggle").GetAttribute("aria-label"));
        Assert.Contains("omni-page-header__details--open", header.Find(".omni-page-header__details").ClassName, StringComparison.Ordinal);

        var plain = Render<OmniPageHeader>(parameters => parameters.Add(component => component.Title, "Sans actions"));
        Assert.Empty(plain.FindAll(".omni-page-header__toggle"));
        Assert.Empty(plain.FindAll(".omni-page-header__details"));
    }

    [Theory]
    [InlineData("fr-FR", "Retour", "Afficher les badges et les actions")]
    [InlineData("en-US", "Back", "Show badges and actions")]
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
            .Add(component => component.PanelId, "corps-assistant"));

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
