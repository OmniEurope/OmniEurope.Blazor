namespace OmniEurope.Blazor.Showcase.Gallery;

/// <summary>
/// One family of components, a group of the gallery menu.
/// </summary>
/// <param name="TitleKey">Resource key of the family name, in <c>ShowcaseStrings</c>.</param>
/// <param name="Icon">The icon of the group in the menu.</param>
/// <param name="Strings">The marker type of the family's own resources
/// (<c>Resources/Gallery/&lt;Family&gt;Strings.resx</c>), which hold the titles, descriptions and
/// settings of its pages.</param>
/// <param name="Pages">The pages of the family, in menu order.</param>
public sealed record GalleryFamily(
    string TitleKey,
    OmniIconName Icon,
    Type Strings,
    IReadOnlyList<GalleryPage> Pages);
