namespace OmniEurope.Blazor.Showcase.Theming;

/// <summary>
/// The modes the visitor can preview: the two halves every palette provides, or the one the
/// system setting picks.
/// </summary>
public enum ThemeMode
{
    /// <summary>Light surfaces carrying dark text.</summary>
    Light,

    /// <summary>Dark surfaces carrying light text.</summary>
    Dark,

    /// <summary>The light or the dark half, whichever the system setting asks for, as it changes.</summary>
    System
}
