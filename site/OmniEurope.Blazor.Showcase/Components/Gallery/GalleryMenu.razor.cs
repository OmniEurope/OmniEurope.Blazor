using OmniEurope.Blazor.Showcase.Gallery;
using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Gallery;

/// <summary>The left menu of the gallery, generated from <see cref="GalleryCatalog"/>.</summary>
public partial class GalleryMenu
{
    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    [Inject]
    private IStringLocalizerFactory Localizers { get; set; } = default!;

    private IStringLocalizer StringsOf(GalleryFamily family) => Localizers.Create(family.Strings);
}
