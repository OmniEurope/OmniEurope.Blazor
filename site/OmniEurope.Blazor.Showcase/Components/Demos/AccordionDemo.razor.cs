using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class AccordionDemo
{
    [Inject] private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private bool RunOpen { get; set; }

    private bool LogOpen { get; set; }

    private int DetailsClicks { get; set; }

    private int OpenRows => (RunOpen ? 1 : 0) + (LogOpen ? 1 : 0);

    private void CountDetails() => DetailsClicks++;
}
