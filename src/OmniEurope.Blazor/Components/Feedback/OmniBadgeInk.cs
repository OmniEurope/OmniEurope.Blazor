namespace OmniEurope.Blazor.Components;

/// <summary>
/// The lettering of a solid badge (<see cref="OmniFill.Solid"/>). Each ink has its own fill for every
/// tone, derived from the palette so that the ink reads on it at 4.5:1 in every theme and mode. The tonal
/// and outline fills keep the ink of their tone whatever the value.
/// </summary>
public enum OmniBadgeInk
{
    /// <summary>White letters on a fill deepened for them. The default.</summary>
    White,

    /// <summary>
    /// White letters drawn with a 1 px black outline (eight shadows, one per direction), for a badge laid
    /// over a picture or a busy colour.
    /// </summary>
    WhiteOutlined,

    /// <summary>Black letters, without outline, on a fill lightened for them.</summary>
    Black,
}
