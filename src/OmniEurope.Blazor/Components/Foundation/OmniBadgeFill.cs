namespace OmniEurope.Blazor.Components;

/// <summary>
/// Whether a badge is painted or drawn.
/// </summary>
public enum OmniBadgeFill
{
    /// <summary>A block of colour carrying the label.</summary>
    Filled,

    /// <summary>An outline in the same colour, with the page showing through.</summary>
    Outline,

    /// <summary>
    /// The fill and the text of the button of the same intention, so a status and the action that
    /// concerns it carry the same colour. Appended, so the published values keep their numbers.
    /// </summary>
    Solid
}
