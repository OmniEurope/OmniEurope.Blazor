namespace OmniEurope.Blazor.Components;

/// <summary>How tightly controls, paddings and gaps are laid out in a theme scope, a section or a component.</summary>
public enum OmniDensity
{
    /// <summary>Tight: more rows on screen, for dense working views.</summary>
    Compact,

    /// <summary>The density the themes are drawn at. The default.</summary>
    Comfortable,

    /// <summary>Airy: more room around each control, for touch or reading.</summary>
    Spacious
}
