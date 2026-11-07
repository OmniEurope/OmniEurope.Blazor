using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniSiteSearch"/> finds a page among the destinations of the host, grouped, ignoring case and
/// accents and looking at the keywords too; picking one navigates and empties the field.
/// </summary>
public sealed class SiteSearchTests : OmniBunitContext
{
    private static readonly IReadOnlyList<OmniSiteSearchDestination> Pages =
    [
        new("Présentation", "/", "Site"),
        new("Boutons", "/components/buttons", "Actions", ["click", "bouton"]),
        new("Menu de l'application", "/components/app-menu", "Navigation", ["header"]),
        new("Fil d'Ariane", "/components/breadcrumb", "Navigation", ["chemin"]),
        new("Éditeur", "/components/editor", "Édition", ["wysiwyg"])
    ];

    private IRenderedComponent<OmniSiteSearch> RenderSearch(IReadOnlyList<OmniSiteSearchDestination>? destinations = null) =>
        Render<OmniSiteSearch>(parameters => parameters.Add(component => component.Destinations, destinations ?? Pages));

    private static async Task TypeAsync(IRenderedComponent<OmniSiteSearch> search, string text) =>
        await search.Find("input").InputAsync(new ChangeEventArgs { Value = text });

    private static string[] Texts(IRenderedComponent<OmniSiteSearch> search) =>
        [.. search.FindAll("[role=option]").Select(option => option.TextContent.Trim())];

