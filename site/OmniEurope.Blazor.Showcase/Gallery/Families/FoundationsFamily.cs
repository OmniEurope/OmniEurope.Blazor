using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Foundations family of the gallery: its pages, in menu order.</summary>
internal static class FoundationsFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyFoundations", OmniIconName.Layers, typeof(FoundationsStrings),
    [
        new("theme", "ThemeTitle", "ThemeDescription", OmniIconName.Palette,
            ["OmniThemeScope", "OmniAppearanceSettings", "OmniAppearanceWindow", "OmniAppearanceState"],
            [
                "Foundations/ThemePresetsExample.razor", "Foundations/ThemePresetsExample.razor.cs",
                "Foundations/ThemePalettesExample.razor", "Foundations/ThemePalettesExample.razor.cs",
                "Foundations/ThemeCombinationsExample.razor", "Foundations/ThemeCombinationsExample.razor.cs",
                "Foundations/ThemeComponentsExample.razor", "Foundations/ThemeComponentsExample.razor.cs",
                "Foundations/ThemeScopeExample.razor", "Foundations/ThemeScopeExample.razor.cs",
                "Foundations/ThemeScopesExample.razor", "Foundations/ThemeScopesExample.razor.cs",
                "Foundations/ThemeFollowingExample.razor", "Foundations/ThemeFollowingExample.razor.cs",
                "Foundations/ThemeTile.razor", "Foundations/ThemeTile.razor.cs",
                "Foundations/ThemeSample.razor", "Foundations/ThemeSample.razor.cs"
            ]),
        new("icons", "IconsTitle", "IconsDescription", OmniIconName.Star,
            ["OmniIcon"],
            [
                "Foundations/IconPreviewExample.razor", "Foundations/IconPreviewExample.razor.cs",
                "Foundations/IconSizesExample.razor", "Foundations/IconSizesExample.razor.cs",
                "Foundations/IconGlyphExample.razor", "Foundations/IconGlyphExample.razor.cs",
                "Foundations/IconGalleryExample.razor", "Foundations/IconGalleryExample.razor.cs",
                "Foundations/IconCatalog.cs"
            ]),
        new("typography", "TypographyTitle", "TypographyDescription", OmniIconName.TextSize,
            ["OmniHeading", "OmniText", "OmniLabel", "OmniLink"],
            [
                "Foundations/TypographyStageExample.razor", "Foundations/TypographyStageExample.razor.cs",
                "Foundations/HeadingLevelsExample.razor", "Foundations/HeadingLevelsExample.razor.cs",
                "Foundations/TextElementsExample.razor", "Foundations/TextElementsExample.razor.cs",
                "Foundations/TextTonesExample.razor", "Foundations/TextTonesExample.razor.cs",
                "Foundations/TruncationExample.razor", "Foundations/TruncationExample.razor.cs",
                "Foundations/LabelsAndLinksExample.razor", "Foundations/LabelsAndLinksExample.razor.cs",
                "Foundations/TypographySpecimens.cs"
            ]),
        new("badges", "BadgesTitle", "BadgesDescription", OmniIconName.Tag,
            ["OmniBadge"],
            [
                "Foundations/BadgeExample.razor", "Foundations/BadgeExample.razor.cs",
                "Foundations/BadgeMatrixExample.razor", "Foundations/BadgeMatrixExample.razor.cs",
                "Foundations/BadgeInkExample.razor", "Foundations/BadgeInkExample.razor.cs",
                "Foundations/BadgeIconsExample.razor", "Foundations/BadgeIconsExample.razor.cs",
                "Foundations/BadgeWidthExample.razor", "Foundations/BadgeWidthExample.razor.cs",
                "Foundations/BadgeContextExample.razor", "Foundations/BadgeContextExample.razor.cs"
            ]),
        new("images", "ImagesTitle", "ImagesDescription", OmniIconName.Image,
            ["OmniImage"],
            [
                "Foundations/ImageExample.razor", "Foundations/ImageExample.razor.cs",
                "Foundations/ImageFitsExample.razor", "Foundations/ImageFitsExample.razor.cs",
                "Foundations/ImageLoadingsExample.razor", "Foundations/ImageLoadingsExample.razor.cs",
                "Foundations/ImageSourcesExample.razor", "Foundations/ImageSourcesExample.razor.cs",
                "Foundations/SampleImages.cs"
            ])
    ]);
}
