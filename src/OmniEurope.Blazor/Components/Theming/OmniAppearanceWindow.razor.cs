namespace OmniEurope.Blazor.Components;

/// <summary>
/// The small draggable window of the look, modeless so the page stays in view: theme, palette and font,
/// then the text size, the density and the size of the controls. Controlled like
/// <see cref="OmniAppearanceSettings"/> (the host keeps and applies every value), and each row only
/// shows once its change is bound. A host opens it from a menu entry ("Theme") through
/// <see cref="Open"/>; <see cref="OmniAppearanceSettings"/> opens the same window from its look row.
/// </summary>
/// <remarks>
/// A new theme comes with its own palette and font: picking one here raises <see cref="PresetChanged"/>,
/// then <see cref="PaletteChanged"/> and <see cref="FontChanged"/> with null for a palette or a font
/// chosen for the previous theme (when they are bound), exactly as <see cref="OmniAppearanceSettings"/>
/// does. The host only stores what it receives.
/// </remarks>
public partial class OmniAppearanceWindow
{
    private readonly string _idPrefix = $"omni-appearance-{Guid.NewGuid():N}";

    /// <summary>Whether the window is shown.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Raised when the window closes (its close button, Escape).</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Title of the window; the localized "Appearance" when null or blank.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>The chosen theme, or null for the first one of the catalogue.</summary>
    [Parameter]
    public OmniThemePreset? Preset { get; set; }

    /// <summary>
    /// Raised with the theme picked, null for the default one; bound, it shows the theme row. The
    /// palette and the font picked for the previous theme are then dropped (see the remarks).
    /// </summary>
    [Parameter]
    public EventCallback<OmniThemePreset?> PresetChanged { get; set; }

    /// <summary>The chosen palette, or null for the theme's own.</summary>
    [Parameter]
    public OmniThemePalette? Palette { get; set; }

    /// <summary>Raised with the palette picked, null for the theme's own; bound, it shows the palette row.</summary>
    [Parameter]
    public EventCallback<OmniThemePalette?> PaletteChanged { get; set; }

    /// <summary>The chosen font, or null for the one the theme is drawn with.</summary>
    [Parameter]
    public OmniThemeFont? Font { get; set; }

    /// <summary>Raised with the font picked, null for the theme's own; bound, it shows the font row.</summary>
    [Parameter]
    public EventCallback<OmniThemeFont?> FontChanged { get; set; }

    /// <summary>
    /// Whether the theme may move its colour field; the host applies it through
    /// <see cref="OmniThemeScope.BackdropMotion"/>. True by default.
    /// </summary>
    [Parameter]
    public bool BackdropMotion { get; set; } = true;

    /// <summary>
    /// Raised with the choice; bound, it shows the row under a theme that moves its field (Givre,
    /// Trou noir), and under no other: the setting would do nothing there.
    /// </summary>
    [Parameter]
    public EventCallback<bool> BackdropMotionChanged { get; set; }

    /// <summary>Text size, 1 to 10 with 5 as drawn (the host applies it, <c>data-oe-text-size</c>).</summary>
    [Parameter]
    public int TextSizeLevel { get; set; } = 5;

    /// <summary>Raised with the new text size; bound, it shows the text size row.</summary>
    [Parameter]
    public EventCallback<int> TextSizeLevelChanged { get; set; }

    /// <summary>
    /// The density of the page (control heights, paddings, gaps), one of three; the host applies it,
    /// for example through <see cref="OmniThemeScope.Density"/>. <see cref="OmniDensity.Comfortable"/> by default.
    /// </summary>
    [Parameter]
    public OmniDensity Density { get; set; } = OmniDensity.Comfortable;

    /// <summary>Raised with the density picked; bound, it shows the density row.</summary>
    [Parameter]
    public EventCallback<OmniDensity> DensityChanged { get; set; }

    /// <summary>Size of the controls, 1 to 10 with 5 as drawn (<c>data-oe-control-size</c>).</summary>
    [Parameter]
    public int ControlSizeLevel { get; set; } = 5;

    /// <summary>Raised with the new size of the controls; bound, it shows that row.</summary>
    [Parameter]
    public EventCallback<int> ControlSizeLevelChanged { get; set; }

    private string EffectiveTitle => LocalizeOr(Title, "AppearanceWindowTitle");

    private bool ShowsBackdropMotion => BackdropMotionChanged.HasDelegate && AppearanceChoices.MovesBackdrop(Preset);

    private bool ShowsRows => PresetChanged.HasDelegate || PaletteChanged.HasDelegate || FontChanged.HasDelegate || ShowsBackdropMotion
        || TextSizeLevelChanged.HasDelegate || DensityChanged.HasDelegate || ControlSizeLevelChanged.HasDelegate;

    private string ThemeName => AppearanceChoices.ThemeName(Preset);

    private string PaletteName => AppearanceChoices.PaletteName(Preset, Palette);

