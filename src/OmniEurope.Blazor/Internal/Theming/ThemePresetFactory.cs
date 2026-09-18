using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Turns a palette into the colour tokens of both modes, and lays a theme's shape over them.
/// </summary>
/// <remarks>
/// Colours are moved until they clear the WCAG ratios: 4.5 for body text and for text laid over a
/// filled surface, 3.0 for a fill against the page. Two kinds of colour are kept apart. A text token
/// (<c>--omni-color-success</c>, <c>--omni-color-accent-strong</c>) must read as text on the page
/// and on its own tint, so it may come out darker or lighter than written. A fill token
/// (<c>--omni-color-success-fill</c>) only has to stand out from the page at 3.0, and it is the text
/// laid on it that is pushed to 4.5, so the brand colour survives. Never paint text with a fill token
/// or fill a surface with a text token. The shape of a theme (radii, borders, shadows, fonts, buttons)
/// is laid over both modes, then its dark-only shape over the dark mode.
/// </remarks>
internal static class ThemePresetFactory
{
    private const double BodyTextRatio = 4.5;
    private const double OverlaidTextRatio = 4.5;
    private const double AccentRatio = 3.0;
    private const double LightTint = 0.14;
    private const double DarkTint = 0.22;
    private const double HoverShare = 0.2;
    private const double ActiveShare = 0.34;

    /// <summary>The off-white laid over the deep warning and danger fills.</summary>
    internal const string OnDeep = "#faf7f2";

    /// <summary>The near-black laid over the bright info and success fills.</summary>
    internal const string OnBright = "#111111";

    /// <summary>Builds a palette, both modes derived.</summary>
    public static OmniThemePalette CreatePalette(PaletteDefinition palette)
    {
        ArgumentNullException.ThrowIfNull(palette);
        return new OmniThemePalette(
            palette.Name,
            palette.Description,
            BuildColors(palette, OmniAppearance.Light),
            BuildColors(palette, OmniAppearance.Dark));
    }

    /// <summary>Builds a theme painted with the given palette.</summary>
    public static OmniThemePreset CreatePreset(ThemeDefinition theme, OmniThemePalette palette)
    {
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(palette);
        var bare = new OmniThemePreset(theme.Name, theme.Description, palette.Light, palette.Dark)
        {
            Shape = theme.Shape,
            DarkShape = theme.DarkShape,
        };
        return bare.With(palette);
    }

