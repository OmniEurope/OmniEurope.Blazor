using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class NavigationDemo
{
    [Inject] private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private string? Tab { get; set; } = "resume";

    private int Step { get; set; } = 1;
}
