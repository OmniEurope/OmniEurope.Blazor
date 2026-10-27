namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>The texts and the picture the card examples share.</summary>
public static class CardSample
{
    /// <summary>The title of the sample card.</summary>
    public const string Title = "Renewal of the maintenance contract";

    /// <summary>The label of the badge next to the title.</summary>
    public const string Badge = "To sign";

    /// <summary>The first paragraph of the body.</summary>
    public const string Text = "The current contract ends on 30 June. The supplier has sent a new proposal with the same scope.";

    /// <summary>The second paragraph of a long body.</summary>
    public const string LongText = "The proposal keeps the response time at four hours on working days and adds an on-call service during the closing period.";

    /// <summary>The third paragraph of a long body.</summary>
    public const string LongTextEnd = "The price rises by three percent, in line with the indexation clause of the previous contract.";

    /// <summary>The text of a footer that carries no action.</summary>
    public const string FooterText = "Updated yesterday by the purchasing department";

    /// <summary>
    /// A landscape drawn in SVG as a data address, so that the example needs no file of its own
    /// (the content-security policy of a typical site allows data: images).
    /// </summary>
    public const string LandscapeImage = "data:image/svg+xml,"
        + "%3Csvg xmlns='http://www.w3.org/2000/svg' width='480' height='320' viewBox='0 0 480 320'%3E"
        + "%3Cdefs%3E%3ClinearGradient id='s' x1='0' y1='0' x2='0' y2='1'%3E"
        + "%3Cstop offset='0' stop-color='%231d2b64'/%3E%3Cstop offset='0.6' stop-color='%23f28f6b'/%3E%3Cstop offset='1' stop-color='%23fcd9a8'/%3E"
        + "%3C/linearGradient%3E%3C/defs%3E"
        + "%3Crect width='480' height='320' fill='url(%23s)'/%3E"
        + "%3Ccircle cx='340' cy='150' r='28' fill='%23ffe7b3'/%3E"
        + "%3Cpath d='M0 190 L70 110 L120 155 L190 75 L255 160 L305 130 L370 190 Z' fill='%233b3f73'/%3E"
        + "%3Cpath d='M210 190 L290 120 L345 165 L405 100 L480 170 L480 190 Z' fill='%232a2d5c'/%3E"
        + "%3Crect y='190' width='480' height='130' fill='%232c3e70'/%3E"
        + "%3Crect x='310' y='196' width='48' height='3' rx='1.5' fill='%23ffe7b3' opacity='0.7'/%3E"
        + "%3C/svg%3E";
}
