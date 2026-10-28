namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>A time picker that offers a minute every five.</summary>
public partial class DatesTimeExample
{
    private TimeOnly? Time { get; set; } = new(9, 30);
}
