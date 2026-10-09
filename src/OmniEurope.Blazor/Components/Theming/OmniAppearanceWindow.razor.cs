namespace OmniEurope.Blazor.Components;

/// <summary>
/// The small draggable window of the look, modeless so the page stays in view: theme, palette and mode,
/// font and density, then the text size, the size of the controls and the width of the content. Controlled like
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

    /// <summary>
    /// Raised when the window closes: Apply, the close button and Escape keep the look tried; Restore
    /// first raises the changes that put back the look the window opened on.
    /// </summary>
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

    /// <summary>The light, dark or system mode, shown fixed to dark while the theme is drawn in dark only.</summary>
    [Parameter]
    public OmniAppearance Appearance { get; set; } = OmniAppearance.System;

    /// <summary>
    /// Raised with the mode picked; bound, the mode shows as three icon buttons joined beside the palette
    /// (review point 92), or on a row of its own when the palette is not bound.
    /// </summary>
    [Parameter]
    public EventCallback<OmniAppearance> AppearanceChanged { get; set; }

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

    /// <summary>
    /// Whether the page content takes the whole width of the window rather than the centred column the
    /// host draws it in. The host applies it, for example by passing <see cref="OmniLayoutWidth.Full"/>
    /// instead of its own width to <see cref="OmniMain.ContentWidth"/>. False (centred) by default.
    /// </summary>
    [Parameter]
    public bool FullWidth { get; set; }

    /// <summary>Raised with the width picked; bound, it shows the content width row.</summary>
    [Parameter]
    public EventCallback<bool> FullWidthChanged { get; set; }

    /// <summary>Size of the controls, 1 to 10 with 5 as drawn (<c>data-oe-control-size</c>).</summary>
    [Parameter]
    public int ControlSizeLevel { get; set; } = 5;

    /// <summary>Raised with the new size of the controls; bound, it shows that row.</summary>
    [Parameter]
    public EventCallback<int> ControlSizeLevelChanged { get; set; }

    /// <summary>The look the window opened on, put back by Restore.</summary>
    private Look? _openedOn;

    private bool _wasOpen;

    /// <summary>
    /// The look as the host passed it: what <see cref="ResetAllAsync"/> compares with the defaults and
    /// what the window keeps when it opens.
    /// </summary>
    private Look Current => new(Appearance, Preset, Palette, Font, BackdropMotion, TextSizeLevel, Density, ControlSizeLevel, FullWidth);

    private bool IsDefaultLook => Current == Look.Default;

    private string EffectiveTitle => LocalizeOr(Title, "AppearanceWindowTitle");

    /// <summary>Keeps the look in force each time the window opens, for Cancel to put it back.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Open && !_wasOpen)
        {
            _openedOn = Current;
        }

        _wasOpen = Open;
    }

    /// <summary>
    /// The close button and Escape only close the window, keeping the look tried as Apply does (owner
    /// decision of 2026-10-02, review point 88): only Restore puts back the look it opened on.
    /// OmniDialog reports only its closing.
    /// </summary>
    private Task OnDialogOpenChangedAsync(bool _) => CloseAsync();

    /// <summary>Puts back the look the window opened on, then closes it.</summary>
    private async Task CancelAsync()
    {
        if (_openedOn is { } openedOn)
        {
            await ApplyLookAsync(openedOn);
        }

        await CloseAsync();
    }

    /// <summary>Keeps the look in force and closes the window.</summary>
    private Task ApplyAsync() => CloseAsync();

    private async Task CloseAsync()
    {
        _openedOn = null;
        await OpenChanged.InvokeAsync(false);
    }

    /// <summary>Every bound setting back to its default; the window stays open, Cancel can still undo it.</summary>
    private Task ResetAllAsync() => ApplyLookAsync(Look.Default);

    /// <summary>
    /// Raises the change of every bound setting that differs from <paramref name="look"/>. The palette and
    /// the font follow a new theme even when they look unchanged, since the host drops them with it.
    /// </summary>
    private async Task ApplyLookAsync(Look look)
    {
        var current = Current;
        var themeChanged = PresetChanged.HasDelegate && !Equals(current.Preset, look.Preset);
        await RaiseAsync(AppearanceChanged, current.Appearance != look.Appearance, look.Appearance);
        await RaiseAsync(PresetChanged, themeChanged, look.Preset);
        await RaiseAsync(PaletteChanged, themeChanged || !Equals(current.Palette, look.Palette), look.Palette);
        await RaiseAsync(FontChanged, themeChanged || !Equals(current.Font, look.Font), look.Font);
        await RaiseAsync(BackdropMotionChanged, current.BackdropMotion != look.BackdropMotion, look.BackdropMotion);
        await RaiseAsync(TextSizeLevelChanged, current.TextSizeLevel != look.TextSizeLevel, look.TextSizeLevel);
        await RaiseAsync(DensityChanged, current.Density != look.Density, look.Density);
        await RaiseAsync(ControlSizeLevelChanged, current.ControlSizeLevel != look.ControlSizeLevel, look.ControlSizeLevel);
        await RaiseAsync(FullWidthChanged, current.FullWidth != look.FullWidth, look.FullWidth);
    }

    /// <summary>Raises a bound setting's change when it is bound and the value differs.</summary>
    private static Task RaiseAsync<T>(EventCallback<T> changed, bool differs, T value) =>
        changed.HasDelegate && differs ? changed.InvokeAsync(value) : Task.CompletedTask;

    private bool ShowsBackdropMotion => BackdropMotionChanged.HasDelegate && AppearanceChoices.MovesBackdrop(Preset);

    private bool ShowsRows => AppearanceChanged.HasDelegate || PresetChanged.HasDelegate || PaletteChanged.HasDelegate || FontChanged.HasDelegate || ShowsBackdropMotion
        || TextSizeLevelChanged.HasDelegate || DensityChanged.HasDelegate || ControlSizeLevelChanged.HasDelegate || FullWidthChanged.HasDelegate;

    /// <summary>The mode a theme drawn in one mode only fixes, or null.</summary>
    private OmniAppearance? FixedMode => Preset?.FixedAppearance;

    private bool ModeFixed => FixedMode is not null;

    private string? ModeFixedTitle => ModeFixed ? Localize("SettingsDarkOnly") : null;

    private OmniAppearance ShownAppearance => FixedMode ?? Appearance;

    private IReadOnlyList<OmniOption<OmniAppearance>> Modes =>
    [
        new(OmniAppearance.Light, Localize("AppearanceLight")),
        new(OmniAppearance.Dark, Localize("AppearanceDark")),
        new(OmniAppearance.System, Localize("AppearanceSystem"))
    ];

    private static OmniIconName ModeIcon(OmniAppearance mode) => mode switch
    {
        OmniAppearance.Light => OmniIconName.ThemeLight,
        OmniAppearance.Dark => OmniIconName.ThemeDark,
        _ => OmniIconName.ThemeSystem
    };

    // A dark-only theme disables the mode buttons, and a disabled OmniButton raises no click.
    private Task SetAppearanceAsync(OmniAppearance mode) => AppearanceChanged.InvokeAsync(mode);

    private string ThemeName => AppearanceChoices.ThemeName(Preset);

    private string PaletteName => AppearanceChoices.PaletteName(Preset, Palette);

    private string FontName => AppearanceChoices.FontName(Preset, Font);

    private IReadOnlyList<OmniOption<string>> ThemeOptions => AppearanceChoices.ThemeOptions(Localize("SettingsDefaultSuffix"));

    private IReadOnlyList<OmniOption<string>> PaletteOptions => AppearanceChoices.PaletteOptions(Preset, Localize("SettingsDefaultSuffix"));

    private IReadOnlyList<OmniOption<string>> FontOptions => AppearanceChoices.FontOptions(Preset, Localize("SettingsDefaultSuffix"));

    private IReadOnlyList<OmniOption<OmniDensity>> DensityOptions =>
        [.. Enum.GetValues<OmniDensity>().Select(density => new OmniOption<OmniDensity>(density, Localize(DensityKey(density))))];

    /// <summary>Centred first, the default, then the whole width.</summary>
    private IReadOnlyList<OmniOption<bool>> WidthOptions =>
        [new(false, Localize("ContentWidthCentered")), new(true, Localize("ContentWidthFull"))];

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
    /// force when the palette is bound: any palette paints any theme. A font other than the one in force
    /// is drawn the same way when the font is bound.
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

        if (FontChanged.HasDelegate)
        {
            // A font drawn too, other than the one in force (review point 90): any font sets any
            // theme. The new theme's own font comes back as null, as when it is picked in the list.
            var inForceFont = OmniThemeFonts.All.FirstOrDefault(item => item.Name == AppearanceChoices.FontName(Preset, Font));
            var font = AppearanceChoices.RandomOther(OmniThemeFonts.All, inForceFont);
            await FontChanged.InvokeAsync(AppearanceChoices.Font(preset, font.Name));
        }
    }

    private Task SetPaletteAsync(string? name) => PaletteChanged.InvokeAsync(AppearanceChoices.Palette(Preset, name));

    private Task ResetPaletteAsync() => PaletteChanged.InvokeAsync(null);

    private Task SetFontAsync(string? name) => FontChanged.InvokeAsync(AppearanceChoices.Font(Preset, name));

    private Task ResetFontAsync() => FontChanged.InvokeAsync(null);

    private Task SetBackdropMotionAsync(bool moves) => BackdropMotionChanged.InvokeAsync(moves);

    private Task SetDensityAsync(OmniDensity density) => DensityChanged.InvokeAsync(density);

    private Task SetFullWidthAsync(bool fullWidth) => FullWidthChanged.InvokeAsync(fullWidth);

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

    /// <summary>
    /// The motion of a theme that moves its field: its text, then the switch it names. It sits at the end
    /// of the theme row's title line, or in a row of its own when the host binds no theme.
    /// </summary>
    private RenderFragment MotionSwitch => builder =>
    {
        var labelId = RowId("motion");
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "omni-appearance-settings__motion");
        builder.OpenElement(2, "span");
        builder.AddAttribute(3, "id", labelId);
        builder.OpenComponent<OmniIcon>(4);
        builder.AddComponentParameter(5, nameof(OmniIcon.Name), OmniIconName.Sparkle);
        builder.CloseComponent();
        builder.AddContent(6, " " + Localize("SettingsBackdropMotion"));
        builder.CloseElement();
        builder.OpenComponent<OmniSwitch<bool>>(7);
        builder.AddComponentParameter(8, nameof(OmniSwitch<bool>.Value), BackdropMotion);
        builder.AddComponentParameter(9, nameof(OmniSwitch<bool>.ValueChanged), EventCallback.Factory.Create<bool>(this, SetBackdropMotionAsync));
        builder.AddComponentParameter(10, nameof(OmniSwitch<bool>.ValueExpression), (Expression<Func<bool>>)(() => BackdropMotion));
        builder.AddComponentParameter(11, "aria-labelledby", labelId);
        builder.CloseComponent();
        builder.CloseElement();
    };

    private static RenderFragment Icon(OmniIconName icon) => builder =>
    {
        builder.OpenComponent<OmniIcon>(0);
        builder.AddComponentParameter(1, nameof(OmniIcon.Name), icon);
        builder.CloseComponent();
    };

    /// <summary>Every setting of the window at once, as the host holds them.</summary>
    private sealed record Look(
        OmniAppearance Appearance,
        OmniThemePreset? Preset,
        OmniThemePalette? Palette,
        OmniThemeFont? Font,
        bool BackdropMotion,
        int TextSizeLevel,
        OmniDensity Density,
        int ControlSizeLevel,
        bool FullWidth)
    {
        /// <summary>The look of a host that never chose anything: the parameters' own defaults.</summary>
        public static Look Default { get; } = new(OmniAppearance.System, null, null, null, true, 5, OmniDensity.Comfortable, 5, false);
    }

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
