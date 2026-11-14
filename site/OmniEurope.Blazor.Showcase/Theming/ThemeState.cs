using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Theming;

/// <summary>
/// The look of the showcase: every appearance setting (theme, palette, mode, density, backdrop and its motion, font,
/// text size, size of the controls, content width) is the package's own <see cref="OmniAppearanceState"/>, kept as the
/// visitor's preference and shared with other tabs by the package; this type only adds what the package has no notion
/// of, the tokens the customizer edits by hand, and pushes the combination onto the live page.
/// </summary>
/// <remarks>
/// Values are written through the CSSOM rather than through a style attribute or an injected style
/// element: the site ships a strict policy that forbids both, and the CSSOM path is the one the
/// policy leaves open. Both halves are pushed, so the page follows the system setting as it changes
/// when the visitor previews the system mode. A setting changed elsewhere (another tab, the host's store, a
/// demonstration that follows the state) repaints the page the same way as one changed here.
/// </remarks>
public sealed class ThemeState : IDisposable
{
    /// <summary>
    /// The browser entry of the showcase: the tokens edited by hand, the one thing the package state does not keep. Before
    /// the showcase delegated its look to the package, the same entry also held the appearance settings; they are moved
    /// into the package state once, at the first visit that finds them, and the entry is written back with the edits alone.
    /// </summary>
    private const string StorageKey = "omnieurope.showcase.theme";

    // Keys of the entry written before the delegation, read once to move the visitor's look into the package state.
    // Every token name starts with "--", so the two kinds cannot collide, and an entry written before the theme and the
    // palette existed (a plain map of edited tokens) still reads as the edits it was.
    private const string ThemeKey = "theme";
    private const string PaletteKey = "palette";
    private const string ModeKey = "mode";
    private const string DensityKey = "density";
    private const string MotionKey = "motion";
    private const string BackdropKey = "backdrop";
    private const string FontKey = "font";
    private const string TextSizeKey = "textSize";
    private const string ControlSizeKey = "controlSize";
    private const string FullWidthKey = "fullWidth";

    /// <summary>The shape token a theme sets when it moves its colour field.</summary>
    private const string MotionToken = "--omni-scope-motion";

    // Names a theme or a palette of the catalogue carried before it was renamed, mapped to its current name, so a
    // combination a visitor kept in the browser under the old name still comes back when it is moved to the package state.
    private static readonly Dictionary<string, string> RenamedThemes = new(StringComparer.Ordinal)
    {
        ["Défaut"] = "Essentiel"
    };

    private static readonly Dictionary<string, string> RenamedPalettes = new(StringComparer.Ordinal)
    {
        ["Défaut"] = "Essentiel",
        ["Crépuscule"] = "Opale"
    };

    private readonly ThemeTokenReader _reader;
    private readonly IJSRuntime _js;
    private readonly OmniAppearanceState _appearance;
    private readonly Dictionary<string, string> _edits = new(StringComparer.Ordinal);
    private IReadOnlyList<ThemeToken> _tokens = [];
    private OmniThemePreset _painted = OmniThemePresets.All[0];
    private DrawnLook _drawn;
    private int _changing;
    private bool _initialized;

    /// <summary>Creates the state over the package's appearance state, which holds every setting.</summary>
    public ThemeState(ThemeTokenReader reader, IJSRuntime js, OmniAppearanceState appearance)
    {
        ArgumentNullException.ThrowIfNull(appearance);
        _reader = reader;
        _js = js;
        _appearance = appearance;
        Remember();
        Painted = _painted;
        _appearance.Changed += OnAppearanceChanged;
    }

    /// <summary>Raised whenever the look changes, here or elsewhere, so open editors redraw.</summary>
    public event Action? Changed;

    /// <summary>The token catalogue read from the stylesheet (showcase only: the customizer's editors).</summary>
    public IReadOnlyList<ThemeToken> Tokens => _tokens;

    /// <summary>The theme that gives the shape: the package state's, the first of the catalogue when it holds none.</summary>
    public OmniThemePreset Theme => _appearance.Preset ?? OmniThemePresets.All[0];