    private BunitNavigationManager Navigation => (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();

    [Fact]
    public async Task Typing_ListsTheMatches_IgnoringCaseAndAccents()
    {
        var search = RenderSearch();

        await TypeAsync(search, "EDITEUR");
        Assert.Equal(["Éditeur"], Texts(search));

        await TypeAsync(search, "presentation");
        Assert.Equal(["Présentation"], Texts(search));

        await TypeAsync(search, "l'app");
        Assert.Equal(["Menu de l'application"], Texts(search));
    }

    [Fact]
    public async Task Keywords_FindThePage_WithoutBeingShown()
    {
        var search = RenderSearch();

        await TypeAsync(search, "WYSIWYG");
        Assert.Equal(["Éditeur"], Texts(search));
        Assert.DoesNotContain("wysiwyg", search.Find("[role=listbox]").TextContent, StringComparison.OrdinalIgnoreCase);

        await TypeAsync(search, "chemin");
        Assert.Equal(["Fil d'Ariane"], Texts(search));
    }

    [Fact]
    public async Task EveryWordTyped_MustBeFound_InTheTextOrAKeyword()
    {
        var search = RenderSearch();

        await TypeAsync(search, "menu header");
        Assert.Equal(["Menu de l'application"], Texts(search));

        await TypeAsync(search, "menu click");
        Assert.Empty(search.FindAll("[role=option]"));
    }

    [Fact]
    public async Task APageFoundByItsText_ComesBeforeOneFoundOnlyByAKeyword_InTheSameGroup()
    {
        var search = RenderSearch(
        [
            new("Alpha", "/a", "G", ["menu"]),
            new("Menu", "/m", "G")
        ]);

        await TypeAsync(search, "menu");

        Assert.Equal(["Menu", "Alpha"], Texts(search));
    }

    [Fact]
    public async Task TheMatches_AreGrouped_ByDestinationGroup_InTheOrderOfTheirFirstPage()
    {
        var search = RenderSearch();

        await TypeAsync(search, "i");

        Assert.Equal(
            ["Site", "Actions", "Navigation", "Édition"],
            search.FindAll(".omni-autocomplete__group-heading").Select(heading => heading.TextContent.Trim()));
        var navigation = search.FindAll("[role=group]").Single(group => group.TextContent.Contains("Fil d'Ariane", StringComparison.Ordinal));
        Assert.Equal(2, navigation.QuerySelectorAll("[role=option]").Length);
    }

    [Fact]
    public async Task APageWithoutGroup_IsListedWithoutHeading()
    {
        var search = RenderSearch([new("Accueil", "/")]);

        await TypeAsync(search, "acc");

        Assert.Equal(["Accueil"], Texts(search));
        Assert.Empty(search.FindAll(".omni-autocomplete__group-heading"));
    }

    [Fact]
    public async Task Clicking_AMatch_Navigates_AndEmptiesTheField()
    {
        var search = RenderSearch();
        await TypeAsync(search, "bout");

        await search.Find("[role=option]").ClickAsync(new MouseEventArgs());

        Assert.EndsWith("/components/buttons", Assert.Single(Navigation.History).Uri, StringComparison.Ordinal);
        Assert.Equal(string.Empty, search.Find("input").GetAttribute("value") ?? string.Empty);
        Assert.Empty(search.FindAll("[role=listbox]"));
    }

    [Fact]
    public async Task Enter_OnTheHighlightedMatch_Navigates_AndEmptiesTheField()
    {
        var search = RenderSearch();
        await TypeAsync(search, "ariane");
        await search.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        await search.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.EndsWith("/components/breadcrumb", Assert.Single(Navigation.History).Uri, StringComparison.Ordinal);
        Assert.Equal(string.Empty, search.Find("input").GetAttribute("value") ?? string.Empty);
        Assert.Empty(search.FindAll("[role=listbox]"));
    }

    [Fact]
    public async Task Escape_ClosesTheList_WithoutNavigating()
    {
        var search = RenderSearch();
        await TypeAsync(search, "bout");
        await search.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Equal("true", search.Find("input").GetAttribute("aria-expanded"));

        await search.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal("false", search.Find("input").GetAttribute("aria-expanded"));
        Assert.Empty(search.FindAll("[role=listbox]"));
        Assert.Empty(Navigation.History);
    }

    [Fact]
    public async Task WithoutAMatch_ALocalizedLineSaysSo_AndGoesWhenTheFieldIsEmptied()
    {
        var search = RenderSearch();
        Assert.Empty(search.FindAll(".omni-site-search__empty"));

        await TypeAsync(search, "zzzz");
        Assert.Equal("Aucun résultat", search.Find(".omni-site-search__empty").TextContent.Trim());
        Assert.Empty(search.FindAll("[role=option]"));

        await TypeAsync(search, "bout");
        Assert.Empty(search.FindAll(".omni-site-search__empty"));

        await TypeAsync(search, "zzzz");
        await TypeAsync(search, string.Empty);
        Assert.Empty(search.FindAll(".omni-site-search__empty"));
    }

    [Fact]
    public async Task WithoutAnOpenList_EscapeReachesTheSearch_AndTakesTheNoMatchLineAway_WhileOtherKeysLeaveIt()
    {
        var search = RenderSearch();
        await TypeAsync(search, "zzzz");
        Assert.Empty(search.FindAll("[role=listbox]"));
        Assert.Equal("Aucun résultat", search.Find(".omni-site-search__empty").TextContent.Trim());

        await search.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Tab" });
        Assert.Equal("Aucun résultat", search.Find(".omni-site-search__empty").TextContent.Trim());

        await search.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(search.FindAll(".omni-site-search__empty"));
        Assert.Empty(Navigation.History);
    }

    [Fact]
    public void TheNameAndTheHint_AreLocalized_AndCanBeOverridden()
    {
        var byDefault = RenderSearch();
        var input = byDefault.Find("input");
        Assert.Equal("Rechercher dans le site", input.GetAttribute("aria-label"));
        Assert.Equal("Rechercher…", input.GetAttribute("placeholder"));
        Assert.Equal("search", byDefault.Find("div.omni-site-search").GetAttribute("role"));

        var overridden = Render<OmniSiteSearch>(parameters => parameters
            .Add(component => component.Destinations, Pages)
            .Add(component => component.Label, "Find a page")
            .Add(component => component.Placeholder, "Page…"));
        Assert.Equal("Find a page", overridden.Find("input").GetAttribute("aria-label"));
        Assert.Equal("Page…", overridden.Find("input").GetAttribute("placeholder"));
    }

    [Fact]
    public void AnUnsafeAddress_IsRefused()
    {
        Assert.ThrowsAny<Exception>(() => RenderSearch([new("Bad", "javascript:alert(1)")]));
    }

    [Fact]
    public async Task MissingDestinationsGiveNoMatchesAndClearingTheQueryHidesThatMessage()
    {
        var search = Render<OmniSiteSearch>(parameters => parameters.Add(component => component.Destinations, null));
        await TypeAsync(search, "audit");
        Assert.Empty(Texts(search));
        Assert.NotEmpty(search.Find(".omni-site-search__empty").TextContent);

        await search.Find("div[role=search]").TriggerEventAsync("oninput", new ChangeEventArgs { Value = null });

        Assert.Empty(search.FindAll(".omni-site-search__empty"));
        Assert.Empty(Navigation.History);
    }

    [Fact]
    public async Task AnEmptySearchRequestClearsTheNoMatchStateWithoutListingDestinations()
    {
        var search = RenderSearch();
        await TypeAsync(search, "zzzz");
        Assert.NotEmpty(search.Find(".omni-site-search__empty").TextContent);
        var autocomplete = search.FindComponent<OmniAutocomplete<OmniSiteSearchDestination>>();

        var options = await search.InvokeAsync(() => autocomplete.Instance.Search!(" \t ", CancellationToken.None));

        Assert.Empty(options);
        Assert.Empty(search.FindAll(".omni-site-search__empty"));
        Assert.Empty(Navigation.History);
    }

    [Fact]
    public async Task ClearingTheAutocompleteSelectionDoesNotNavigateAndRemovesTheNoMatchMessage()
    {
        var search = RenderSearch();
        await TypeAsync(search, "zzzz");
        var autocomplete = search.FindComponent<OmniAutocomplete<OmniSiteSearchDestination>>();

        await search.InvokeAsync(() => autocomplete.Instance.ValueChanged.InvokeAsync(null));

        Assert.Empty(search.FindAll(".omni-site-search__empty"));
        Assert.Empty(Navigation.History);
    }
}
