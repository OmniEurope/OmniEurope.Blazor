using OmniEurope.Blazor.Showcase.Gallery.Families;

namespace OmniEurope.Blazor.Showcase.Gallery;

/// <summary>
/// The plan of the gallery: its families in menu order, each declared in its own file under
/// <c>Gallery/Families</c>. The menu is generated from it, and the guard tests use it to prove that
/// every page renders and that every source it lists is embedded.
/// </summary>
public static class GalleryCatalog
{
    /// <summary>Every family, in the order of the menu.</summary>
    public static IReadOnlyList<GalleryFamily> Families { get; } =
    [
        ShellFamily.Family,
        NavigationFamily.Family,
        FoundationsFamily.Family,
        LayoutFamily.Family,
        ActionsFamily.Family,
        FeedbackFamily.Family,
        OverlaysFamily.Family,
        FormsFamily.Family,
        SelectionFamily.Family,
        DataFamily.Family,
        ChartsFamily.Family,
        SchedulingFamily.Family,
        EditorFamily.Family,
        DiagramFamily.Family
    ];

    /// <summary>Every page, family after family.</summary>
    public static IEnumerable<GalleryPage> Pages => Families.SelectMany(family => family.Pages);

    /// <summary>The page with that slug, or <see langword="null"/> when the gallery has none.</summary>
    public static GalleryPage? Find(string? slug) =>
        Pages.FirstOrDefault(page => string.Equals(page.Slug, slug, StringComparison.Ordinal));

    /// <summary>The family a page belongs to.</summary>
    /// <exception cref="ArgumentException">The page is not in the catalogue.</exception>
    public static GalleryFamily FamilyOf(GalleryPage page) =>
        Families.FirstOrDefault(family => family.Pages.Contains(page))
        ?? throw new ArgumentException($"The gallery has no page '{page?.Slug}'.", nameof(page));

    /// <summary>The page with that slug, for a slug the caller knows to exist.</summary>
    /// <exception cref="ArgumentException">No page has <paramref name="slug"/>.</exception>
    public static GalleryPage Resolve(string slug) =>
        Find(slug) ?? throw new ArgumentException($"The gallery has no page '{slug}'.", nameof(slug));
}