    /// <summary>
    /// The theme as the page shows it: the theme painted with the palette, edits included, in a single
    /// preset. The layout hands it to its theme scope, which draws the theme's canvas backdrop from its
    /// shape and writes the very tokens <see cref="Light"/> and <see cref="Dark"/> push, so the scope and
    /// the CSSOM push agree and an edit survives the scope repainting. A new instance follows each push.
    /// </summary>
    public OmniThemePreset Painted { get; private set; }

    /// <summary>The palette that gives the colours: the package state's, the theme's own when it holds none.</summary>
    public OmniThemePalette Palette => _appearance.Palette ?? OmniThemePresets.DefaultPaletteFor(Theme);

    /// <summary>Whether the palette is the one the theme comes with.</summary>
    public bool HasThemePalette => ReferenceEquals(Palette, OmniThemePresets.DefaultPaletteFor(Theme));

    /// <summary>The mode the visitor chose, the package state's.</summary>
    public OmniAppearance Mode => _appearance.Appearance;

    /// <summary>The mode drawn: <see cref="Mode"/>, except under a theme drawn in dark mode only, always dark.</summary>
    public OmniAppearance EffectiveMode => Theme.DarkOnly ? OmniAppearance.Dark : Mode;

    /// <summary>The density of the whole page, which a section with a density of its own overrides.</summary>
    public OmniDensity Density => _appearance.Density;

    /// <summary>The font chosen for the text and the headings, or null for the one the theme comes with.</summary>
    public OmniThemeFont? Font => _appearance.Font;

    /// <summary>The text size level, 1 to 10; the package's default level leaves the page as shipped.</summary>
    public int TextSizeLevel => _appearance.TextSizeLevel;

    /// <summary>The size level of the controls, 1 to 10; the package's default level leaves them as shipped.</summary>
    public int ControlSizeLevel => _appearance.ControlSizeLevel;

    /// <summary>Whether the content takes the whole width instead of the centred column.</summary>
    public bool FullWidth => _appearance.FullWidth;

    /// <summary>Whether the theme may move its colour field; the layout hands it to its theme scope.</summary>
    public bool BackdropMotion => _appearance.BackdropMotion;

    /// <summary>Whether the backdrop drawn moves its colour field, so the setting that holds it still has something to hold.</summary>
    public bool MovesBackdrop => _painted.Shape.ContainsKey(MotionToken);

    /// <summary>
    /// The backdrop chosen for the theme, or null for the one the theme comes with; a backdrop the theme does
    /// not offer is not drawn.
    /// </summary>
    public OmniThemeBackdrop? Backdrop => _appearance.Backdrop;

    /// <summary>The backdrop the page is drawn over: <see cref="Backdrop"/> when the theme offers it, the theme's own otherwise.</summary>
    public OmniThemeBackdrop DrawnBackdrop => Backdrop is { } chosen && OmniThemePresets.BackdropsFor(Theme).Contains(chosen) ? chosen : OmniThemePresets.DefaultBackdropFor(Theme);

    /// <summary>The tokens the visitor edited by hand, laid over both halves of the combination (showcase only).</summary>
    public IReadOnlyDictionary<string, string> Edits => _edits;

    /// <summary>The light half in force: the theme painted with the palette, then the edits.</summary>
    public IReadOnlyDictionary<string, string> Light => Overlay(_painted.Light);

    /// <summary>The dark half in force: the palette's dark colours, the shape, the theme's dark shape, then the edits.</summary>
    public IReadOnlyDictionary<string, string> Dark => Overlay(_painted.Dark);

