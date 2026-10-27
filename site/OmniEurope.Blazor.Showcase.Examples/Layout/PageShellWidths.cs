using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>What the page shell examples share: the sample paragraphs and the names of the content widths.</summary>
public static class PageShellWidths
{
    /// <summary>Sample text, long enough to make the frame of the miniature scroll.</summary>
    public static IReadOnlyList<string> Paragraphs { get; } =
    [
        "This area is the main element of the page: assistive technologies list it among the landmarks, and a skip link lands here.",
        "The header above belongs to the layout, not to the main area: it holds what every page of the application shares.",
        "The body sits between the two: in a real application, it also receives the side menu, next to the main area.",
        "A long enough text makes the frame scroll, which is the only way to judge a sticky header.",
        "The main area is the landmark a skip link targets, so it takes the focus when the link is followed.",
        "Naming the area by its heading lets a screen reader announce the page title when it enters the landmark.",
        "With its own scrolling, the area scrolls from edge to edge and its scrollbar stays at the right edge of the frame.",
        "End of the sample content."
    ];

    /// <summary>The widths a main area can take.</summary>
    public static IReadOnlyList<OmniLayoutWidth> All { get; } = Enum.GetValues<OmniLayoutWidth>();

    /// <summary>The name of a content width.</summary>
    /// <param name="width">The content width.</param>
    /// <returns>The English name of the width.</returns>
    public static string Label(OmniLayoutWidth width) => width switch
    {
        OmniLayoutWidth.Wide => "Wide",
        OmniLayoutWidth.Content => "Content",
        _ => "Full width"
    };

    /// <summary>The cap a content width puts on the column.</summary>
    /// <param name="width">The content width.</param>
    /// <returns>The cap, written as a length.</returns>
    public static string Cap(OmniLayoutWidth width) => width switch
    {
        OmniLayoutWidth.Wide => "90rem",
        OmniLayoutWidth.Content => "72rem",
        _ => "100 %"
    };
}
