using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class LayoutDemo
{
    [Inject] private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private bool TileChoice { get; set; } = true;

    private int CardClicks { get; set; }
}
