namespace OmniEurope.Blazor.Components;

/// <summary>
/// Controlled appearance picker: the mode (light, dark, system) inline, then a row that opens the
/// <see cref="OmniAppearanceWindow"/> for the theme, the palette with the mode, the font and the scales. The host
/// keeps every value and applies it to its theme scope; this component only raises the changes.
/// </summary>
/// <remarks>
/// A new theme comes with its own palette and font: picking one raises <see cref="PresetChanged"/>, then
/// <see cref="PaletteChanged"/> and <see cref="FontChanged"/> with null for a palette or a font chosen
/// for the previous theme, as <see cref="OmniAppearanceWindow"/> does.
/// </remarks>
public partial class OmniAppearanceSettings
{
    /// <summary>True under a theme drawn in dark mode only: the mode is shown as dark and cannot change.</summary>
    private bool DarkOnly => Preset?.DarkOnly == true;

    private OmniAppearance ShownAppearance => DarkOnly ? OmniAppearance.Dark : Appearance;

    private readonly string _idPrefix = $"omni-appearance-settings-{Guid.NewGuid():N}";
    private bool _windowOpen;
    private bool? _windowOpenParameter;

    /// <summary>Lays the settings out for a narrow place, a menu: the look row shows a summary of the values.</summary>
    [Parameter] public bool Compact { get; set; }

    /// <summary>The mode; <see cref="OmniAppearance.System"/> by default.</summary>
    [Parameter] public OmniAppearance Appearance { get; set; } = OmniAppearance.System;

    /// <summary>Raised with the mode picked.</summary>
    [Parameter] public EventCallback<OmniAppearance> AppearanceChanged { get; set; }

    /// <summary>The chosen theme, or null for the first one of the catalogue.</summary>
    [Parameter] public OmniThemePreset? Preset { get; set; }

    /// <summary>Raised with the theme picked in the window, null for the default one.</summary>
    [Parameter] public EventCallback<OmniThemePreset?> PresetChanged { get; set; }

    /// <summary>The chosen palette, or null for the theme's own.</summary>
    [Parameter] public OmniThemePalette? Palette { get; set; }

    /// <summary>Raised with the palette picked in the window, null for the theme's own.</summary>
    [Parameter] public EventCallback<OmniThemePalette?> PaletteChanged { get; set; }

    /// <summary>The chosen font, or null for the one the theme is drawn with.</summary>
    [Parameter] public OmniThemeFont? Font { get; set; }

    /// <summary>Raised with the font picked in the window, null for the theme's own; bound, the window shows the font row.</summary>
    [Parameter] public EventCallback<OmniThemeFont?> FontChanged { get; set; }

    /// <summary>
    /// Whether the theme may move its colour field; the host applies it through
    /// <see cref="OmniThemeScope.BackdropMotion"/>. True by default.
    /// </summary>
    [Parameter] public bool BackdropMotion { get; set; } = true;

    /// <summary>Raised with the choice; bound, the window shows the row under a theme that moves its field.</summary>
    [Parameter] public EventCallback<bool> BackdropMotionChanged { get; set; }

    /// <summary>Text size, 1 to 10 with 5 as drawn (the host applies it, <c>data-oe-text-size</c>).</summary>
    [Parameter] public int TextSizeLevel { get; set; } = 5;

    /// <summary>Raised with the new text size; bound, the window shows the text size row.</summary>
    [Parameter] public EventCallback<int> TextSizeLevelChanged { get; set; }

    /// <summary>
    /// The density of the page, one of three; the host applies it, for example through
    /// <see cref="OmniThemeScope.Density"/>. <see cref="OmniDensity.Comfortable"/> by default.
    /// </summary>
    [Parameter] public OmniDensity Density { get; set; } = OmniDensity.Comfortable;

    /// <summary>Raised with the density picked; bound, the window shows the density row.</summary>
    [Parameter] public EventCallback<OmniDensity> DensityChanged { get; set; }

    /// <summary>
    /// Size of the controls (buttons, fields, lists), 1 to 10 with 5 as drawn. The host applies it, for
    /// example through <c>data-oe-control-size</c> on the document root, which the control tokens read.
    /// The setting only shows once <see cref="ControlSizeLevelChanged"/> is bound, so a host that does not
    /// apply it never offers a control that does nothing.
    /// </summary>
    [Parameter] public int ControlSizeLevel { get; set; } = 5;

    /// <summary>Raised with the new size of the controls; bound, the window shows that row.</summary>
    [Parameter] public EventCallback<int> ControlSizeLevelChanged { get; set; }

