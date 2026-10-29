using OmniEurope.Blazor.Showcase.Gallery;
using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Layout;

/// <summary>
/// The pages the search of the header leads to: the sections of the site, then every page of the gallery
/// under the family it belongs to, with the titles in the language shown, as the gallery menu draws them.
/// </summary>
public static class ShowcaseSearchDestinations
{
    public static IReadOnlyList<OmniSiteSearchDestination> Build(
        IStringLocalizer<ShowcaseStrings> text,
        IStringLocalizerFactory localizers)
    {
        var site = text["SearchGroupSite"].Value;
        var destinations = new List<OmniSiteSearchDestination>
        {
            new(text["NavShowcase"], string.Empty, site, Icon: OmniIconName.Home),
            new(text["NavDocumentation"], "documentation", site, Icon: OmniIconName.BookOpen),
            new(text["NavComponents"], GalleryPage.RoutePrefix, site, Icon: OmniIconName.Flask),
            new(text["NavCustomize"], "customizer", site, Icon: OmniIconName.Palette)
        };

        foreach (var family in GalleryCatalog.Families.Where(family => family.Pages.Count > 0))
        {
            var group = text[family.TitleKey].Value;
            var strings = localizers.Create(family.Strings);
            destinations.AddRange(family.Pages.Select(page =>
                new OmniSiteSearchDestination(strings[page.TitleKey], page.Href, group, [page.Slug], page.Icon)));
        }

        return destinations;
    }
}
