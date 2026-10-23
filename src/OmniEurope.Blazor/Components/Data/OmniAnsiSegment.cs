namespace OmniEurope.Blazor.Components;

/// <summary>A run of text that carries one set of ANSI graphic attributes, as read by <see cref="OmniAnsi.Parse"/>.</summary>
/// <param name="Text">The text of the run, escape sequences removed, every other character kept as written. Never empty in a parsed list.</param>
/// <param name="Bold">Whether the run is bold (SGR 1).</param>
/// <param name="Foreground">The text colour as an index into the sixteen ANSI colours (0 to 7 normal, 8 to 15 bright), or null for the default text colour. A 256-colour or true-colour request is reduced to the nearest of the sixteen.</param>
/// <param name="Background">The background colour as an index into the sixteen ANSI colours, or null for none.</param>
/// <param name="Dim">Whether the run is faint (SGR 2).</param>
/// <param name="Italic">Whether the run is italic (SGR 3).</param>
/// <param name="Underline">Whether the run is underlined (SGR 4).</param>
public sealed record OmniAnsiSegment(
    string Text,
    bool Bold = false,
    int? Foreground = null,
    int? Background = null,
    bool Dim = false,
    bool Italic = false,
    bool Underline = false);
