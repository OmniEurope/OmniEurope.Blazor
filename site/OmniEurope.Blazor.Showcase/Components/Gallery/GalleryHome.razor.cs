using OmniEurope.Blazor.Showcase.Gallery;
using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Gallery;

/// <summary>The entry page of the gallery.</summary>
public partial class GalleryHome
{
    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    [Inject]
    private IStringLocalizerFactory Localizers { get; set; } = default!;

    private static string FamilyId(GalleryFamily family) => "gallery-family-" + family.TitleKey;

    private IStringLocalizer StringsOf(GalleryFamily family) => Localizers.Create(family.Strings);
}
