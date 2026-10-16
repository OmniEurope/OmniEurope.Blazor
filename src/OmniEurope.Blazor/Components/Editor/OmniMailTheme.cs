using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The colours, font and corner radius <see cref="OmniMailRenderer"/> writes into a mail. A mail client
/// reads no stylesheet and resolves no CSS variable, so every value is a literal one, resolved on the
/// server: <see cref="From(OmniThemePreset, OmniAppearance)"/> takes them from a theme of the package,
/// <see cref="From(OmniThemePalette, OmniAppearance)"/> from a palette, <see cref="FromTokens"/> from any
/// set of <c>--omni-*</c> tokens, so a mail can wear the colours of its reader's appearance.
/// </summary>
/// <remarks>
/// A colour is a hexadecimal colour (<c>#4340d2</c>), an <c>rgb()</c>, <c>rgba()</c>, <c>hsl()</c> or
/// <c>hsla()</c> of numbers, or a colour keyword; a font is a list of family names. Anything else would
/// let a value escape its <c>style</c> attribute, so <see cref="OmniMailRenderer.Render"/> refuses it.
/// </remarks>
public sealed record OmniMailTheme
{
    // Last-resort values, used where a token is missing or not literal; constants, so the initialisers
    // below do not read a static that is still being built.
    private const string FallbackPage = "#f2f2f2";
    private const string FallbackRadius = "8px";
    private static readonly OmniMailTheme Fallback = new();

    /// <summary>The light half of the package's default theme and palette.</summary>
    public static OmniMailTheme Default { get; } = From(OmniThemePresets.All[0]);

    /// <summary>Behind the mail's card, to the edges of the reader's window.</summary>
    public string PageBackground { get; init; } = FallbackPage;

    /// <summary>The mail's card, under its text.</summary>
    public string Surface { get; init; } = "#ffffff";

    /// <summary>The body text and the headings.</summary>
    public string Text { get; init; } = "#1f1f1f";

    /// <summary>Quotations and table captions.</summary>
    public string MutedText { get; init; } = "#5c5c5c";

    /// <summary>The rule beside a quotation, and the fill of an action button.</summary>
    public string Accent { get; init; } = "#4340d2";

    /// <summary>Text drawn on <see cref="Accent"/>: the label of an action button.</summary>
    public string OnAccent { get; init; } = "#ffffff";

    /// <summary>Links.</summary>
    public string Link { get; init; } = "#3532a8";

    /// <summary>The card's frame, table cells and horizontal rules.</summary>
    public string Border { get; init; } = "#c7c7c7";

    /// <summary>Behind marked text.</summary>
    public string Highlight { get; init; } = "#e7e6fa";

    /// <summary>Behind code, code blocks and table headers.</summary>
    public string CodeBackground { get; init; } = FallbackPage;

    /// <summary>The rule at the start of an information banner (<c>--omni-color-info</c>).</summary>
    public string Info { get; init; } = "#1565c0";

    /// <summary>Behind an information banner (<c>--omni-color-info-subtle</c>).</summary>
    public string InfoSubtle { get; init; } = "#e3eefa";

    /// <summary>The rule at the start of a success banner (<c>--omni-color-success</c>).</summary>
    public string Success { get; init; } = "#2e7d32";

    /// <summary>Behind a success banner (<c>--omni-color-success-subtle</c>).</summary>
    public string SuccessSubtle { get; init; } = "#e6f2e7";

    /// <summary>The rule at the start of a warning banner (<c>--omni-color-warning</c>).</summary>
    public string Warning { get; init; } = "#b26a00";

    /// <summary>Behind a warning banner (<c>--omni-color-warning-subtle</c>).</summary>
    public string WarningSubtle { get; init; } = "#fbf0dd";

    /// <summary>The rule at the start of a danger banner (<c>--omni-color-danger</c>).</summary>
    public string Danger { get; init; } = "#c62828";

    /// <summary>Behind a danger banner (<c>--omni-color-danger-subtle</c>).</summary>
    public string DangerSubtle { get; init; } = "#fae6e6";

    /// <summary>The text font, followed by fonts every mail client has (Arial, Helvetica, sans-serif) when it does not end with a generic family.</summary>
    public string FontFamily { get; init; } = MailCss.SafeFontStack;

