namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>A date picker inside a draggable dialog.</summary>
public partial class DatesDialogExample
{
    private bool Open { get; set; }

    private DateOnly? Date { get; set; } = new(2026, 3, 2);
}
