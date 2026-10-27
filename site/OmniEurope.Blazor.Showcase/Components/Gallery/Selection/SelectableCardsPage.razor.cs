using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Selection;

/// <summary>The Selectable cards page: the settings of a transport card group, then single cards, cards drawn from options and card layouts.</summary>
public partial class SelectableCardsPage
{
    [Inject]
    private IStringLocalizer<SelectionStrings> Text { get; set; } = default!;

    private bool Multiple { get; set; }

    private bool Disabled { get; set; }

    private bool ShowDescriptions { get; set; } = true;
}