    /// <summary>
    /// Whether the appearance window is open. The component opens it from its look row and closes it
    /// with the window; a host may open or close it too, and bind it both ways.
    /// </summary>
    [Parameter] public bool WindowOpen { get; set; }

    /// <summary>
    /// Raised when the window opens or closes, so an enclosing menu can release its outside-click shield
    /// while the window is on screen.
    /// </summary>
    [Parameter] public EventCallback<bool> WindowOpenChanged { get; set; }

    private bool ShowsControlSize => ControlSizeLevelChanged.HasDelegate;

    private string LookTitle => Localize(Compact ? "SettingsLook" : "SettingsLookTitle");

    private string LookSummary
    {
        get
        {
            var theme = AppearanceChoices.EffectivePreset(Preset).Name;
            var density = Localize(OmniAppearanceWindow.DensityKey(Density));
            return ShowsControlSize
                ? Localize("SettingsLookSummaryFull", theme, TextSizeLevel, density, ControlSizeLevel)
                : Localize("SettingsLookSummary", theme, TextSizeLevel, density);
        }
    }

    private IReadOnlyList<OmniOption<OmniAppearance>> ModeOptions =>
    [
        new(OmniAppearance.Light, Localize("AppearanceLight")),
        new(OmniAppearance.Dark, Localize("AppearanceDark")),
        new(OmniAppearance.System, Localize("AppearanceSystem"))
    ];

    /// <summary>The window gets the control size change only when the host binds it, as it shows the row only then.</summary>
    private EventCallback<int> WindowControlSizeChanged => ShowsControlSize
        ? EventCallback.Factory.Create<int>(this, ChangeControlSizeAsync)
        : default;

    /// <summary>The window gets the mode change only when the host binds it, as it shows the mode only then.</summary>
    private EventCallback<OmniAppearance> WindowAppearanceChanged => AppearanceChanged.HasDelegate
        ? EventCallback.Factory.Create<OmniAppearance>(this, SetAppearanceAsync)
        : default;

    /// <summary>The window gets the font change only when the host binds it, as it shows the row only then.</summary>
    private EventCallback<OmniThemeFont?> WindowFontChanged => FontChanged.HasDelegate
        ? EventCallback.Factory.Create<OmniThemeFont?>(this, ChangeFontAsync)
        : default;

    /// <summary>The window gets the field motion change only when the host binds it, as it shows the row only then.</summary>
    private EventCallback<bool> WindowBackdropMotionChanged => BackdropMotionChanged.HasDelegate
        ? EventCallback.Factory.Create<bool>(this, ChangeBackdropMotionAsync)
        : default;

    private static OmniIconName? ModeIcon(OmniAppearance mode) => mode switch
    {
        OmniAppearance.Light => OmniIconName.ThemeLight,
        OmniAppearance.Dark => OmniIconName.ThemeDark,
        _ => OmniIconName.ThemeSystem
    };

    private string RowId(string row) => $"{_idPrefix}-{row}";

    /// <summary>Adopts the open state the host sets, whenever it changes it.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (_windowOpenParameter != WindowOpen)
        {
            _windowOpenParameter = WindowOpen;
            _windowOpen = WindowOpen;
        }
    }

    private Task SetAppearanceAsync(OmniAppearance mode) => AppearanceChanged.InvokeAsync(mode);

    private Task OpenWindowAsync() => SetWindowOpenAsync(true);

    private async Task SetWindowOpenAsync(bool open)
    {
        _windowOpen = open;
        await WindowOpenChanged.InvokeAsync(open);
    }

    /// <summary>
    /// The theme picked in the window, which then drops the palette and the font chosen for the
    /// previous theme itself, both being bound in it.
    /// </summary>
    private Task ChangeThemeAsync(OmniThemePreset? preset) => PresetChanged.InvokeAsync(preset);

    private Task ChangePaletteAsync(OmniThemePalette? palette) => PaletteChanged.InvokeAsync(palette);

    private Task ChangeFontAsync(OmniThemeFont? font) => FontChanged.InvokeAsync(font);

    private Task ChangeBackdropMotionAsync(bool moves) => BackdropMotionChanged.InvokeAsync(moves);

    private Task ChangeTextSizeAsync(int level) => TextSizeLevelChanged.InvokeAsync(level);

    private Task ChangeDensityAsync(OmniDensity density) => DensityChanged.InvokeAsync(density);

    private Task ChangeControlSizeAsync(int level) => ControlSizeLevelChanged.InvokeAsync(level);
}