    /// <summary>
    /// The colour tokens of one mode. The order of the operations is the one of the reference mockup
    /// (<c>plans/PLAN-008-maquette-themes.html</c>, <c>buildTokens</c>): a divergence changes values.
    /// </summary>
    public static Dictionary<string, string> BuildColors(PaletteDefinition palette, OmniAppearance mode)
    {
        ArgumentNullException.ThrowIfNull(palette);
        var dark = mode is OmniAppearance.Dark;
        var tint = dark ? DarkTint : LightTint;
        var accentColor = dark ? palette.DarkAccent ?? palette.Accent : palette.Accent;
        var surface = dark ? palette.DarkSurface : palette.LightSurface;
        var authoredText = dark ? palette.DarkText : palette.LightText;

        var text = PushApart(authoredText, surface, BodyTextRatio);
        var (accent, onAccent) = Fill(accentColor, surface);
        var (success, onSuccess) = Severity(palette.Success, surface, tint);
        var (danger, onDanger) = Severity(palette.Danger, surface, tint);
        var (warning, onWarning) = Severity(palette.Warning, surface, tint);
        var info = Visible(palette.Info, surface);
        var (infoText, onInfo) = Severity(palette.Info, surface, tint);
        var accentSubtle = ThemeColor.Mix(accent, surface, tint);

        var accentFill = ReadableFill(Visible(accentColor, surface));
        var successFill = ReadableFill(Visible(palette.Success, surface));
        var infoFill = ReadableFill(Visible(palette.Info, surface));
        var warningFill = ReadableFill(Visible(palette.Warning, surface));
        var dangerFill = ReadableFill(Visible(palette.Danger, surface));

        var warningDeep = PushApart(warningFill, OnDeep, BodyTextRatio);
        var dangerDeep = PushApart(dangerFill, OnDeep, BodyTextRatio);
        var infoBright = PushApart(infoFill, OnBright, BodyTextRatio);
        var successBright = PushApart(successFill, OnBright, BodyTextRatio);

        var accentStrong = PushApart(
            PushApart(dark ? ThemeColor.Lighten(accent, 0.25) : ThemeColor.Darken(accent, 0.2), surface, BodyTextRatio),
            accentSubtle,
            BodyTextRatio);
        var surfaceMuted = dark ? ThemeColor.Lighten(surface, 0.06) : ThemeColor.Darken(surface, 0.04);
        var surfaceHover = dark ? ThemeColor.Lighten(surface, 0.12) : ThemeColor.Darken(surface, 0.08);
        var neutralFill = ThemeColor.Mix(accent, ThemeColor.Mix(text, surface, dark ? 0.12 : 0.1), 0.05);

        var tokens = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["--omni-color-accent"] = accent,
            ["--omni-color-accent-strong"] = accentStrong,
            ["--omni-color-surface"] = surface,
            ["--omni-color-surface-muted"] = surfaceMuted,
            ["--omni-color-surface-hover"] = surfaceHover,
            ["--omni-color-surface-highlight"] = dark ? ThemeColor.Lighten(surface, 0.16) : ThemeColor.Darken(surface, 0.1),
            ["--omni-color-border"] = ThemeColor.Mix(text, surface, 0.3),
            ["--omni-color-text"] = text,
            // Measured against the hovered surface, the furthest from the page of the three it sits on
            // (page, muted, hovered: darker in light mode, lighter in dark mode), so the discreet text of
            // a hovered row or menu item stays readable, and on the two others with it (PLAN-008 lot 8).
            ["--omni-color-text-muted"] = PushApart(ThemeColor.Mix(text, surface, 0.65), surfaceHover, BodyTextRatio),
            ["--omni-color-success"] = success,
            ["--omni-color-warning"] = warning,
            ["--omni-color-danger"] = danger,
            ["--omni-color-info"] = infoText,
            ["--omni-color-on-accent"] = onAccent,
            ["--omni-color-on-success"] = onSuccess,
            ["--omni-color-on-warning"] = onWarning,
            ["--omni-color-on-danger"] = onDanger,
            ["--omni-color-on-info"] = onInfo,
            ["--omni-color-accent-subtle"] = accentSubtle,
            ["--omni-color-success-subtle"] = ThemeColor.Mix(success, surface, tint),
            ["--omni-color-warning-subtle"] = ThemeColor.Mix(warning, surface, tint),
            ["--omni-color-danger-subtle"] = ThemeColor.Mix(danger, surface, tint),
            ["--omni-color-info-subtle"] = ThemeColor.Mix(infoText, surface, tint),
            ["--omni-color-inverse-surface"] = dark ? ThemeColor.Lighten(surface, 0.2) : ThemeColor.Mix(text, surface, 0.9),
            ["--omni-color-on-inverse"] = dark ? text : surface,
            ["--omni-color-warning-deep"] = warningDeep,
            ["--omni-color-warning-deep-hover"] = AwayFrom(warningDeep, OnDeep, HoverShare),
            ["--omni-color-warning-deep-active"] = AwayFrom(warningDeep, OnDeep, ActiveShare),
            ["--omni-color-danger-deep"] = dangerDeep,
            ["--omni-color-danger-deep-hover"] = AwayFrom(dangerDeep, OnDeep, HoverShare),
            ["--omni-color-danger-deep-active"] = AwayFrom(dangerDeep, OnDeep, ActiveShare),
            ["--omni-color-on-deep"] = OnDeep,
            ["--omni-color-info-bright"] = infoBright,
            ["--omni-color-info-bright-hover"] = AwayFrom(infoBright, OnBright, HoverShare),
            ["--omni-color-info-bright-active"] = AwayFrom(infoBright, OnBright, ActiveShare),
            ["--omni-color-success-bright"] = successBright,
            ["--omni-color-success-bright-hover"] = AwayFrom(successBright, OnBright, HoverShare),
            ["--omni-color-success-bright-active"] = AwayFrom(successBright, OnBright, ActiveShare),
            ["--omni-color-on-bright"] = OnBright,
            ["--omni-color-neutral-fill"] = neutralFill,
            ["--omni-color-neutral-fill-hover"] = TowardWhileReadable(neutralFill, text, text, 0.1),
            ["--omni-color-neutral-fill-active"] = TowardWhileReadable(neutralFill, text, text, 0.18),
            // Elevation depends on the mode, not on the palette: a 22 % black shadow disappears on a
            // dark page, and the top-edge highlight only exists in dark mode.
            ["--omni-elevation-shadow"] = dark ? "rgb(0 0 0 / 60%)" : "rgb(0 0 0 / 22%)",
            ["--omni-elevation-shadow-soft"] = dark ? "rgb(0 0 0 / 40%)" : "rgb(0 0 0 / 12%)",
            ["--omni-elevation-highlight"] = dark ? "rgb(255 255 255 / 7%)" : "rgb(255 255 255 / 0%)",
            // Trial (PLAN-008 T14), kept together so that removing it is one commit: a layer drawn
            // after the menus of a desktop system, recreated from the rendering, no code reused. In
            // dark mode the layer is one step lighter than the page with a light hairline and a top
            // highlight, since a shadow alone detaches nothing there.
            ["--omni-layer-fill"] = dark ? ThemeColor.Lighten(surface, 0.05) : surface,
            ["--omni-layer-stroke"] = dark ? "rgb(255 255 255 / 9%)" : "rgb(0 0 0 / 7%)",
            ["--omni-layer-shadow"] = dark
                ? "inset 0 1px 0 rgb(255 255 255 / 5%), 0 0 2px rgb(0 0 0 / 30%), 0 4px 10px rgb(0 0 0 / 34%)"
                : "0 0 2px rgb(0 0 0 / 10%), 0 4px 10px rgb(0 0 0 / 8%)",
            ["--omni-layer-shadow-deep"] = dark
                ? "inset 0 1px 0 rgb(255 255 255 / 6%), 0 0 4px rgb(0 0 0 / 34%), 0 16px 40px rgb(0 0 0 / 46%)"
                : "0 0 4px rgb(0 0 0 / 10%), 0 16px 40px rgb(0 0 0 / 16%)",
            ["--omni-layer-acrylic"] = WithAlpha(dark ? ThemeColor.Lighten(surface, 0.07) : ThemeColor.Darken(surface, 0.015), dark ? 0.86 : 0.84),
            ["--omni-chart-color-0"] = accent,
            ["--omni-chart-color-1"] = success,
            ["--omni-chart-color-2"] = warning,
            ["--omni-chart-color-3"] = info,
            ["--omni-chart-color-4"] = danger,
        };