    /// <summary>The radius of the card's corners, in pixels (<c>8px</c>); clients that ignore it draw square corners.</summary>
    public string Radius { get; init; } = FallbackRadius;

    /// <summary>Whether the colours are a dark half: the mail then declares a dark colour scheme to the clients that read it.</summary>
    public bool Dark { get; init; }

    /// <summary>A theme of the package, in one mode (<see cref="OmniAppearance.System"/> reads as light).</summary>
    public static OmniMailTheme From(OmniThemePreset theme, OmniAppearance mode = OmniAppearance.Light)
    {
        ArgumentNullException.ThrowIfNull(theme);
        return FromTokens(theme.For(mode), mode is OmniAppearance.Dark);
    }

    /// <summary>A palette of the package, in one mode, with the default font and radius.</summary>
    public static OmniMailTheme From(OmniThemePalette palette, OmniAppearance mode = OmniAppearance.Light)
    {
        ArgumentNullException.ThrowIfNull(palette);
        return FromTokens(palette.For(mode), mode is OmniAppearance.Dark);
    }

    /// <summary>
    /// Reads the <c>--omni-*</c> tokens of a theme: <c>--omni-color-surface-muted</c> behind the card,
    /// <c>--omni-color-surface</c>, <c>--omni-color-text</c>, <c>--omni-color-text-muted</c>,
    /// <c>--omni-color-accent</c>, <c>--omni-color-on-accent</c>, <c>--omni-color-accent-strong</c> for links,
    /// <c>--omni-color-border</c>, <c>--omni-color-accent-subtle</c> for marked text,
    /// <c>--omni-color-info</c>, <c>--omni-color-success</c>, <c>--omni-color-warning</c>,
    /// <c>--omni-color-danger</c> and their <c>-subtle</c> tokens for the banners,
    /// <c>--omni-font-family</c> and <c>--omni-card-radius</c> (else <c>--omni-radius</c>). A missing token,
    /// or one that is not a literal value (a <c>var()</c>, a <c>color-mix()</c>), keeps the default.
    /// </summary>
    public static OmniMailTheme FromTokens(IReadOnlyDictionary<string, string> tokens, bool dark = false)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        string ColorOr(string name, string fallback) =>
            tokens.TryGetValue(name, out var value) && MailCss.IsColor(value) ? value.Trim() : fallback;

        var surfaceMuted = ColorOr("--omni-color-surface-muted", Fallback.PageBackground);
        var radius = MailCss.Pixels(tokens.GetValueOrDefault("--omni-card-radius"))
            ?? MailCss.Pixels(tokens.GetValueOrDefault("--omni-radius"))
            ?? Fallback.Radius;
        return new OmniMailTheme
        {
            PageBackground = surfaceMuted,
            Surface = ColorOr("--omni-color-surface", Fallback.Surface),
            Text = ColorOr("--omni-color-text", Fallback.Text),
            MutedText = ColorOr("--omni-color-text-muted", Fallback.MutedText),
            Accent = ColorOr("--omni-color-accent", Fallback.Accent),
            OnAccent = ColorOr("--omni-color-on-accent", Fallback.OnAccent),
            Link = ColorOr("--omni-color-accent-strong", Fallback.Link),
            Border = ColorOr("--omni-color-border", Fallback.Border),
            Highlight = ColorOr("--omni-color-accent-subtle", Fallback.Highlight),
            CodeBackground = surfaceMuted,
            Info = ColorOr("--omni-color-info", Fallback.Info),
            InfoSubtle = ColorOr("--omni-color-info-subtle", Fallback.InfoSubtle),
            Success = ColorOr("--omni-color-success", Fallback.Success),
            SuccessSubtle = ColorOr("--omni-color-success-subtle", Fallback.SuccessSubtle),
            Warning = ColorOr("--omni-color-warning", Fallback.Warning),
            WarningSubtle = ColorOr("--omni-color-warning-subtle", Fallback.WarningSubtle),
            Danger = ColorOr("--omni-color-danger", Fallback.Danger),
            DangerSubtle = ColorOr("--omni-color-danger-subtle", Fallback.DangerSubtle),
            FontFamily = tokens.TryGetValue("--omni-font-family", out var font) && MailCss.IsFontFamily(font)
                ? MailCss.WithSafeFallback(font)
                : Fallback.FontFamily,
            Radius = radius,
            Dark = dark,
        };
    }
}
