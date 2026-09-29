using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class TypographyDemo
{
    [Inject] private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;
}
