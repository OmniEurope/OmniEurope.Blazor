using OmniEurope.Blazor.Components;
using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Showcase.Theming;

/// <summary>
/// Holds the theme the visitor is building, a theme of the catalogue painted with a palette of the
/// catalogue plus the tokens edited by hand, and pushes it onto the live page.
/// </summary>
/// <remarks>
/// Values are written through the CSSOM rather than through a style attribute or an injected style
/// element: the site ships a strict policy that forbids both, and the CSSOM path is the one the
/// policy leaves open. Both halves are pushed, so the page follows the system setting as it changes
/// when the visitor previews the system mode.
/// </remarks>
public sealed class ThemeState(ThemeTokenReader reader, IJSRuntime js)
{
    private const string StorageKey = "omnieurope.showcase.theme";

    // Keys of the stored entry that are not tokens. Every token name starts with "--", so the two
    // kinds cannot collide, and an entry written before the theme and palette existed (a plain map
    // of edited tokens) still reads as the edits it was.
    private const string ThemeKey = "theme";
    private const string PaletteKey = "palette";
    private const string ModeKey = "mode";
    private const string DensityKey = "density";

    private readonly Dictionary<string, string> _edits = new(StringComparer.Ordinal);
    private IReadOnlyList<ThemeToken> _tokens = [];
    private OmniThemePreset _painted = OmniThemePresets.All[0];

    /// <summary>Raised whenever the theme changes, so open editors redraw.</summary>
    public event Action? Changed;

    /// <summary>The token catalogue read from the stylesheet.</summary>
    public IReadOnlyList<ThemeToken> Tokens => _tokens;

    /// <summary>The theme of the catalogue that gives the shape.</summary>
    public OmniThemePreset Theme { get; private set; } = OmniThemePresets.All[0];

    /// <summary>The palette of the catalogue that gives the colours.</summary>
    public OmniThemePalette Palette { get; private set; } = DefaultPaletteOf(OmniThemePresets.All[0]);

    /// <summary>Whether the palette is the one the theme comes with.</summary>
    public bool HasThemePalette => ReferenceEquals(Palette, DefaultPaletteOf(Theme));

    /// <summary>The mode the visitor is previewing.</summary>
    public ThemeMode Mode { get; private set; } = ThemeMode.Light;

    /// <summary>The density of the whole page, which a section with a density of its own overrides.</summary>
    public OmniDensity Density { get; private set; } = OmniDensity.Comfortable;

    /// <summary>The tokens the visitor edited by hand, laid over both halves of the combination.</summary>
    public IReadOnlyDictionary<string, string> Edits => _edits;

    /// <summary>The light half in force: the theme painted with the palette, then the edits.</summary>
    public IReadOnlyDictionary<string, string> Light => Overlay(_painted.Light);

    /// <summary>The dark half in force: the palette's dark colours, the shape, the theme's dark shape, then the edits.</summary>
    public IReadOnlyDictionary<string, string> Dark => Overlay(_painted.Dark);

    /// <summary>
    /// The palette a theme of the catalogue comes with. The public model carries no link from a theme
    /// to its palette, but a theme is its palette with the shape laid over it, so the palette that
    /// repaints the theme into itself is the one it was built with.
    /// </summary>
    public static OmniThemePalette DefaultPaletteOf(OmniThemePreset theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        foreach (var palette in OmniThemePalettes.All)
        {
            var painted = theme.With(palette);
            if (SameTokens(painted.Light, theme.Light) && SameTokens(painted.Dark, theme.Dark))
            {
                return palette;
            }
        }

        return OmniThemePalettes.All[0];
    }

