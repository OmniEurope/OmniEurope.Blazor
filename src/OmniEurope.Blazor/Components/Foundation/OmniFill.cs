namespace OmniEurope.Blazor.Components;

/// <summary>
/// How a coloured element such as <see cref="OmniAlert"/> or <see cref="OmniBadge"/> fills its box. The enum
/// has no default of its own: each component states its own (<see cref="OmniAlert"/> draws
/// <see cref="Outline"/>, <see cref="OmniBadge"/> <see cref="Solid"/>, <see cref="OmniStatus"/> and so
/// <see cref="OmniStatusBadge{TValue}"/> <see cref="Tonal"/>), each one chosen for what that component
/// shows.
/// </summary>
public enum OmniFill
{
    /// <summary>A light tint of the colour behind text of the strong shade.</summary>
    Tonal,

    /// <summary>A border of the colour on the surface, no fill.</summary>
    Outline,

    /// <summary>The full colour behind text of the contrasting colour.</summary>
    Solid
}
