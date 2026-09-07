using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase;

public partial class App
{
    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;
}
