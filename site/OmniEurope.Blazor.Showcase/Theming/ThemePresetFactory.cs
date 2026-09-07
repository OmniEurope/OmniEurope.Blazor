namespace OmniEurope.Blazor.Showcase.Theming;

/// <summary>
/// Turns an upstream palette into the two modes the catalogue offers.
/// </summary>
/// <remarks>
/// <para>
/// Upstream ships a single mode per palette. The other mode is derived here, by keeping the accent
/// and the severities and rebuilding the surfaces around them. The result is this project's work,
/// not the upstream one, which is why <see cref="ThemePreset.DerivedMode"/> records which half was
/// derived.
/// </para>
/// <para>
/// Values are then moved until they clear the WCAG ratios: 4.5 for body text and for text laid over
/// a filled surface, 3.0 for the accent against its background. Some upstream palettes do not clear
/// those ratios on their own, so a palette here can differ from its published colours. Legibility
/// wins over fidelity, and the tests hold that line.
/// </para>
/// </remarks>
public static class ThemePresetFactory
{
    private const double BodyTextRatio = 4.5;
    private const double OverlaidTextRatio = 4.5;
    private const double AccentRatio = 3.0;
    private const string DarkSurface = "#14181f";
    private const string LightSurface = "#ffffff";
    private const string DarkText = "#e8ecf4";
    private const string LightText = "#172033";

    /// <summary>Builds the light and dark halves of one palette.</summary>
    public static ThemePreset Create(ThemePalette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);
        var upstream = palette.UpstreamMode;
        var light = upstream is ThemeMode.Light
            ? Build(palette, palette.Background, palette.Foreground, ThemeMode.Light)
            : Build(palette, LightSurface, LightText, ThemeMode.Light);
        var dark = upstream is ThemeMode.Dark
            ? Build(palette, palette.Background, palette.Foreground, ThemeMode.Dark)
            : Build(palette, DarkSurface, DarkText, ThemeMode.Dark);

        return new ThemePreset(
            palette.Name,
            palette.Description,
            light,
            dark,
            "Bootswatch, licence MIT",
            upstream is ThemeMode.Light ? ThemeMode.Dark : ThemeMode.Light);
    }

    /// <summary>Every palette of the catalogue, both modes built.</summary>
    public static IReadOnlyList<ThemePreset> CreateAll(IEnumerable<ThemePalette> palettes)
    {
        ArgumentNullException.ThrowIfNull(palettes);
        return [.. palettes.Select(Create)];
    }

    private static Dictionary<string, string> Build(ThemePalette palette, string surface, string upstreamText, ThemeMode mode)
    {
        var text = PushApart(upstreamText, surface, BodyTextRatio);
        var (accent, onAccent) = Fill(palette.Primary, surface);
        var (success, onSuccess) = Fill(palette.Success, surface);
        var (danger, onDanger) = Fill(palette.Danger, surface);
        var (warning, _) = Fill(palette.Warning, surface);
        var info = Visible(palette.Info, surface);
        var dark = mode is ThemeMode.Dark;

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
