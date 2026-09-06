using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Pages;

public partial class Home
{
    private static readonly (string TitleKey, string BodyKey)[] Pillars =
    [
        ("PillarCspTitle", "PillarCspBody"),
        ("PillarThemeTitle", "PillarThemeBody"),
        ("PillarA11yTitle", "PillarA11yBody"),
        ("PillarWeightTitle", "PillarWeightBody")
    ];

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;
}
