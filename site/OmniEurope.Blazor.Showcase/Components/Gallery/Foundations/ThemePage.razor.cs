using System.Globalization;
using OmniEurope.Blazor.Showcase.Resources.Gallery;
using OmniEurope.Blazor.Showcase.Theming;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Foundations;

/// <summary>
/// The Theme page: the appearance of the whole showcase, bound to its own <see cref="ThemeState"/>, and
/// what a theme scope does to a sample, nested so the look of the page stays the visitor's.
/// </summary>
public partial class ThemePage : IDisposable
{
    private static readonly OmniAppearance[] AllAppearances = Enum.GetValues<OmniAppearance>();

    private static readonly OmniDensity[] AllDensities = Enum.GetValues<OmniDensity>();

    [Inject]
    private IStringLocalizer<FoundationsStrings> Text { get; set; } = default!;

    [Inject]
    private ThemeState Look { get; set; } = default!;

    private OmniThemeFont? PreviewFont { get; set; }

    private bool PreviewFullWidth { get; set; }

    private bool WindowOpen { get; set; }

    /// <summary>The appearance of the nested preview scope, shared by the bar and by the toggle button.</summary>
    private OmniAppearance ScopeAppearance { get; set; } = OmniAppearance.Dark;

    private OmniDensity ScopeDensity { get; set; } = OmniDensity.Comfortable;

    /// <summary>The mode the showcase is drawn in, as the appearance the settings and the tiles speak.</summary>
    private OmniAppearance ModuleAppearance => Look.EffectiveMode switch
    {
        ThemeMode.Dark => OmniAppearance.Dark,
        ThemeMode.System => OmniAppearance.System,
        _ => OmniAppearance.Light
    };

    /// <summary>The palette as the settings expect it: null while it is the palette the theme comes with.</summary>
    private OmniThemePalette? PaletteSetting => Look.HasThemePalette ? null : Look.Palette;

    /// <summary>The icon of the appearance the toggle button switches to.</summary>
    private OmniIconName ScopeToggleIcon => ScopeAppearance == OmniAppearance.Dark ? OmniIconName.ThemeLight : OmniIconName.ThemeDark;

    private string CombinationCount => string.Format(
        CultureInfo.CurrentCulture,
        Text["ThemeCombinationsCount"].Value,
        OmniThemePresets.All.Count,
        OmniThemePalettes.All.Count,
        OmniThemePresets.All.Count * OmniThemePalettes.All.Count);

    private IReadOnlyList<OmniOption<OmniAppearance>> AppearanceOptions =>
        [.. AllAppearances.Select(appearance => new OmniOption<OmniAppearance>(appearance, AppearanceLabel(appearance)))];

    private IReadOnlyList<OmniOption<OmniDensity>> DensityOptions =>
        [.. AllDensities.Select(density => new OmniOption<OmniDensity>(density, DensityLabel(density)))];

    protected override void OnInitialized() => Look.Changed += OnLookChanged;

    public void Dispose()
    {
        Look.Changed -= OnLookChanged;
        GC.SuppressFinalize(this);
    }

    private void OnLookChanged() => InvokeAsync(StateHasChanged);

    private Task SetAppearanceAsync(OmniAppearance appearance) => Look.SetModeAsync(appearance switch
    {
        OmniAppearance.Dark => ThemeMode.Dark,
        OmniAppearance.System => ThemeMode.System,
        _ => ThemeMode.Light
    });

    /// <summary>A theme picked in the settings or in the window; null is the first of the catalogue.</summary>
    private Task SetPresetAsync(OmniThemePreset? preset) => SelectPresetAsync(preset ?? OmniThemePresets.All[0]);

    private Task SelectPresetAsync(OmniThemePreset preset) => Look.SelectThemeAsync(preset);

    /// <summary>A palette picked in the settings or in the window; null returns to the palette of the theme.</summary>
    private Task SetPaletteAsync(OmniThemePalette? palette) =>
        palette is null ? Look.ResetPaletteAsync() : SelectPaletteAsync(palette);

    private Task SelectPaletteAsync(OmniThemePalette palette) => Look.SelectPaletteAsync(palette);

    private Task SetDensityAsync(OmniDensity density) => Look.SetDensityAsync(density);

    private Task SetBackdropMotionAsync(bool moves) => Look.SetBackdropMotionAsync(moves);

    private void SetFont(OmniThemeFont? font) => PreviewFont = font;

    private void SetFullWidth(bool fullWidth) => PreviewFullWidth = fullWidth;

    /// <summary>A named button that switches the preview scope between light and dark.</summary>
    private void ToggleScopeAppearance() =>
        ScopeAppearance = ScopeAppearance == OmniAppearance.Dark ? OmniAppearance.Light : OmniAppearance.Dark;

    private string AppearanceLabel(OmniAppearance appearance) => appearance switch
    {
        OmniAppearance.Light => Text["ThemeAppearanceLight"],
        OmniAppearance.Dark => Text["ThemeAppearanceDark"],
        _ => Text["ThemeAppearanceSystem"]
    };

    private string DensityLabel(OmniDensity density) => density switch
    {
        OmniDensity.Compact => Text["ThemeDensityCompact"],
        OmniDensity.Spacious => Text["ThemeDensitySpacious"],
        _ => Text["ThemeDensityComfortable"]
    };
}
