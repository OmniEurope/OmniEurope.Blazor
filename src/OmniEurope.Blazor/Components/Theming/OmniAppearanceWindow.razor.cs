namespace OmniEurope.Blazor.Components;

/// <summary>
/// The small draggable window of the look, modeless so the page stays in view: theme and palette, then
/// the text size, the density and the size of the controls. Controlled like
/// <see cref="OmniAppearanceSettings"/> (the host keeps and applies every value), and each row only
/// shows once its change is bound. A host opens it from a menu entry ("Theme") through
/// <see cref="Open"/>; <see cref="OmniAppearanceSettings"/> opens the same window from its scale row.
/// </summary>
public partial class OmniAppearanceWindow
{
    /// <summary>Whether the window is shown.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Raised when the window closes (its close button, Escape).</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Title of the window; the localized "Appearance" when empty.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>The chosen theme, or null for the first one of the catalogue.</summary>
    [Parameter]
    public OmniThemePreset? Preset { get; set; }

    /// <summary>
    /// Raised with the theme picked, null for the default one; bound, it shows the theme row. A new
    /// theme comes with its own palette and font: the host drops the ones it keeps for the previous
    /// theme, as <see cref="OmniAppearanceSettings"/> does.
    /// </summary>
    [Parameter]
    public EventCallback<OmniThemePreset?> PresetChanged { get; set; }

    /// <summary>The chosen palette, or null for the theme's own.</summary>
    [Parameter]
    public OmniThemePalette? Palette { get; set; }

    /// <summary>Raised with the palette picked, null for the theme's own; bound, it shows the palette row.</summary>
    [Parameter]
    public EventCallback<OmniThemePalette?> PaletteChanged { get; set; }

    /// <summary>Text size, 1 to 10 with 5 as drawn (the host applies it, <c>data-oe-text-size</c>).</summary>
    [Parameter]
    public int TextSizeLevel { get; set; } = 5;

    /// <summary>Raised with the new text size; bound, it shows the text size row.</summary>
    [Parameter]
    public EventCallback<int> TextSizeLevelChanged { get; set; }

    /// <summary>Density, 1 to 10 with 5 as drawn.</summary>
    [Parameter]
    public int DensityLevel { get; set; } = 5;

    /// <summary>Raised with the new density; bound, it shows the density row.</summary>
    [Parameter]
    public EventCallback<int> DensityLevelChanged { get; set; }

    /// <summary>Size of the controls, 1 to 10 with 5 as drawn (<c>data-oe-control-size</c>).</summary>
    [Parameter]
    public int ControlSizeLevel { get; set; } = 5;

    /// <summary>Raised with the new size of the controls; bound, it shows that row.</summary>
    [Parameter]
    public EventCallback<int> ControlSizeLevelChanged { get; set; }

    private string EffectiveTitle => string.IsNullOrWhiteSpace(Title) ? Localize("AppearanceWindowTitle") : Title;

    private bool ShowsLook => PresetChanged.HasDelegate || PaletteChanged.HasDelegate;

    private bool ShowsScale => TextSizeLevelChanged.HasDelegate || DensityLevelChanged.HasDelegate || ControlSizeLevelChanged.HasDelegate;

    private string ThemeName => AppearanceChoices.ThemeName(Preset);

    private string PaletteName => AppearanceChoices.PaletteName(Preset, Palette);

    private IReadOnlyList<OmniOption<string>> ThemeOptions => AppearanceChoices.ThemeOptions(Localize("SettingsDefaultSuffix"));

    private IReadOnlyList<OmniOption<string>> PaletteOptions => AppearanceChoices.PaletteOptions(Preset, Localize("SettingsDefaultSuffix"));

    private double TextSizeValue => TextSizeLevel;

    private double DensityValue => DensityLevel;

    private double ControlSizeValue => ControlSizeLevel;

    private Task SetThemeAsync(string? name) => PresetChanged.InvokeAsync(AppearanceChoices.Theme(name));

    private Task SetPaletteAsync(string? name) => PaletteChanged.InvokeAsync(AppearanceChoices.Palette(Preset, name));

    private Task OnTextSizeSlider(double value) => TextSizeLevelChanged.InvokeAsync((int)value);

    private Task OnDensitySlider(double value) => DensityLevelChanged.InvokeAsync((int)value);

    private Task OnControlSizeSlider(double value) => ControlSizeLevelChanged.InvokeAsync((int)value);
}