    private string FontName => AppearanceChoices.FontName(Preset, Font);

    private IReadOnlyList<OmniOption<string>> ThemeOptions => AppearanceChoices.ThemeOptions(Localize("SettingsDefaultSuffix"));

    private IReadOnlyList<OmniOption<string>> PaletteOptions => AppearanceChoices.PaletteOptions(Preset, Localize("SettingsDefaultSuffix"));

    private IReadOnlyList<OmniOption<string>> FontOptions => AppearanceChoices.FontOptions(Preset, Localize("SettingsDefaultSuffix"));

    private IReadOnlyList<OmniOption<OmniDensity>> DensityOptions =>
        [.. Enum.GetValues<OmniDensity>().Select(density => new OmniOption<OmniDensity>(density, Localize(DensityKey(density))))];

    private double TextSizeValue => TextSizeLevel;

    private double ControlSizeValue => ControlSizeLevel;

    private ScaleSetting TextSizeScale => new(
        "text-size", OmniIconName.TextSize, "SettingsTextSize", "SettingsTextSizeDecrease", "SettingsTextSizeIncrease",
        TextSizeLevel, TextSizeLevelChanged, () => TextSizeValue);

    private ScaleSetting ControlSizeScale => new(
        "control-size", OmniIconName.CornersOut, "SettingsControlSize", "SettingsControlSizeDecrease", "SettingsControlSizeIncrease",
        ControlSizeLevel, ControlSizeLevelChanged, () => ControlSizeValue);

    /// <summary>The resource key of the name of a density.</summary>
    internal static string DensityKey(OmniDensity density) => density switch
    {
        OmniDensity.Compact => "DensityCompact",
        OmniDensity.Spacious => "DensitySpacious",
        _ => "DensityComfortable"
    };

    private string RowId(string row) => $"{_idPrefix}-{row}";

    private Task SetThemeAsync(string? name) => ChangeThemeAsync(AppearanceChoices.Theme(name));

    private Task ResetThemeAsync() => ChangeThemeAsync(null);

    /// <summary>The new theme, then null for the palette and the font chosen for the previous one.</summary>
    private async Task ChangeThemeAsync(OmniThemePreset? preset)
    {
        await PresetChanged.InvokeAsync(preset);
        if (Palette is not null && PaletteChanged.HasDelegate)
        {
            await PaletteChanged.InvokeAsync(null);
        }

        if (Font is not null && FontChanged.HasDelegate)
        {
            await FontChanged.InvokeAsync(null);
        }
    }

    /// <summary>
    /// A theme drawn at random, other than the current one, and with it a palette other than the one in
    /// force when the palette is bound: any palette paints any theme. The font chosen for the previous
    /// theme is dropped, as on any new theme.
    /// </summary>
    private async Task RandomLookAsync()
    {
        var theme = AppearanceChoices.RandomOther(OmniThemePresets.All, AppearanceChoices.EffectivePreset(Preset));
        var preset = AppearanceChoices.Theme(AppearanceChoices.ThemeName(theme));
        var inForce = PaletteName;
        await PresetChanged.InvokeAsync(preset);
        if (PaletteChanged.HasDelegate)
        {
            var palette = AppearanceChoices.RandomOther(OmniThemePalettes.All, OmniThemePalettes.All.FirstOrDefault(item => item.Name == inForce));
            await PaletteChanged.InvokeAsync(AppearanceChoices.Palette(preset, palette.Name));
        }

        if (Font is not null && FontChanged.HasDelegate)
        {
            await FontChanged.InvokeAsync(null);
        }
    }

    private Task SetPaletteAsync(string? name) => PaletteChanged.InvokeAsync(AppearanceChoices.Palette(Preset, name));

    private Task ResetPaletteAsync() => PaletteChanged.InvokeAsync(null);

    private Task SetFontAsync(string? name) => FontChanged.InvokeAsync(AppearanceChoices.Font(Preset, name));

    private Task ResetFontAsync() => FontChanged.InvokeAsync(null);

    private Task SetBackdropMotionAsync(bool moves) => BackdropMotionChanged.InvokeAsync(moves);

    private Task SetDensityAsync(OmniDensity density) => DensityChanged.InvokeAsync(density);

    private static Task ChangeLevelAsync(ScaleSetting setting, int level) => setting.Changed.InvokeAsync(Math.Clamp(level, 1, 10));

    /// <summary>
    /// One scale row, the same for the text size and the size of the controls: its label, then a group
    /// named by it (reduce, the level, enlarge, back to the default) described by the level, then a
    /// slider over the same 1 to 10.
    /// </summary>
    private RenderFragment ScaleRow(ScaleSetting setting) => builder =>
    {
        var labelId = RowId(setting.Key);
        var levelId = $"{labelId}-level";
        var name = Localize(setting.NameKey);

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "omni-appearance-settings__row");