    /// <summary>
    /// Loads the catalogue and replays whatever the visitor had left behind in this browser.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _tokens = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        var stored = await js.InvokeAsync<string?>("omniShowcaseTheme.load", cancellationToken, StorageKey)
            .ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(stored))
        {
            Restore(stored);
        }

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
    /// Picks a theme, painted with its own palette: a theme is chosen whole, and the palette picker
    /// then repaints it. The edits, made for the previous combination, are dropped.
    /// </summary>
    public async Task SelectThemeAsync(OmniThemePreset theme, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(theme);
        Theme = theme;
        Palette = DefaultPaletteOf(theme);
        _edits.Clear();
        Repaint();
        await PushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Repaints the theme with another palette; the theme and its shape stay.</summary>
    public async Task SelectPaletteAsync(OmniThemePalette palette, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(palette);
        Palette = palette;
        _edits.Clear();
        Repaint();
        await PushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Returns to the palette the theme comes with.</summary>
    public Task ResetPaletteAsync(CancellationToken cancellationToken = default) =>
        SelectPaletteAsync(DefaultPaletteOf(Theme), cancellationToken);

    /// <summary>Switches the mode previewed; both halves are already on the page.</summary>
    public async Task SetModeAsync(ThemeMode mode, CancellationToken cancellationToken = default)
    {
        Mode = mode;
        await PushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sets the density of the whole page.</summary>
    public async Task SetDensityAsync(OmniDensity density, CancellationToken cancellationToken = default)
    {
        Density = density;
        await PushAsync(cancellationToken).ConfigureAwait(false);
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
            builder.Append(", ").Append(_edits.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(" token(s) edited");
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

    private static bool SameTokens(IReadOnlyDictionary<string, string> first, IReadOnlyDictionary<string, string> second) =>
        first.Count == second.Count
        && first.All(entry => second.TryGetValue(entry.Key, out var value) && string.Equals(value, entry.Value, StringComparison.Ordinal));

    private string? CombinationValueOf(string name)
    {
        var half = Mode is ThemeMode.Dark ? _painted.Dark : _painted.Light;
        return _edits.TryGetValue(name, out var edited) ? edited : half.TryGetValue(name, out var value) ? value : null;
    }

    private Dictionary<string, string> Overlay(IReadOnlyDictionary<string, string> half)
    {
        var merged = new Dictionary<string, string>(half, StringComparer.Ordinal);
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

    private void Repaint() => _painted = Theme.With(Palette);

    private void Restore(string stored)
    {
        Dictionary<string, string>? saved;
        try
        {
            saved = JsonSerializer.Deserialize<Dictionary<string, string>>(stored);
        }
        catch (JsonException)
        {
            // A hand-edited or half-written entry must not take the site down; the visitor simply
            // starts again from the shipped theme.
            return;
        }

        if (saved is null)
        {
            return;
        }

        if (saved.TryGetValue(ThemeKey, out var themeName)
            && OmniThemePresets.All.FirstOrDefault(theme => theme.Name == (themeName == "Défaut" ? "Essentiel" : themeName)) is { } theme)
        {
            Theme = theme;
            Palette = DefaultPaletteOf(theme);
        }

        if (saved.TryGetValue(PaletteKey, out var paletteName)
            && OmniThemePalettes.All.FirstOrDefault(palette => palette.Name == paletteName) is { } palette)
        {
            Palette = palette;
        }

        if (saved.TryGetValue(ModeKey, out var mode) && Enum.TryParse<ThemeMode>(mode, out var parsedMode) && Enum.IsDefined(parsedMode))
        {
            Mode = parsedMode;
        }

        if (saved.TryGetValue(DensityKey, out var density) && Enum.TryParse<OmniDensity>(density, out var parsedDensity) && Enum.IsDefined(parsedDensity))
        {
            Density = parsedDensity;
        }

        foreach (var (name, value) in saved.Where(entry => entry.Key.StartsWith("--", StringComparison.Ordinal)))
        {
            _edits[name] = value;
        }

        Repaint();
    }

    private string Serialize()
    {
        var saved = new Dictionary<string, string>(_edits, StringComparer.Ordinal)
        {
            [ThemeKey] = Theme.Name,
            [PaletteKey] = Palette.Name,
            [ModeKey] = Mode.ToString(),
            [DensityKey] = Density.ToString()
        };
        return JsonSerializer.Serialize(saved);
    }

    private async Task PushAsync(CancellationToken cancellationToken)
    {
        await js.InvokeVoidAsync(
            "omniShowcaseTheme.apply",
            cancellationToken,
            Light,
            Dark,
            Mode.ToString().ToLowerInvariant(),
            StorageKey,
            Serialize()).ConfigureAwait(false);
        Changed?.Invoke();
    }
}
