using System.Globalization;
using OmniEurope.Blazor.Showcase.Gallery;
using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Gallery;

/// <summary>The entry page of the gallery.</summary>
public partial class GalleryHome
{
    private const CompareOptions Loose = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    [Inject]
    private IStringLocalizerFactory Localizers { get; set; } = default!;

    /// <summary>The text the families and their pages are narrowed by, ignoring case and accents.</summary>
    private string? Filter { get; set; }

    private bool HasNoMatch => !string.IsNullOrWhiteSpace(Filter) && Visible.Count == 0;

    /// <summary>
    /// The families to list with the pages to list under each: all of them without a filter; otherwise a
    /// family whose title matches keeps all its pages, and any other keeps the pages whose title matches
    /// and drops out when none does.
    /// </summary>
    private IReadOnlyList<(GalleryFamily Family, IReadOnlyList<GalleryPage> Pages)> Visible =>
    [
        .. GalleryCatalog.Families
            .Where(family => family.Pages.Count > 0)
            .Select(family => (Family: family, Pages: PagesShown(family)))
            .Where(entry => entry.Pages.Count > 0)
    ];

    private IReadOnlyList<GalleryPage> PagesShown(GalleryFamily family)
    {
        var needle = Filter?.Trim();
        if (string.IsNullOrEmpty(needle) || Matches(Text[family.TitleKey], needle))
        {
            return family.Pages;
        }

        var strings = StringsOf(family);
        return [.. family.Pages.Where(page => Matches(strings[page.TitleKey], needle))];
    }

    private static bool Matches(string text, string needle) =>
        CultureInfo.CurrentCulture.CompareInfo.IndexOf(text, needle, Loose) >= 0;

    private static string FamilyId(GalleryFamily family) => "gallery-family-" + family.TitleKey;

    private IStringLocalizer StringsOf(GalleryFamily family) => Localizers.Create(family.Strings);
}
