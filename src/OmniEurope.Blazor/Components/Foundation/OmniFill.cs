namespace OmniEurope.Blazor.Components;

/// <summary>How a coloured element such as <see cref="OmniAlert"/> or <see cref="OmniBadge"/> fills its box.</summary>
public enum OmniFill
{
    /// <summary>A light tint of the colour behind text of the strong shade. The default.</summary>
    Tonal,

    /// <summary>A border of the colour on the surface, no fill.</summary>
    Outline,

    /// <summary>The full colour behind text of the contrasting colour.</summary>
    Solid
}
