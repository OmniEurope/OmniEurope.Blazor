namespace OmniEurope.Blazor.Components;

/// <summary>
/// The corners of an <see cref="OmniBadge"/>, whatever its <see cref="OmniFill"/>: the colours, the border
/// and the height do not change, only the radius does (client application acceptance review R-3).
/// </summary>
public enum OmniBadgeShape
{
    /// <summary>Fully rounded ends, like a pill (<c>--omni-radius-pill</c>). The default.</summary>
    Pill,

    /// <summary>Slightly rounded corners, the small radius of the theme (<c>--omni-radius-sm</c>), as the badge was drawn before the pill.</summary>
    Rounded,

    /// <summary>Square corners, no radius.</summary>
    Square,
}
