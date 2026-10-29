using System.Net;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Showcase.Components.Layout;
using OmniEurope.Blazor.Showcase.Gallery;
using OmniEurope.Blazor.Showcase.Localization;
using OmniEurope.Blazor.Showcase.Theming;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The header of the showcase holds the site search between the brand and the application menu, and it
/// leads to the sections of the site and to every page of the gallery, under its family, in the language
/// shown. A bUnit render proves the markup, the data and the navigation, not the width: that is the
/// stylesheet's, measured in a real browser.
/// </summary>
public sealed class ShowcaseSearchTests : OmniBunitContext
{
    public ShowcaseSearchTests()
    {
        Services.AddScoped(_ => new HttpClient(new StylesheetHandler(":root { --omni-color-accent: #2563eb; }")) { BaseAddress = new Uri("https://localhost/") });
        Services.AddScoped<ThemeTokenReader>();
        Services.AddScoped<ThemeState>();
        Services.AddScoped<ShowcaseLanguageSwitcher>();
    }

    [Fact]
    public void TheHeader_HoldsTheSearch_AfterTheBrandAndBeforeTheAppMenu() =>
        ShowcaseLanguageTests.WithUiCulture("en-IE", () =>
    {
        var layout = RenderLayout();

        var children = layout.Find("header.omni-header").Children.Select(child => child.ClassName ?? string.Empty).ToArray();
        Assert.StartsWith("omni-sidebar-toggle", children[0], StringComparison.Ordinal);
        Assert.Contains("omni-header__brand", children[1], StringComparison.Ordinal);
        Assert.Contains("omni-site-search", children[2], StringComparison.Ordinal);
        Assert.Contains("omni-app-menu", children[^1], StringComparison.Ordinal);
        Assert.Equal("Search the site", layout.Find(".omni-site-search input").GetAttribute("aria-label"));
    });

    [Fact]
    public async Task TheSearch_FindsAGalleryPage_UnderItsFamily_AndLeadsToIt()
    {
        var page = GalleryCatalog.Pages.First();
        var family = GalleryCatalog.FamilyOf(page);
        var strings = Services.GetRequiredService<IStringLocalizerFactory>().Create(family.Strings);
        var title = strings[page.TitleKey].Value;
        var familyTitle = Services.GetRequiredService<IStringLocalizer<OmniEurope.Blazor.Showcase.Resources.ShowcaseStrings>>()[family.TitleKey].Value;

        var layout = RenderLayout();
        await layout.Find(".omni-site-search input").InputAsync(new ChangeEventArgs { Value = page.Slug });

        var option = layout.FindAll(".omni-site-search [role=option]").First(found => found.TextContent.Contains(title, StringComparison.Ordinal));
        Assert.Contains(familyTitle, layout.FindAll(".omni-site-search .omni-autocomplete__group-heading").Select(heading => heading.TextContent.Trim()));

        await option.ClickAsync(new MouseEventArgs());

        var navigation = (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith(page.Href, Assert.Single(navigation.History).Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void TheDestinations_AreTheFourSectionsOfTheSite_ThenEveryGalleryPage_ByFamily() =>
        ShowcaseLanguageTests.WithUiCulture("en-IE", () =>
    {
        var text = Services.GetRequiredService<IStringLocalizer<OmniEurope.Blazor.Showcase.Resources.ShowcaseStrings>>();
        var destinations = ShowcaseSearchDestinations.Build(text, Services.GetRequiredService<IStringLocalizerFactory>());

        Assert.Equal(["Overview", "Documentation", "Components", "Customisation"], destinations.Take(4).Select(destination => destination.Text));
        Assert.All(destinations.Take(4), destination => Assert.Equal("Site", destination.Group));

        var gallery = destinations.Skip(4).ToArray();
        Assert.Equal(GalleryCatalog.Pages.Select(page => page.Href), gallery.Select(destination => destination.Href));
        Assert.All(gallery, destination => Assert.False(string.IsNullOrWhiteSpace(destination.Group)));
        Assert.All(gallery, destination => Assert.False(string.IsNullOrWhiteSpace(destination.Text)));
        Assert.Equal(gallery.Length, gallery.Select(destination => destination.Href).Distinct().Count());
    });

    private IRenderedComponent<MainLayout> RenderLayout() =>
        Render<MainLayout>(parameters => parameters.Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<p id=\"page\">page</p>"))));

    private sealed class StylesheetHandler(string css) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(css) });
    }
}
