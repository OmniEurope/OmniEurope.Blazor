namespace OmniEurope.Blazor.Showcase.Theming;

/// <summary>
/// The families the theme editor splits its tokens into.
/// </summary>
public enum ThemeTokenGroup
{
    /// <summary>Palette entries: surfaces, text, accents and severities.</summary>
    Color,

    /// <summary>Type scale and font family.</summary>
    Typography,

    /// <summary>Spacing steps and control height.</summary>
    Spacing,

    /// <summary>Corner radii and border widths.</summary>
    Shape,

    /// <summary>Elevations, focus rings and overlays.</summary>
    Elevation,

    /// <summary>Categorical series colours used by charts.</summary>
    Chart,

    /// <summary>Dark surfaces that only resolve inside the data grid.</summary>
    Grid
}
