using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Turns a theme definition into the two modes the catalogue offers.
/// </summary>
/// <remarks>
/// Colours are moved until they clear the WCAG ratios: 4.5 for body text and for text laid over a
/// filled surface, 3.0 for the accent against its background. A definition can therefore come out a
/// little darker or lighter than written; legibility wins, and the tests hold that line. The shape of
/// the theme (radii, borders, shadows, fonts, buttons) is laid over both modes unchanged.
/// </remarks>
internal static class ThemePresetFactory
{
    private const double BodyTextRatio = 4.5;
    private const double OverlaidTextRatio = 4.5;
    private const double AccentRatio = 3.0;

    /// <summary>Builds the light and dark halves of one theme.</summary>
    public static OmniThemePreset Create(ThemeDefinition theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        var light = Build(theme, theme.Accent, theme.LightSurface, theme.LightText, OmniAppearance.Light);
        var dark = Build(theme, theme.DarkAccent ?? theme.Accent, theme.DarkSurface, theme.DarkText, OmniAppearance.Dark);
        return new OmniThemePreset(theme.Name, theme.Description, light, dark);
    }

    /// <summary>Every theme of the catalogue, both modes built.</summary>
    public static IReadOnlyList<OmniThemePreset> CreateAll(IEnumerable<ThemeDefinition> themes)
    {
        ArgumentNullException.ThrowIfNull(themes);
        return [.. themes.Select(Create)];
    }

    private static Dictionary<string, string> Build(ThemeDefinition theme, string accentColor, string surface, string authoredText, OmniAppearance mode)
    {
        var text = PushApart(authoredText, surface, BodyTextRatio);
        var (accent, onAccent) = Fill(accentColor, surface);
        var (success, onSuccess) = Fill(theme.Success, surface);
        var (danger, onDanger) = Fill(theme.Danger, surface);
        var (warning, _) = Fill(theme.Warning, surface);
        var info = Visible(theme.Info, surface);
        var dark = mode is OmniAppearance.Dark;

        var tokens = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["--omni-color-accent"] = accent,
            ["--omni-color-accent-strong"] = dark ? ThemeColor.Lighten(accent, 0.25) : ThemeColor.Darken(accent, 0.2),
            ["--omni-color-surface"] = surface,
            ["--omni-color-surface-muted"] = dark ? ThemeColor.Lighten(surface, 0.06) : ThemeColor.Darken(surface, 0.04),
            ["--omni-color-surface-hover"] = dark ? ThemeColor.Lighten(surface, 0.12) : ThemeColor.Darken(surface, 0.08),
            ["--omni-color-surface-highlight"] = dark ? ThemeColor.Lighten(surface, 0.16) : ThemeColor.Darken(surface, 0.1),
            ["--omni-color-border"] = ThemeColor.Mix(text, surface, 0.3),
            ["--omni-color-text"] = text,
            ["--omni-color-text-muted"] = PushApart(ThemeColor.Mix(text, surface, 0.65), surface, BodyTextRatio),
            ["--omni-color-success"] = success,
            ["--omni-color-warning"] = warning,
            ["--omni-color-danger"] = danger,
            ["--omni-color-on-accent"] = onAccent,
            ["--omni-color-on-success"] = onSuccess,
            ["--omni-color-on-danger"] = onDanger,
            ["--omni-color-accent-subtle"] = ThemeColor.Mix(accent, surface, dark ? 0.22 : 0.14),
            ["--omni-color-success-subtle"] = ThemeColor.Mix(success, surface, dark ? 0.22 : 0.14),
            ["--omni-color-warning-subtle"] = ThemeColor.Mix(warning, surface, dark ? 0.22 : 0.14),
            ["--omni-color-danger-subtle"] = ThemeColor.Mix(danger, surface, dark ? 0.22 : 0.14),
            ["--omni-color-inverse-surface"] = dark ? ThemeColor.Lighten(surface, 0.2) : ThemeColor.Mix(text, surface, 0.9),
            ["--omni-color-on-inverse"] = dark ? text : surface,
            ["--omni-chart-color-0"] = accent,
            ["--omni-chart-color-1"] = success,
            ["--omni-chart-color-2"] = warning,
            ["--omni-chart-color-3"] = info,
            ["--omni-chart-color-4"] = danger
        };

        foreach (var (name, value) in theme.Shape)
        {
            tokens[name] = value;
        }

        return tokens;
    }

    /// <summary>
    /// A filled surface and the text that sits on it. Both candidate text colours are tried, the
    /// fill being moved away from each until the pair is readable; the candidate that keeps the
    /// fill most visible against the page wins, so the brand colour is bent no further than the
    /// ratios require.
    /// </summary>
    private static (string Fill, string Over) Fill(string color, string surface)
    {
        var start = Visible(color, surface);
        var best = ("#000000", start, 0d);
        foreach (var over in new[] { "#ffffff", "#111111" })
        {
            var candidate = PushApart(start, over, OverlaidTextRatio);
            var visibility = ThemeColor.Contrast(candidate, surface);
            if (visibility > best.Item3)
            {
                best = (over, candidate, visibility);
            }
        }

        return (best.Item2, best.Item1);
    }

    /// <summary>Moves a colour away from the surface until it clears the large-element ratio.</summary>
    private static string Visible(string color, string surface) => PushApart(color, surface, AccentRatio);

    /// <summary>
    /// Walks a colour away from a reference until the two clear the given ratio, towards whichever
    /// pole the reference is not at. Stops when the ratio is met or the colour reaches the pole.
    /// </summary>
    private static string PushApart(string color, string reference, double ratio)
    {
        var target = ThemeColor.Luminance(reference) > 0.35 ? "#000000" : "#ffffff";
        var candidate = color;
        for (var step = 0; step < 40 && ThemeColor.Contrast(candidate, reference) < ratio; step++)
        {
            candidate = ThemeColor.Mix(target, candidate, 0.05);
        }

        return candidate;
    }
}
