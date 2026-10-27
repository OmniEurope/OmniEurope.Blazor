namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>A compact accordion with free headers, an own control in a row and the open state of each row bound.</summary>
public partial class AccordionAdvancedExample
{
    private bool RunOpen { get; set; }

    private bool LogOpen { get; set; }

    private int DetailsClicks { get; set; }

    private int OpenRows => (RunOpen ? 1 : 0) + (LogOpen ? 1 : 0);

    private void CountDetails() => DetailsClicks++;
}
