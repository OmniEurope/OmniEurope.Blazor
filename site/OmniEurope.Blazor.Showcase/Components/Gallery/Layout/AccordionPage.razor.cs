using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Layout;

/// <summary>The Accordion page: a list of quality gates driven by its settings, then a compact accordion with free headers.</summary>
public partial class AccordionPage
{
    [Inject]
    private IStringLocalizer<LayoutStrings> Text { get; set; } = default!;

    private bool Multiple { get; set; }

    private bool KeepContent { get; set; }

    private OmniHeadingLevel Level { get; set; } = OmniHeadingLevel.H3;

    private bool ShowHelp { get; set; } = true;

    private bool FirstOpen { get; set; } = true;

    private static IReadOnlyList<OmniOption<OmniHeadingLevel>> LevelOptions { get; } =
    [
        new(OmniHeadingLevel.H2, "H2"),
        new(OmniHeadingLevel.H3, "H3"),
        new(OmniHeadingLevel.H4, "H4")
    ];
}
