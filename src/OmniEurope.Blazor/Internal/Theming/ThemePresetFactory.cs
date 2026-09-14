using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Turns a theme definition into the two modes the catalogue offers.
/// </summary>
/// <remarks>
/// Colours are moved until they clear the WCAG ratios: 4.5 for body text and for text laid over a
/// filled surface, 3.0 for the accent against its background. The stylesheet also writes text in the
/// severity colours (badges, outlined alerts, validation messages) and in the strong accent (links,
/// tabs, ghost buttons, the accent badge), so those clear 4.5 against the page and against their own
/// tint too. A definition can therefore come out a little darker or lighter than written; legibility
/// wins, and the tests hold that line. The shape of the theme (radii, borders, shadows, fonts,
/// buttons) is laid over both modes unchanged.
/// </remarks>
internal static class ThemePresetFactory
{
    private const double BodyTextRatio = 4.5;
    private const double OverlaidTextRatio = 4.5;
    private const double AccentRatio = 3.0;
    private const double LightTint = 0.14;
    private const double DarkTint = 0.22;

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
        var dark = mode is OmniAppearance.Dark;
        var tint = dark ? DarkTint : LightTint;
        var text = PushApart(authoredText, surface, BodyTextRatio);
        var (accent, onAccent) = Fill(accentColor, surface);
        var (success, onSuccess) = Severity(theme.Success, surface, tint);
        var (danger, onDanger) = Severity(theme.Danger, surface, tint);
        var (warning, onWarning) = Severity(theme.Warning, surface, tint);
        var info = Visible(theme.Info, surface);
        var accentSubtle = ThemeColor.Mix(accent, surface, tint);
        var accentStrong = PushApart(
            PushApart(dark ? ThemeColor.Lighten(accent, 0.25) : ThemeColor.Darken(accent, 0.2), surface, BodyTextRatio),
            accentSubtle,
            BodyTextRatio);
        var surfaceMuted = dark ? ThemeColor.Lighten(surface, 0.06) : ThemeColor.Darken(surface, 0.04);

        var tokens = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["--omni-color-accent"] = accent,
            ["--omni-color-accent-strong"] = accentStrong,
            ["--omni-color-surface"] = surface,
            ["--omni-color-surface-muted"] = surfaceMuted,
            ["--omni-color-surface-hover"] = dark ? ThemeColor.Lighten(surface, 0.12) : ThemeColor.Darken(surface, 0.08),
            ["--omni-color-surface-highlight"] = dark ? ThemeColor.Lighten(surface, 0.16) : ThemeColor.Darken(surface, 0.1),
            ["--omni-color-border"] = ThemeColor.Mix(text, surface, 0.3),
            ["--omni-color-text"] = text,
            // Measured against the muted surface, the darker of the two in light mode and the lighter
            // in dark mode, so the discreet text stays readable on both.
            ["--omni-color-text-muted"] = PushApart(ThemeColor.Mix(text, surface, 0.65), surfaceMuted, BodyTextRatio),
            ["--omni-color-success"] = success,
            ["--omni-color-warning"] = warning,
            ["--omni-color-danger"] = danger,
            ["--omni-color-on-accent"] = onAccent,
            ["--omni-color-on-success"] = onSuccess,
            ["--omni-color-on-warning"] = onWarning,
            ["--omni-color-on-danger"] = onDanger,
            ["--omni-color-accent-subtle"] = accentSubtle,
            ["--omni-color-success-subtle"] = ThemeColor.Mix(success, surface, tint),
            ["--omni-color-warning-subtle"] = ThemeColor.Mix(warning, surface, tint),
            ["--omni-color-danger-subtle"] = ThemeColor.Mix(danger, surface, tint),
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

    /// <summary>
    /// A severity colour. It is a fill with its own readable text, like the accent, and it is also
    /// written as text on the page and on its own tint, so it keeps moving away from the surface
    /// until both of those read too. Moving away from the surface only widens the gap with the text
    /// laid over the fill, which is picked again at the end.
    /// </summary>
    private static (string Fill, string Over) Severity(string color, string surface, double tint)
    {
        var (fill, _) = Fill(color, surface);
        var target = ThemeColor.Luminance(surface) > 0.35 ? "#000000" : "#ffffff";
        for (var step = 0; step < 40 && !ReadsAsText(fill, surface, tint); step++)
        {
            fill = ThemeColor.Mix(target, fill, 0.05);
        }

        return (fill, ThemeColor.ReadableOn(fill));
    }

    private static bool ReadsAsText(string color, string surface, double tint) =>
        ThemeColor.Contrast(color, surface) >= BodyTextRatio
        && ThemeColor.Contrast(color, ThemeColor.Mix(color, surface, tint)) >= BodyTextRatio;

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