        AddFill(tokens, "accent", accentFill);
        AddFill(tokens, "success", successFill);
        AddFill(tokens, "info", infoFill);
        AddFill(tokens, "warning", warningFill);
        AddFill(tokens, "danger", dangerFill);
        return tokens;
    }

    /// <summary>
    /// A fill, its text and its two states. Hover and press move the fill away from the colour of its
    /// text, so the contrast can only rise and the change is visible on any hue.
    /// </summary>
    private static void AddFill(Dictionary<string, string> tokens, string name, string fill)
    {
        var ink = PushApart(ThemeColor.ReadableOn(fill), fill, OverlaidTextRatio);
        tokens[$"--omni-color-{name}-fill"] = fill;
        tokens[$"--omni-color-{name}-fill-hover"] = AwayFrom(fill, ink, HoverShare);
        tokens[$"--omni-color-{name}-fill-active"] = AwayFrom(fill, ink, ActiveShare);
        tokens[$"--omni-color-on-{name}-fill"] = ink;
    }

    /// <summary>A fill on which neither white nor black would reach 4.5 is pushed away from the better of the two.</summary>
    private static string ReadableFill(string fill) => PushApart(fill, ThemeColor.ReadableOn(fill), OverlaidTextRatio);

    private static string AwayFrom(string fill, string ink, double amount) =>
        ThemeColor.Mix(ThemeColor.Luminance(ink) > 0.35 ? "#000000" : "#ffffff", fill, amount);

    /// <summary>Moves a colour towards another while a text colour still reads on it at 4.5.</summary>
    private static string TowardWhileReadable(string baseColor, string toward, string ink, double amount)
    {
        for (var share = amount; share > 0.001; share -= 0.02)
        {
            var candidate = ThemeColor.Mix(toward, baseColor, share);
            if (ThemeColor.Contrast(ink, candidate) >= OverlaidTextRatio)
            {
                return candidate;
            }
        }

        return baseColor;
    }

    private static string WithAlpha(string hex, double alpha)
    {
        var (r, g, b) = ThemeColor.Parse(hex);
        return string.Create(System.Globalization.CultureInfo.InvariantCulture, $"rgb({r} {g} {b} / {Math.Round(alpha * 100)}%)");
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
    /// A severity colour written as text. It keeps moving away from the surface until it reads on the
    /// page and on its own tint; the text laid over it is picked again at the end.
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
    internal static string PushApart(string color, string reference, double ratio)
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
