using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class AlertsDemo
{
    [Inject] private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;
}
