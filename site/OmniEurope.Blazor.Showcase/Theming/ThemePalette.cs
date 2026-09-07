namespace OmniEurope.Blazor.Showcase.Theming;

/// <summary>
/// The handful of colours an upstream palette actually defines, before this project derives a full
/// token set from them.
/// </summary>
/// <param name="Name">The palette name.</param>
/// <param name="Description">The one-line description published with the palette.</param>
/// <param name="Primary">The accent colour.</param>
/// <param name="Success">The success severity.</param>
/// <param name="Info">The informational severity.</param>
/// <param name="Warning">The warning severity.</param>
/// <param name="Danger">The danger severity.</param>
/// <param name="Background">The page background the palette ships.</param>
/// <param name="Foreground">The body text colour the palette ships.</param>
public sealed record ThemePalette(
    string Name,
    string Description,
    string Primary,
    string Success,
    string Info,
    string Warning,
    string Danger,
    string Background,
    string Foreground)
{
    /// <summary>
    /// The mode the upstream palette was authored in, decided from the background it ships rather
    /// than from a label, so the classification cannot disagree with the colours.
    /// </summary>
    public ThemeMode UpstreamMode => ThemeColor.IsDark(Background) ? ThemeMode.Dark : ThemeMode.Light;
}