        builder.OpenElement(2, "span");
        builder.AddAttribute(3, "class", "omni-appearance-settings__label");
        builder.AddAttribute(4, "id", labelId);
        builder.OpenComponent<OmniIcon>(5);
        builder.AddComponentParameter(6, nameof(OmniIcon.Name), setting.Icon);
        builder.CloseComponent();
        builder.AddContent(7, " " + name);
        builder.CloseElement();

        builder.OpenElement(8, "div");
        builder.AddAttribute(9, "class", "omni-appearance-settings__actions");
        builder.AddAttribute(10, "role", "group");
        builder.AddAttribute(11, "aria-labelledby", labelId);
        builder.AddAttribute(12, "aria-describedby", levelId);

        builder.OpenComponent<OmniButton>(13);
        builder.AddComponentParameter(14, nameof(OmniButton.Variant), OmniButtonVariant.Secondary);
        builder.AddComponentParameter(15, nameof(OmniButton.Disabled), setting.Level <= 1);
        builder.AddComponentParameter(16, nameof(OmniButton.Label), Localize(setting.DecreaseKey));
        builder.AddComponentParameter(17, nameof(OmniButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, () => ChangeLevelAsync(setting, setting.Level - 1)));
        builder.AddComponentParameter(18, nameof(OmniButton.ChildContent), Icon(OmniIconName.Remove));
        builder.CloseComponent();

        builder.OpenComponent<OmniBadge>(19);
        builder.AddComponentParameter(20, nameof(OmniBadge.Id), levelId);
        builder.AddComponentParameter(21, nameof(OmniBadge.Tone), OmniTone.Accent);
        builder.AddComponentParameter(22, nameof(OmniBadge.Fill), OmniFill.Solid);
        builder.AddComponentParameter(23, nameof(OmniBadge.ChildContent), (RenderFragment)(text => text.AddContent(0, Localize("SettingsLevel", setting.Level))));
        builder.CloseComponent();

        builder.OpenComponent<OmniButton>(24);
        builder.AddComponentParameter(25, nameof(OmniButton.Variant), OmniButtonVariant.Secondary);
        builder.AddComponentParameter(26, nameof(OmniButton.Disabled), setting.Level >= 10);
        builder.AddComponentParameter(27, nameof(OmniButton.Label), Localize(setting.IncreaseKey));
        builder.AddComponentParameter(28, nameof(OmniButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, () => ChangeLevelAsync(setting, setting.Level + 1)));
        builder.AddComponentParameter(29, nameof(OmniButton.ChildContent), Icon(OmniIconName.Add));
        builder.CloseComponent();

        builder.OpenComponent<OmniButton>(30);
        builder.AddComponentParameter(31, nameof(OmniButton.Variant), OmniButtonVariant.Secondary);
        builder.AddComponentParameter(32, nameof(OmniButton.Disabled), setting.Level == 5);
        builder.AddComponentParameter(33, nameof(OmniButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, () => ChangeLevelAsync(setting, 5)));
        builder.AddComponentParameter(34, nameof(OmniButton.ChildContent), (RenderFragment)(content =>
        {
            content.OpenComponent<OmniIcon>(0);
            content.AddComponentParameter(1, nameof(OmniIcon.Name), OmniIconName.Reset);
            content.CloseComponent();
            content.OpenElement(2, "span");
            content.AddContent(3, Localize("SettingsDefault"));
            content.CloseElement();
        }));
        builder.CloseComponent();
        builder.CloseElement();

        builder.OpenComponent<OmniSlider>(35);
        builder.AddComponentParameter(36, nameof(OmniSlider.Minimum), 1d);
        builder.AddComponentParameter(37, nameof(OmniSlider.Maximum), 10d);
        builder.AddComponentParameter(38, nameof(OmniSlider.Step), 1d);
        builder.AddComponentParameter(39, nameof(OmniSlider.ShowValue), false);
        builder.AddComponentParameter(40, nameof(OmniSlider.Value), (double)setting.Level);
        builder.AddComponentParameter(41, nameof(OmniSlider.ValueChanged), EventCallback.Factory.Create<double>(this, value => ChangeLevelAsync(setting, (int)value)));
        builder.AddComponentParameter(42, nameof(OmniSlider.ValueExpression), setting.ValueExpression);
        builder.AddComponentParameter(43, "aria-label", name);
        builder.CloseComponent();

        builder.CloseElement();
    };

    private static RenderFragment Icon(OmniIconName icon) => builder =>
    {
        builder.OpenComponent<OmniIcon>(0);
        builder.AddComponentParameter(1, nameof(OmniIcon.Name), icon);
        builder.CloseComponent();
    };

    /// <summary>What differs between the scale rows: ids, icon, texts, level, change and the slider's binding.</summary>
    private sealed record ScaleSetting(
        string Key,
        OmniIconName Icon,
        string NameKey,
        string DecreaseKey,
        string IncreaseKey,
        int Level,
        EventCallback<int> Changed,
        Expression<Func<double>> ValueExpression);
}
