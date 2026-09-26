using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>The detail shell, the empty state, the sign-in shell, the return address and the description list.</summary>
public sealed class PageLayoutTests : OmniBunitContext
{
    [Fact]
    public void DetailShell_WhileLoading_PaintsTheHeaderAndKeepsTheContentMountedButHidden()
    {
        var shell = RenderShell(OmniDetailState.Loading);

        Assert.Equal("true", shell.Find(".omni-detail-shell").GetAttribute("aria-busy"));
        Assert.Equal("En-tête", shell.Find(".host-header").TextContent);
        Assert.Equal("status", shell.Find(".omni-detail-shell__loading").GetAttribute("role"));
        // Mounted, so a child that starts the load from its own lifecycle does start it.
        var content = shell.Find(".omni-detail-shell__content");
        Assert.True(content.HasAttribute("hidden"));
        Assert.Equal("Contenu", content.TextContent);
        Assert.DoesNotContain("style=", shell.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DetailShell_OnceFound_ShowsHeaderAndContentWithoutPlaceholder()
    {
        var shell = RenderShell(OmniDetailState.Found);

        Assert.Null(shell.Find(".omni-detail-shell").GetAttribute("aria-busy"));
        Assert.Single(shell.FindAll(".host-header"));
        Assert.Empty(shell.FindAll(".omni-detail-shell__loading"));
        Assert.False(shell.Find(".omni-detail-shell__content").HasAttribute("hidden"));
    }

    [Fact]
    public void DetailShell_NotFound_ReplacesTheHeaderWithAWayBack()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        var shell = RenderShell(OmniDetailState.NotFound, parameters => parameters
            .Add(component => component.BackHref, "/servers")
            .Add(component => component.BackText, "Retour aux serveurs")
            .Add(component => component.NotFoundDescription, "Le serveur a été retiré."));

        Assert.Empty(shell.FindAll(".host-header"));
        Assert.Equal("Élément introuvable", shell.Find(".omni-detail-shell__not-found h2").TextContent);
        Assert.Equal("Le serveur a été retiré.", shell.Find(".omni-empty-state__description").TextContent);
        Assert.True(shell.Find(".omni-detail-shell__content").HasAttribute("hidden"));

        var back = shell.Find(".omni-detail-shell__not-found button");
        Assert.Contains("Retour aux serveurs", back.TextContent, StringComparison.Ordinal);
        back.Click();
        Assert.EndsWith("/servers", navigation.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void DetailShell_CustomNotFoundAndLoadingContent_ReplaceTheDefaults()
    {
        var loading = RenderShell(OmniDetailState.Loading, parameters => parameters
            .Add(component => component.LoadingContent, builder => builder.AddMarkupContent(0, "<p class=\"host-loading\">Patience</p>")));
        Assert.Single(loading.FindAll(".host-loading"));
        Assert.Empty(loading.FindAll(".omni-detail-shell__loading"));

        var missing = RenderShell(OmniDetailState.NotFound, parameters => parameters
            .Add(component => component.NotFoundTitle, "Projet introuvable")
            .Add(component => component.NotFoundContent, builder => builder.AddMarkupContent(0, "<p class=\"host-missing\">Absent</p>")));
        Assert.Single(missing.FindAll(".host-missing"));
        Assert.Empty(missing.FindAll(".omni-empty-state"));
    }

    [Fact]
    public void StatTile_WithoutOnClick_IsAPlainBlockWithValueLabelAndDetail()
    {
        var tile = Render<OmniStatTile>(parameters => parameters
            .Add(component => component.Value, "12,4 k")
            .Add(component => component.Label, "Tokens aujourd'hui")
            .Add(component => component.Detail, "Réinitialisation à 18:00")
            .Add(component => component.Icon, (RenderFragment)(icon =>
            {
                icon.OpenComponent<OmniIcon>(0);
                icon.AddComponentParameter(1, nameof(OmniIcon.Name), OmniIconName.Database);
                icon.CloseComponent();
            })));

        var root = tile.Find(".omni-stat-tile");
        Assert.Equal("DIV", root.TagName);
        Assert.Empty(tile.FindAll("button"));
        Assert.Equal("true", tile.Find(".omni-stat-tile__icon").GetAttribute("aria-hidden"));
        Assert.Equal("12,4 k", tile.Find(".omni-stat-tile__value").TextContent);
        Assert.Equal("Tokens aujourd'hui", tile.Find(".omni-stat-tile__label").TextContent);
        Assert.Equal("Réinitialisation à 18:00", tile.Find(".omni-stat-tile__detail").TextContent);
        Assert.DoesNotContain("style=", tile.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StatTile_WithOnClick_IsOneNamedButton_AndOmitsAMissingDetail()
    {
        var clicks = 0;
        var tile = Render<OmniStatTile>(parameters => parameters
            .Add(component => component.Value, "83 %")
            .Add(component => component.Label, "Hebdomadaire")
            .Add(component => component.OnClick, () => clicks++));

        var button = tile.Find("button.omni-stat-tile.omni-stat-tile--action");
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal("Hebdomadaire : 83 %", button.GetAttribute("aria-label"));
        Assert.Empty(tile.FindAll(".omni-stat-tile__detail"));
        Assert.Empty(tile.FindAll(".omni-stat-tile__icon"));

        button.Click();
        Assert.Equal(1, clicks);

        var named = Render<OmniStatTile>(parameters => parameters
            .Add(component => component.Label, "Tokens")
            .Add(component => component.AriaLabel, "Ouvrir le détail des tokens")
            .Add(component => component.OnClick, () => { }));
        Assert.Equal("Ouvrir le détail des tokens", named.Find("button").GetAttribute("aria-label"));
    }

    [Fact]
    public void EmptyState_DefaultsToADecorativeTrayAndAParagraphTitle()
    {
        var empty = Render<OmniEmptyState>(parameters => parameters
            .Add(component => component.Title, "Aucun projet")
            .Add(component => component.Description, "Créez le premier.")
            .Add(component => component.Actions, builder => builder.AddMarkupContent(0, "<button type=\"button\">Créer</button>")));

        Assert.Equal("true", empty.Find(".omni-empty-state__icon").GetAttribute("aria-hidden"));
        Assert.Single(empty.FindAll(".omni-empty-state__icon svg.omni-icon"));
        Assert.Equal("P", empty.Find(".omni-empty-state__title").TagName);
        Assert.Equal("Aucun projet", empty.Find(".omni-empty-state__title").TextContent);
        Assert.Equal("Créez le premier.", empty.Find(".omni-empty-state__description").TextContent);
        Assert.Equal("Créer", empty.Find(".omni-empty-state__actions button").TextContent);
    }

    [Fact]
    public void EmptyState_LevelMakesTheTitleAHeading_AndNothingIsWrittenForMissingText()
    {
        var heading = Render<OmniEmptyState>(parameters => parameters
            .Add(component => component.Title, "Aucun résultat")
            .Add(component => component.Level, OmniHeadingLevel.H3)
            .Add(component => component.Icon, builder => builder.AddMarkupContent(0, "<i class=\"host-icon\"></i>")));
        Assert.Equal("H3", heading.Find(".omni-empty-state__title").TagName);
        Assert.Single(heading.FindAll(".host-icon"));
        Assert.Empty(heading.FindAll("svg"));

        var bare = Render<OmniEmptyState>();
        Assert.Empty(bare.FindAll(".omni-empty-state__title"));
        Assert.Empty(bare.FindAll(".omni-empty-state__description"));
        Assert.Empty(bare.FindAll(".omni-empty-state__actions"));
    }

    [Theory]
    [InlineData("fr-FR", "Connexion")]
    [InlineData("en-US", "Sign in")]
    public void LoginShell_NamesItsCardByTheTitleAndPlacesLogoFormAndFooter(string cultureName, string defaultTitle)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            var shell = Render<OmniLoginShell>(parameters => parameters
                .Add(component => component.Description, "Espace d'administration")
                .Add(component => component.Logo, builder => builder.AddMarkupContent(0, "<img src=\"logo.svg\" alt=\"Logo\" />"))
                .Add(component => component.ChildContent, builder => builder.AddMarkupContent(0, "<form class=\"host-form\"></form>"))
                .Add(component => component.Footer, builder => builder.AddContent(0, "Version 2.4")));

            var title = shell.Find("h1.omni-login-shell__title");
            Assert.Equal(defaultTitle, title.TextContent);
            Assert.Equal(title.Id, shell.Find(".omni-login-shell__card").GetAttribute("aria-labelledby"));
            Assert.Single(shell.FindAll(".omni-login-shell__logo img"));
            Assert.Equal("Espace d'administration", shell.Find(".omni-login-shell__description").TextContent);
            Assert.Single(shell.FindAll(".omni-card__body .host-form"));
            Assert.Equal("Version 2.4", shell.Find(".omni-card__footer").TextContent);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void LoginShell_WithoutLogoOrFooter_RendersNeither()
    {
        var shell = Render<OmniLoginShell>(parameters => parameters
            .Add(component => component.Title, "Identification")
            .Add(component => component.Level, OmniHeadingLevel.H2));

        Assert.Equal("Identification", shell.Find("h2").TextContent);
        Assert.Empty(shell.FindAll(".omni-login-shell__logo"));
        Assert.Empty(shell.FindAll(".omni-card__footer"));
    }

    [Theory]
    [InlineData("/projects/12", true)]
    [InlineData("/go?to=https://example.org", true)]
    [InlineData("/", true)]
    [InlineData("//evil.example", false)]
    [InlineData("/\\evil.example", false)]
    [InlineData("https://evil.example", false)]
    [InlineData("/https://evil.example", false)]
    [InlineData("projects/12", false)]
    [InlineData("/a\tb", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ReturnUrl_AcceptsOnlyPathsOfTheApplication(string? url, bool local)
    {
        Assert.Equal(local, OmniReturnUrl.IsLocal(url));
    }

    [Fact]
    public void ReturnUrl_ResolvesAndAppendsOnlyALocalAddress()
    {
        Assert.False(OmniReturnUrl.IsLocal("/" + new string('a', 2048)));
        Assert.Equal("/projects/12?tab=logs", OmniReturnUrl.Resolve("/projects/12?tab=logs"));
        Assert.Equal("/", OmniReturnUrl.Resolve("//evil.example"));
        Assert.Equal("/home", OmniReturnUrl.Resolve(null, "/home"));

        Assert.Equal("/login?returnUrl=%2Fprojects%2F12%3Ftab%3Dlogs", OmniReturnUrl.Append("/login", "/projects/12?tab=logs"));
        Assert.Equal("/login?x=1&next=%2Fa", OmniReturnUrl.Append("/login?x=1", "/a", "next"));
        Assert.Equal("/login", OmniReturnUrl.Append("/login", "https://evil.example"));
    }

    [Fact]
    public void DescriptionList_PairsEachValueWithItsLabelInTheColumnsAsked()
    {
        var list = Render<OmniDescriptionList>(parameters => parameters
            .Add(component => component.Columns, 2)
            .AddChildContent<OmniDescriptionItem>(item => item
                .Add(component => component.Label, "Adresse IP")
                .Add(component => component.Actions, builder => builder.AddMarkupContent(0, "<button type=\"button\">Copier</button>"))
                .AddChildContent("10.0.4.21")));

        var root = list.Find("dl");
        Assert.Contains("omni-description-list--columns-2", root.ClassName, StringComparison.Ordinal);
        var item = list.Find("dl > div.omni-description-list__item");
        Assert.Equal("Adresse IP", item.QuerySelector("dt")!.TextContent);
        Assert.Equal("10.0.4.21", item.QuerySelector("dd .omni-description-list__content")!.TextContent);
        Assert.Equal("Copier", item.QuerySelector("dd .omni-description-list__actions button")!.TextContent);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(3, 3)]
    [InlineData(9, 4)]
    public void DescriptionList_KeepsTheColumnCountBetweenOneAndFour(int asked, int rendered)
    {
        var list = Render<OmniDescriptionList>(parameters => parameters
            .Add(component => component.Columns, asked)
            .AddChildContent<OmniDescriptionItem>(item => item.Add(component => component.Label, "Nom")));

        Assert.Contains($"omni-description-list--columns-{rendered}", list.Find("dl").ClassName, StringComparison.Ordinal);
        Assert.Empty(list.FindAll(".omni-description-list__actions"));
    }

    private IRenderedComponent<OmniDetailShell> RenderShell(
        OmniDetailState state,
        Action<ComponentParameterCollectionBuilder<OmniDetailShell>>? configure = null) =>
        Render<OmniDetailShell>(parameters =>
        {
            parameters
                .Add(component => component.State, state)
                .Add(component => component.Header, builder => builder.AddMarkupContent(0, "<div class=\"host-header\">En-tête</div>"))
                .Add(component => component.ChildContent, builder => builder.AddContent(0, "Contenu"));
            configure?.Invoke(parameters);
        });
}
