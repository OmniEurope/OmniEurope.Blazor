using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Navigation;

/// <summary>A long article and the back-to-top button that follows the scroll of the page.</summary>
public partial class BackToTopExample
{
    private const string Body = "A long page needs a quick way back up. The content scrolls inside the main area of the layout, so the "
        + "button follows that area, not the window; it stays out of sight, and out of the Tab order, until the reader is far enough down.";

    private static readonly IReadOnlyList<string> Chapters =
    [
        "1. Arrival", "2. Rooms", "3. Meals", "4. Activities", "5. Excursions",
        "6. Transport", "7. Payment", "8. Cancellation", "9. Contact", "10. Departure"
    ];

    /// <summary>How far, in pixels, the page scrolls before the button appears.</summary>
    [Parameter]
    public int Threshold { get; set; } = 400;

    /// <summary>The name of the button; null keeps the localized "Back to top".</summary>
    [Parameter]
    public string? Label { get; set; }
}
