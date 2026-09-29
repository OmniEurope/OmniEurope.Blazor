namespace OmniEurope.Blazor.Components;

/// <summary>
/// The colour of a text drawn by <see cref="OmniText"/> or <see cref="OmniHeading"/>. It keeps
/// <see cref="Muted"/>, which only a text has, and otherwise speaks the words of <see cref="OmniTone"/>.
/// </summary>
public enum OmniTextTone
{
    /// <summary>The page text colour. The default.</summary>
    Neutral,

    /// <summary>The secondary text colour, for what matters less.</summary>
    Muted,

    /// <summary>The accent colour of the palette.</summary>
    Accent,

    /// <summary>A favourable state.</summary>
    Success,

    /// <summary>An error or a failure.</summary>
    Danger
}
