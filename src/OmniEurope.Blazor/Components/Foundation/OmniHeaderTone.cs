namespace OmniEurope.Blazor.Components;

/// <summary>
/// What the application header is painted with.
/// </summary>
public enum OmniHeaderTone
{
    /// <summary>The surface colour, the header reading as part of the page.</summary>
    Surface,

    /// <summary>
    /// The theme's accent, with its contrasting text: the header reads as the application's band,
    /// and the ghost buttons, links and sidebar toggle inside it take the band's text colour.
    /// </summary>
    Accent
}
