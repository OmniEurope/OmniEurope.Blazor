using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Showcase.Components.Gallery;
using OmniEurope.Blazor.Showcase.Gallery;
using OmniEurope.Blazor.Showcase.Resources;
using OmniEurope.Blazor.Showcase.Theming;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The search of the gallery overview (R1-28): typing narrows the family cards to the components whose
/// title contains the text, ignoring case and accents, and says so when none does. The expectations are
/// computed from the catalogue and the showcase resources, so adding a page never breaks them.
/// </summary>
public sealed class ShowcaseGalleryHomeFilterTests : OmniBunitContext
{
    private const string Search = "input[type=search]";

    public ShowcaseGalleryHomeFilterTests()
    {
        Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri("https://localhost/") });
        Services.AddScoped<ThemeTokenReader>();
        Services.AddScoped<ThemeState>();
    }

    [Fact]
    public void WithoutATextEveryFamilyWithPagesAndEveryPageIsListedAndNoMessageShows()
    {
        var home = Render<GalleryHome>();

        Assert.Equal(GalleryCatalog.Families.Count(family => family.Pages.Count > 0), home.FindAll(".gallery-home__family").Count);
        Assert.Equal(GalleryCatalog.Pages.Count(), home.FindAll("a.gallery-home__page").Count);
        Assert.Equal(string.Empty, home.Find("p.gallery-home__empty").TextContent);
        Assert.Equal("search", home.Find(Search).GetAttribute("type"));
        Assert.Equal(Text("GalleryFilterPlaceholder"), home.Find(Search).GetAttribute("aria-label"));
    }

    [Fact]
    public void ATitleNarrowsTheListToTheMatchingPagesAndDropsTheFamiliesWithoutAny()
    {
        var home = Render<GalleryHome>();
        var (family, page) = FirstPageWithUniqueTitle();
        var title = PageTitle(family, page);

        home.Find(Search).Input(title);

        var links = home.FindAll("a.gallery-home__page");
        Assert.Single(links);
        Assert.Equal(title, links[0].TextContent.Trim());
        Assert.Single(home.FindAll(".gallery-home__family"));
        Assert.Equal(string.Empty, home.Find("p.gallery-home__empty").TextContent);
    }

    [Fact]
    public void TheMatchIgnoresCase()
    {
        var home = Render<GalleryHome>();
        var (family, page) = FirstPageWithUniqueTitle();

        home.Find(Search).Input(PageTitle(family, page).ToUpperInvariant());

        Assert.Single(home.FindAll("a.gallery-home__page"));
    }

    [Fact]
    public void TheMatchIgnoresAccentsOfTheFamilyTitleItIsTyped() =>
        ShowcaseLanguageTests.WithUiCulture("fr-FR", () =>
    {
        var home = Render<GalleryHome>();
        var data = GalleryCatalog.Families.Single(family => family.TitleKey == "GalleryFamilyData");
        Assert.Equal("Données", Text("GalleryFamilyData"));

        home.Find(Search).Input("DONNEES");

        var card = Assert.Single(home.FindAll(".gallery-home__family"), element => element.QuerySelector("h2")!.TextContent.Trim() == "Données");
        Assert.Equal(data.Pages.Count, card.QuerySelectorAll("a.gallery-home__page").Length);
    });

    [Fact]
    public void AFamilyWhoseTitleMatchesKeepsAllItsPages()
    {
        var home = Render<GalleryHome>();
        var family = GalleryCatalog.Families.First(candidate => candidate.Pages.Count > 1);

        home.Find(Search).Input(Text(family.TitleKey));

        var card = home.FindAll(".gallery-home__family")
            .First(element => element.QuerySelector("h2")!.TextContent.Trim() == Text(family.TitleKey));
        Assert.Equal(family.Pages.Count, card.QuerySelectorAll("a.gallery-home__page").Length);
    }

    [Fact]
    public void WhenNothingMatchesNoCardIsShownAndALiveRegionSaysSo()
    {
        var home = Render<GalleryHome>();
        Assert.Equal("status", home.Find("p.gallery-home__empty").GetAttribute("role"));

        home.Find(Search).Input("zzzqqq");

        Assert.Empty(home.FindAll(".gallery-home__family"));
        Assert.Equal(Text("GalleryNoMatch"), home.Find("p.gallery-home__empty").TextContent);
    }

    [Fact]
    public void ClearingTheTextListsEverythingAgain()
    {
        var home = Render<GalleryHome>();
        home.Find(Search).Input("zzzqqq");

        home.Find(Search).Input(string.Empty);

        Assert.Equal(GalleryCatalog.Pages.Count(), home.FindAll("a.gallery-home__page").Count);
        Assert.Equal(string.Empty, home.Find("p.gallery-home__empty").TextContent);
    }

    private string Text(string key) => Services.GetRequiredService<IStringLocalizer<ShowcaseStrings>>()[key].Value;

    private string PageTitle(GalleryFamily family, GalleryPage page) =>
        Services.GetRequiredService<IStringLocalizerFactory>().Create(family.Strings)[page.TitleKey].Value;

    // A page whose title no other page title, and no family title, contains, so that typing it leaves
    // exactly one link.
    private (GalleryFamily Family, GalleryPage Page) FirstPageWithUniqueTitle()
    {
        var titles = GalleryCatalog.Families.SelectMany(family => family.Pages.Select(page => PageTitle(family, page))).ToArray();
        foreach (var family in GalleryCatalog.Families)
        {
            foreach (var page in family.Pages)
            {
                var title = PageTitle(family, page);
                var familyTitles = GalleryCatalog.Families.Select(candidate => Text(candidate.TitleKey));
                if (titles.Count(other => other.Contains(title, StringComparison.OrdinalIgnoreCase)) == 1
                    && !familyTitles.Any(other => other.Contains(title, StringComparison.OrdinalIgnoreCase)))
                {
                    return (family, page);
                }
            }
        }

        throw new InvalidOperationException("The catalogue has no page with a title of its own.");
    }
}