    /// <summary>
    /// Loads the catalogue, the package state and the edits this browser kept, moves a look kept under the showcase's
    /// former entry into the package state, then paints the page.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _tokens = await _reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        await _appearance.LoadAsync(cancellationToken).ConfigureAwait(false);
        var stored = await _js.InvokeAsync<string?>("omniShowcaseTheme.load", cancellationToken, StorageKey)
            .ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(stored))
        {
            await RestoreAsync(stored).ConfigureAwait(false);
        }

        _initialized = true;
        Remember();
        await PushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>The value in force for a token in the mode previewed, shipped value included.</summary>
    /// <remarks>The system mode shows the light half: which half the system picks is only known in the browser.</remarks>
    public string ValueOf(ThemeToken token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return CombinationValueOf(token.Name) ?? token.DefaultValue;
    }

    /// <summary>Moves one token over both halves and repaints the page.</summary>
    public async Task SetAsync(ThemeToken token, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);
        _edits.Remove(token.Name);
        var inForce = CombinationValueOf(token.Name) ?? token.DefaultValue;
        if (!string.Equals(value, inForce, StringComparison.Ordinal))
        {
            _edits[token.Name] = value;
        }

        await PushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Drops every edit and returns to the combination as the catalogue draws it.</summary>
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _edits.Clear();
        await PushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Picks a theme, painted with its own palette over its own backdrop: a theme is chosen whole, and the palette picker
    /// then repaints it. The package state also drops the font chosen for another theme. The edits, made for the previous
    /// combination, are dropped, even when the theme stays (Reset all of the appearance window picks the first theme again).
    /// </summary>
    public Task SelectThemeAsync(OmniThemePreset theme, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(theme);
        _edits.Clear();
        return ChangeAsync(
            async () =>
            {
                await _appearance.SetPresetAsync(ReferenceEquals(theme, OmniThemePresets.All[0]) ? null : theme).ConfigureAwait(false);
                await _appearance.SetPaletteAsync(null).ConfigureAwait(false);
                await _appearance.SetBackdropAsync(null).ConfigureAwait(false);
            },
            cancellationToken);
    }

    /// <summary>Repaints the theme with another palette; the theme and its shape stay, the edits are dropped.</summary>
    public Task SelectPaletteAsync(OmniThemePalette palette, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(palette);
        _edits.Clear();
        var own = ReferenceEquals(palette, OmniThemePresets.DefaultPaletteFor(Theme));
        return ChangeAsync(() => _appearance.SetPaletteAsync(own ? null : palette), cancellationToken);
    }

    /// <summary>Returns to the palette the theme comes with.</summary>
    public Task ResetPaletteAsync(CancellationToken cancellationToken = default) =>
        SelectPaletteAsync(OmniThemePresets.DefaultPaletteFor(Theme), cancellationToken);

    /// <summary>Switches the mode previewed; both halves are already on the page.</summary>
    public Task SetModeAsync(OmniAppearance mode, CancellationToken cancellationToken = default) =>
        ChangeAsync(() => _appearance.SetAppearanceAsync(mode), cancellationToken);

    /// <summary>Sets the density of the whole page.</summary>
    public Task SetDensityAsync(OmniDensity density, CancellationToken cancellationToken = default) =>
        ChangeAsync(() => _appearance.SetDensityAsync(density), cancellationToken);

    /// <summary>Sets the font of the text and the headings, or null for the theme's own.</summary>
    public Task SetFontAsync(OmniThemeFont? font, CancellationToken cancellationToken = default) =>
        ChangeAsync(() => _appearance.SetFontAsync(font), cancellationToken);

    /// <summary>Sets the text size level; the package state brings a level outside 1 to 10 back into it.</summary>
    public Task SetTextSizeLevelAsync(int level, CancellationToken cancellationToken = default) =>
        ChangeAsync(() => _appearance.SetTextSizeLevelAsync(level), cancellationToken);

    /// <summary>Sets the control size level; the package state brings a level outside 1 to 10 back into it.</summary>
    public Task SetControlSizeLevelAsync(int level, CancellationToken cancellationToken = default) =>
        ChangeAsync(() => _appearance.SetControlSizeLevelAsync(level), cancellationToken);

    /// <summary>Lets the content take the whole width, or returns it to the centred column.</summary>
    public Task SetFullWidthAsync(bool fullWidth, CancellationToken cancellationToken = default) =>
        ChangeAsync(() => _appearance.SetFullWidthAsync(fullWidth), cancellationToken);

    /// <summary>Lets the theme move its colour field, or holds it still.</summary>
    public Task SetBackdropMotionAsync(bool moves, CancellationToken cancellationToken = default) =>
        ChangeAsync(() => _appearance.SetBackdropMotionAsync(moves), cancellationToken);

    /// <summary>
    /// Draws the theme over another backdrop, or over its own with null; the theme, its palette and the
    /// edits stay, the edits of the previous backdrop's tokens aside: they are dropped with it.
    /// </summary>
    public Task SelectBackdropAsync(OmniThemeBackdrop? backdrop, CancellationToken cancellationToken = default)
    {
        DropEditsOf(DrawnBackdrop);
        var chosen = backdrop is null || ReferenceEquals(backdrop, OmniThemePresets.DefaultBackdropFor(Theme)) ? null : backdrop;
        return ChangeAsync(() => _appearance.SetBackdropAsync(chosen), cancellationToken);
    }

    /// <summary>
    /// The stylesheet the visitor can paste into their own application: the combination, both
    /// halves, edits included. Tokens come in stylesheet order, then those a theme sets without the
    /// stylesheet declaring them at the root (the button press, the card border width).
    /// </summary>
    /// <remarks>
    /// The stylesheet declares its tokens again on every theme scope (<c>[data-omni-theme]</c>), so a
    /// value written on <c>:root</c> alone is shadowed inside an <c>OmniThemeScope</c> and on any
    /// element carrying the attribute. The export therefore names the scope for each mode, after
    /// the stylesheet so it wins at equal specificity: the light half for <c>:root</c>, the light scope
    /// and the system scope, the dark half for the dark scope and, under a dark system setting, for
    /// the system scope. The dark half carries the theme's dark shape.
    /// </remarks>
    public string ExportCss()
    {
        var light = Light;
        var dark = Dark;
        var builder = new StringBuilder();
        builder.Append("/* OmniEurope.Blazor: theme ").Append(Theme.Name).Append(", palette ").Append(Palette.Name);
        if (_edits.Count > 0)
        {
            builder.Append(", ").Append(_edits.Count.ToString(CultureInfo.InvariantCulture)).Append(" token(s) edited");
        }

        builder.AppendLine(". Load after omnieurope.blazor.css. */");
        builder.AppendLine("/* The values target the theme scope (OmniThemeScope, data-omni-theme), where the stylesheet redeclares its tokens. */");
        builder.AppendLine(":root,");
        builder.AppendLine("[data-omni-theme=\"light\"],");
        builder.AppendLine("[data-omni-theme=\"system\"] {");
        AppendDeclarations(builder, light, "    ");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("[data-omni-theme=\"dark\"] {");
        AppendDeclarations(builder, dark, "    ");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("@media (prefers-color-scheme: dark) {");
        builder.AppendLine("    [data-omni-theme=\"system\"] {");
        AppendDeclarations(builder, dark, "        ");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString();
    }

    /// <summary>Stops following the package state.</summary>
    public void Dispose() => _appearance.Changed -= OnAppearanceChanged;

    /// <summary>
    /// Runs a change of the package state made here, then repaints and pushes once: the package raises its own change
    /// event for each setting, which this state ignores while it applies its own change.
    /// </summary>
    private async Task ChangeAsync(Func<Task> change, CancellationToken cancellationToken)
    {
        _changing++;
        try
        {
            await change().ConfigureAwait(false);
        }
        finally
        {
            _changing--;
        }

        Follow();
        await PushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>A setting changed elsewhere (another tab, the host's store, a demonstration that follows the state).</summary>
    private void OnAppearanceChanged()
    {
        if (_changing > 0)
        {
            return;
        }

        if (!_initialized)
        {
            // Nothing is painted yet and the edits are not read: the look is only noted, InitializeAsync paints it.
            Remember();
            Changed?.Invoke();
            return;
        }

        Follow();
        _ = PushQuietlyAsync();
    }

    private async Task PushQuietlyAsync()
    {
        try
        {
            await PushAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is JSException or JSDisconnectedException or OperationCanceledException or ObjectDisposedException)
        {
            // The page is gone or its script refused: the next change pushes again.
        }
    }

    /// <summary>
    /// Takes the look of the package state: edits made for another theme or palette are dropped, those of a backdrop
    /// that is no longer drawn too, as when the visitor picks them here.
    /// </summary>
    private void Follow()
    {
        var now = Look();
        if (!ReferenceEquals(now.Theme, _drawn.Theme) || !ReferenceEquals(now.Palette, _drawn.Palette))
        {
            _edits.Clear();
        }
        else if (!ReferenceEquals(now.Backdrop, _drawn.Backdrop))
        {
            DropEditsOf(_drawn.Backdrop);
        }

        _drawn = now;
        Repaint();
    }

    /// <summary>Takes the look of the package state as it is, edits kept.</summary>
    private void Remember()
    {
        _drawn = Look();
        Repaint();
    }

    private DrawnLook Look() => new(Theme, Palette, DrawnBackdrop);

    private void DropEditsOf(OmniThemeBackdrop backdrop)
    {
        foreach (var name in backdrop.Shape.Keys.Concat(backdrop.DarkShape.Keys))
        {
            _edits.Remove(name);
        }
    }

    private string? CombinationValueOf(string name)
    {
        var half = EffectiveMode is OmniAppearance.Dark ? _painted.Dark : _painted.Light;
        return _edits.TryGetValue(name, out var edited) ? edited : half.TryGetValue(name, out var value) ? value : null;
    }

    private Dictionary<string, string> Overlay(IReadOnlyDictionary<string, string> half)
    {
        var merged = new Dictionary<string, string>(half, StringComparer.Ordinal);
        if (Font is not null)
        {
            merged["--omni-font-family"] = Font.Family;
            merged["--omni-heading-font-family"] = Font.Family;
        }

        foreach (var (name, value) in _edits)
        {
            merged[name] = value;
        }

        return merged;
    }

    private void AppendDeclarations(StringBuilder builder, IReadOnlyDictionary<string, string> tokens, string indent)
    {
        var catalogue = _tokens.Select(token => token.Name).ToArray();
        var known = catalogue.Where(tokens.ContainsKey);
        var unknown = tokens.Keys.Except(catalogue, StringComparer.Ordinal).Order(StringComparer.Ordinal);
        foreach (var name in known.Concat(unknown))
        {
            builder.Append(indent).Append(name).Append(": ").Append(tokens[name]).AppendLine(";");
        }
    }

    private void Repaint() => _painted = Theme.With(Palette).With(DrawnBackdrop);

    /// <summary>The current name of a stored theme or palette name, the name itself when it was never renamed.</summary>
    private static string LegacyName(string stored, IReadOnlyDictionary<string, string> renamed) =>
        renamed.TryGetValue(stored, out var current) ? current : stored;

    /// <summary>Reads the browser entry: its edits, and a look kept there before the delegation, moved into the package state.</summary>
    private async Task RestoreAsync(string stored)
    {
        Dictionary<string, string>? saved;
        try
        {
            saved = JsonSerializer.Deserialize<Dictionary<string, string>>(stored);
        }
        catch (JsonException)
        {
            // A hand-edited or half-written entry must not take the site down; the visitor simply
            // starts again without edits.
            return;
        }

        if (saved is null)
        {
            return;
        }

        foreach (var (name, value) in saved.Where(entry => entry.Key.StartsWith("--", StringComparison.Ordinal)))
        {
            _edits[name] = value;
        }

        if (saved.Keys.Any(key => !key.StartsWith("--", StringComparison.Ordinal)))
        {
            _changing++;
            try
            {
                await MigrateAsync(saved).ConfigureAwait(false);
            }
            finally
            {
                _changing--;
            }
        }
    }

    /// <summary>
    /// Moves a look kept under the former entry into the package state, one setting at a time: a name the catalogue no
    /// longer has, an unknown value or a level out of 1 to 10 leaves that setting as the package state holds it. The
    /// push that follows writes the entry back with the edits alone, so the move happens once.
    /// </summary>
    private async Task MigrateAsync(Dictionary<string, string> saved)
    {
        // The theme first: a new theme drops the palette, the font and the backdrop chosen for another one.
        if (saved.TryGetValue(ThemeKey, out var themeName)
            && OmniThemePresets.All.FirstOrDefault(theme => theme.Name == LegacyName(themeName, RenamedThemes)) is { } theme)
        {
            await _appearance.SetPresetAsync(ReferenceEquals(theme, OmniThemePresets.All[0]) ? null : theme).ConfigureAwait(false);
        }

        if (saved.TryGetValue(PaletteKey, out var paletteName)
            && OmniThemePalettes.All.FirstOrDefault(palette => palette.Name == LegacyName(paletteName, RenamedPalettes)) is { } palette)
        {
            await _appearance.SetPaletteAsync(ReferenceEquals(palette, OmniThemePresets.DefaultPaletteFor(Theme)) ? null : palette).ConfigureAwait(false);
        }

        // The former showcase modes (Light, Dark, System) carry the names of OmniAppearance.
        if (saved.TryGetValue(ModeKey, out var mode) && Enum.TryParse<OmniAppearance>(mode, out var parsedMode) && Enum.IsDefined(parsedMode))
        {
            await _appearance.SetAppearanceAsync(parsedMode).ConfigureAwait(false);
        }

        if (saved.TryGetValue(DensityKey, out var density) && Enum.TryParse<OmniDensity>(density, out var parsedDensity) && Enum.IsDefined(parsedDensity))
        {
            await _appearance.SetDensityAsync(parsedDensity).ConfigureAwait(false);
        }

        if (saved.TryGetValue(MotionKey, out var motion) && bool.TryParse(motion, out var parsedMotion))
        {
            await _appearance.SetBackdropMotionAsync(parsedMotion).ConfigureAwait(false);
        }

        if (saved.TryGetValue(FontKey, out var fontName) && OmniThemeFonts.All.FirstOrDefault(font => font.Name == fontName) is { } font)
        {
            await _appearance.SetFontAsync(font).ConfigureAwait(false);
        }

        if (saved.TryGetValue(TextSizeKey, out var textSize) && int.TryParse(textSize, CultureInfo.InvariantCulture, out var parsedText) && parsedText is >= 1 and <= 10)
        {
            await _appearance.SetTextSizeLevelAsync(parsedText).ConfigureAwait(false);
        }

        if (saved.TryGetValue(ControlSizeKey, out var controlSize) && int.TryParse(controlSize, CultureInfo.InvariantCulture, out var parsedControl) && parsedControl is >= 1 and <= 10)
        {
            await _appearance.SetControlSizeLevelAsync(parsedControl).ConfigureAwait(false);
        }

        if (saved.TryGetValue(FullWidthKey, out var fullWidth) && bool.TryParse(fullWidth, out var parsedWidth))
        {
            await _appearance.SetFullWidthAsync(parsedWidth).ConfigureAwait(false);
        }

        // Empty or absent: the theme's own backdrop. A name the catalogue no longer has keeps it too.
        if (saved.TryGetValue(BackdropKey, out var backdropName)
            && OmniThemeBackdrops.All.FirstOrDefault(backdrop => backdrop.Name == backdropName) is { } backdrop
            && !ReferenceEquals(backdrop, OmniThemePresets.DefaultBackdropFor(Theme)))
        {
            await _appearance.SetBackdropAsync(backdrop).ConfigureAwait(false);
        }
    }

    private string Serialize() => JsonSerializer.Serialize(_edits);

    private async Task PushAsync(CancellationToken cancellationToken)
    {
        Painted = _painted with { Light = Light, Dark = Dark };
        if (_initialized)
        {
            await _js.InvokeVoidAsync(
                "omniShowcaseTheme.apply",
                cancellationToken,
                Light,
                Dark,
                EffectiveMode.ToString().ToLowerInvariant(),
                StorageKey,
                Serialize(),
                TextSizeLevel,
                ControlSizeLevel).ConfigureAwait(false);
        }

        Changed?.Invoke();
    }

    /// <summary>The theme, the palette and the backdrop the page was last painted with, to tell what a change replaced.</summary>
    private readonly record struct DrawnLook(OmniThemePreset Theme, OmniThemePalette Palette, OmniThemeBackdrop Backdrop);
}
