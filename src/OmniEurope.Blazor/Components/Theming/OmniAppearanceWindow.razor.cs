namespace OmniEurope.Blazor.Components;

/// <summary>
/// The small draggable window of the look, modeless so the page stays in view, laid out as the options of a video game:
/// one line per setting (a short label, then the control), under three sections. Style: mode, theme, palette,
/// background and its animation; Text: font and text size; Layout: density, size of the controls and width of the content. Controlled like
/// <see cref="OmniAppearanceSettings"/> (the host keeps and applies every value), and each row only
/// shows once its change is bound. A host opens it from a menu entry ("Theme") through
/// <see cref="Open"/>; <see cref="OmniAppearanceSettings"/> opens the same window from its look row.
/// </summary>
/// <remarks>
/// A new theme comes with its own palette, font and backdrop: picking one here raises <see cref="PresetChanged"/>,
/// then <see cref="PaletteChanged"/>, <see cref="FontChanged"/> and <see cref="BackdropChanged"/> with null for a palette, a font or a backdrop
/// chosen for the previous theme (when they are bound), exactly as <see cref="OmniAppearanceSettings"/>
/// does. The host only stores what it receives.
/// <para>
/// A window whose markup writes none of the twenty appearance parameters (the ten values, from
/// <see cref="Appearance"/> to <see cref="FullWidth"/>, and their change callbacks) follows the user's
/// <see cref="OmniAppearanceState"/> instead: it shows every row, reads the values from the state and
/// writes the changes to it, so the host only opens it (<see cref="Open"/>). Writing any of them keeps
/// the window controlled by the host, as before. The choice is made at the first render.
/// </para>
/// </remarks>
public partial class OmniAppearanceWindow : IDisposable
{
    private readonly string _idPrefix = $"omni-appearance-{Guid.NewGuid():N}";
    private AppearanceFollower? _shared;
    private bool _decided;

    /// <summary>The appearance parameters, values and change callbacks: a host that writes any of them controls the component.</summary>
    private static readonly string[] AppearanceParameters =
    [
        nameof(Appearance), nameof(AppearanceChanged), nameof(Preset), nameof(PresetChanged), nameof(Palette), nameof(PaletteChanged),
        nameof(Font), nameof(FontChanged), nameof(Backdrop), nameof(BackdropChanged), nameof(BackdropMotion), nameof(BackdropMotionChanged), nameof(TextSizeLevel),
        nameof(TextSizeLevelChanged), nameof(Density), nameof(DensityChanged), nameof(ControlSizeLevel), nameof(ControlSizeLevelChanged),
        nameof(FullWidth), nameof(FullWidthChanged)
    ];

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
    /// Raised with the mode picked; bound, the mode shows as three icon buttons joined, on the first line
    /// of the Style section.
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
    /// The chosen backdrop, or null for the one the theme is drawn over; the host applies it through
    /// <see cref="OmniThemeScope.Backdrop"/>.
    /// </summary>
    [Parameter]
    public OmniThemeBackdrop? Backdrop { get; set; }

    /// <summary>
    /// Raised with the backdrop picked, null for the theme's own; bound, it shows the backdrop row, which
    /// offers the backdrops of <see cref="OmniThemePresets.BackdropsFor"/> (every one of the catalogue, whatever the theme).
    /// </summary>
    [Parameter]
    public EventCallback<OmniThemeBackdrop?> BackdropChanged { get; set; }

    /// <summary>
    /// Whether the backdrop may move its colour field; the host applies it through
    /// <see cref="OmniThemeScope.BackdropMotion"/>. True by default.
    /// </summary>
    [Parameter]
    public bool BackdropMotion { get; set; } = true;

    /// <summary>
    /// Raised with the choice; bound, it shows the switch on its own row, Animation, under the backdrop. Over a backdrop that moves (Givre, Trou noir)
    /// it holds the choice; over any other the setting would do nothing, so the switch is disabled and shown off, and the choice you stored is kept for the next backdrop that moves.
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
    /// host draws it in. The host applies it, for example by passing <see cref="OmniLayoutWidth.FullWidth"/>
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
    /// The look as the host passed it: what <see cref="ApplyLookCoreAsync"/> compares with the look to apply and
    /// what the window keeps when it opens.
    /// </summary>
    private Look Current => new(Appearance, Preset, Palette, Font, Backdrop, BackdropMotion, TextSizeLevel, Density, ControlSizeLevel, FullWidth);

    private string EffectiveTitle => LocalizeOr(Title, "AppearanceWindowTitle");

    /// <summary>
    /// Decides, from the parameters of the first render, whether the window follows the user's
    /// <see cref="OmniAppearanceState"/> (none of its appearance values or change callbacks written).
    /// </summary>
    /// <param name="parameters">The parameters given by the renderer.</param>
    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (!_decided)
        {
            _decided = true;
            var hostControls = AppearanceFollower.Sets(parameters, AppearanceParameters);
            _shared = AppearanceFollower.Start(SharedAppearance, hostControls, () => InvokeAsync(() =>
            {
                FollowState();
                StateHasChanged();
            }));
        }

        return base.SetParametersAsync(parameters);
    }

    /// <summary>Following the state, reads the preference before showing it.</summary>
    protected override Task OnInitializedAsync() => _shared?.State.LoadAsync() ?? Task.CompletedTask;

    /// <summary>Stops following the state.</summary>
    public void Dispose() => _shared?.Dispose();

    /// <summary>Keeps the look in force each time the window opens, for Cancel to put it back.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        FollowState();
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

    /// <summary>Following the state, takes every value from it and sends every change to it.</summary>
    private void FollowState()
    {
        if (_shared?.State is not { } state)
        {
            return;
        }

        Appearance = state.Appearance;
        AppearanceChanged = EventCallback.Factory.Create<OmniAppearance>(this, state.SetAppearanceAsync);
        Preset = state.Preset;
        PresetChanged = EventCallback.Factory.Create<OmniThemePreset?>(this, state.SetPresetAsync);
        Palette = state.Palette;
        PaletteChanged = EventCallback.Factory.Create<OmniThemePalette?>(this, state.SetPaletteAsync);
        Font = state.Font;
        FontChanged = EventCallback.Factory.Create<OmniThemeFont?>(this, state.SetFontAsync);
        Backdrop = state.Backdrop;
        BackdropChanged = EventCallback.Factory.Create<OmniThemeBackdrop?>(this, state.SetBackdropAsync);
        BackdropMotion = state.BackdropMotion;
        BackdropMotionChanged = EventCallback.Factory.Create<bool>(this, state.SetBackdropMotionAsync);
        TextSizeLevel = state.TextSizeLevel;
        TextSizeLevelChanged = EventCallback.Factory.Create<int>(this, state.SetTextSizeLevelAsync);
        Density = state.Density;
        DensityChanged = EventCallback.Factory.Create<OmniDensity>(this, state.SetDensityAsync);
        ControlSizeLevel = state.ControlSizeLevel;
        ControlSizeLevelChanged = EventCallback.Factory.Create<int>(this, state.SetControlSizeLevelAsync);
        FullWidth = state.FullWidth;
        FullWidthChanged = EventCallback.Factory.Create<bool>(this, state.SetFullWidthAsync);
    }

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

    /// <summary>
    /// Every bound setting back to its default, each one raised even when it already holds it, the theme included
    /// (owner decision of 2026-11-12, R1-7): a host that clears its own extras when the theme changes then resets
    /// everything, whatever the theme in force. The window stays open, Cancel can still undo it.
    /// </summary>
    private Task ResetAllAsync() => InBatchAsync(() => ApplyLookCoreAsync(Look.Default, everything: true));

    /// <summary>
    /// Raises the change of every bound setting that differs from <paramref name="look"/> (every one with <c>everything</c>). The palette, the backdrop and
    /// the font follow a new theme even when they look unchanged, since the host drops them with it.
    /// </summary>
    private Task ApplyLookAsync(Look look) => InBatchAsync(() => ApplyLookCoreAsync(look));

    /// <summary>
    /// Following the state, runs a change of several settings as one: the preference is written once, with
    /// the look it leaves. A host that binds the settings itself receives each change as it comes.
    /// </summary>
    private Task InBatchAsync(Func<Task> change) => _shared?.State is { } state ? state.BatchAsync(change) : change();

    private async Task ApplyLookCoreAsync(Look look, bool everything = false)
    {
        var current = Current;
        var themeChanged = PresetChanged.HasDelegate && (everything || !Equals(current.Preset, look.Preset));
        await RaiseAsync(AppearanceChanged, everything || current.Appearance != look.Appearance, look.Appearance);
        await RaiseAsync(PresetChanged, themeChanged, look.Preset);
        await RaiseAsync(PaletteChanged, everything || themeChanged || !Equals(current.Palette, look.Palette), look.Palette);
        await RaiseAsync(FontChanged, everything || themeChanged || !Equals(current.Font, look.Font), look.Font);
        await RaiseAsync(BackdropChanged, everything || themeChanged || !Equals(current.Backdrop, look.Backdrop), look.Backdrop);
        await RaiseAsync(BackdropMotionChanged, everything || current.BackdropMotion != look.BackdropMotion, look.BackdropMotion);
        await RaiseAsync(TextSizeLevelChanged, everything || current.TextSizeLevel != look.TextSizeLevel, look.TextSizeLevel);
        await RaiseAsync(DensityChanged, everything || current.Density != look.Density, look.Density);
        await RaiseAsync(ControlSizeLevelChanged, everything || current.ControlSizeLevel != look.ControlSizeLevel, look.ControlSizeLevel);
        await RaiseAsync(FullWidthChanged, everything || current.FullWidth != look.FullWidth, look.FullWidth);
    }

    /// <summary>Raises a bound setting's change when it is bound and the value differs.</summary>
    private static Task RaiseAsync<T>(EventCallback<T> changed, bool differs, T value) =>
        changed.HasDelegate && differs ? changed.InvokeAsync(value) : Task.CompletedTask;

    private bool ShowsBackdropMotion => BackdropMotionChanged.HasDelegate;

    /// <summary>Whether the backdrop drawn moves its colour field: the switch is enabled only then.</summary>
    private bool MovesBackdrop => AppearanceChoices.MovesBackdrop(Preset, Backdrop);

    /// <summary>The switch as drawn: the stored choice over a backdrop that moves, off over one that does not (the stored choice is kept).</summary>
    private bool ShownBackdropMotion => MovesBackdrop && BackdropMotion;

    /// <summary>The Style section shows once one of its rows does: mode, theme, palette, background, animation.</summary>
    private bool ShowsStyle => AppearanceChanged.HasDelegate || PresetChanged.HasDelegate || PaletteChanged.HasDelegate || BackdropChanged.HasDelegate || ShowsBackdropMotion;

    /// <summary>The Text section: font and text size.</summary>
    private bool ShowsText => FontChanged.HasDelegate || TextSizeLevelChanged.HasDelegate;

    /// <summary>The Layout section: density, size of the controls and width of the content.</summary>
    private bool ShowsLayout => DensityChanged.HasDelegate || ControlSizeLevelChanged.HasDelegate || FullWidthChanged.HasDelegate;

    private bool ShowsRows => ShowsStyle || ShowsText || ShowsLayout;

    /// <summary>The mode a theme drawn in one mode only fixes, or null.</summary>
    private OmniAppearance? FixedMode => Preset?.FixedAppearance;

    private bool ModeFixed => FixedMode is not null;

    private OmniAppearance ShownAppearance => FixedMode ?? Appearance;

    // A dark-only theme disables the mode buttons, and a disabled OmniButton raises no click.
    private RenderFragment ModeGroup => AppearanceModeGroup.Render(this, ShownAppearance, ModeFixed, key => Localize(key), SetAppearanceAsync);

    private Task SetAppearanceAsync(OmniAppearance mode) => AppearanceChanged.InvokeAsync(mode);

    private string ThemeName => AppearanceChoices.ThemeName(Preset);

    private string PaletteName => AppearanceChoices.PaletteName(Preset, Palette);

    private string FontName => AppearanceChoices.FontName(Preset, Font);

    private string BackdropName => AppearanceChoices.BackdropName(Preset, Backdrop);

    private IReadOnlyList<OmniOption<string>> ThemeOptions => AppearanceChoices.ThemeOptions(Localize("SettingsDefaultSuffix"));

    private IReadOnlyList<OmniOption<string>> PaletteOptions => AppearanceChoices.PaletteOptions(Preset, Localize("SettingsDefaultSuffix"));

    private IReadOnlyList<OmniOption<string>> FontOptions => AppearanceChoices.FontOptions(Preset, Localize("SettingsDefaultSuffix"));

    private IReadOnlyList<OmniOption<string>> BackdropOptions =>
        AppearanceChoices.BackdropOptions(Preset, Localize("SettingsDefaultSuffix"), Localize("SettingsBackdropNone"));

    private IReadOnlyList<OmniOption<OmniDensity>> DensityOptions =>
        [.. Enum.GetValues<OmniDensity>().Select(density => new OmniOption<OmniDensity>(density, Localize(DensityKey(density))))];

    /// <summary>Centred first, the default, then the whole width.</summary>
    private IReadOnlyList<OmniOption<bool>> WidthOptions =>
        [new(false, Localize("ContentWidthCentered")), new(true, Localize("ContentWidthFull"))];

    private ScaleSetting TextSizeScale => new(
        "text-size", OmniIconName.TextSize, "SettingsTextSize", "SettingsTextSizeDecrease", "SettingsTextSizeIncrease",
        TextSizeLevel, TextSizeLevelChanged);

    private ScaleSetting ControlSizeScale => new(
        "control-size", OmniIconName.CornersOut, "SettingsControlSize", "SettingsControlSizeDecrease", "SettingsControlSizeIncrease",
        ControlSizeLevel, ControlSizeLevelChanged);

    /// <summary>
    /// What a level draws, as a percentage of the default: 75 % at level 1 up to 131.25 % at level 10, 100 % at
    /// level 5 (the steps of <c>data-oe-text-size</c> and <c>data-oe-control-size</c>).
    /// </summary>
    internal static double ScalePercent(int level) => 75d + 6.25d * (level - 1);

    /// <summary>The percentage a slider position stands for, written as the row shows it (the slider value and its spoken text).</summary>
    private string ScaleText(double level) => Localize("SettingsScaleValue", ScalePercent((int)Math.Round(level)));

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

    /// <summary>The new theme, then null for the palette, the font and the backdrop chosen for the previous one.</summary>
    private Task ChangeThemeAsync(OmniThemePreset? preset) => InBatchAsync(() => ChangeThemeCoreAsync(preset));

    /// <remarks>
    /// The palette, the font and the backdrop reset are the ones in force when the theme was picked: a host that writes the
    /// theme slowly (an account round trip) may receive a palette or a font picked in the meantime, which
    /// the new theme keeps.
    /// </remarks>
    private async Task ChangeThemeCoreAsync(OmniThemePreset? preset)
    {
        var palette = Palette;
        var font = Font;
        var backdrop = Backdrop;
        await PresetChanged.InvokeAsync(preset);
        if (palette is not null && Equals(Palette, palette) && PaletteChanged.HasDelegate)
        {
            await PaletteChanged.InvokeAsync(null);
        }

        if (font is not null && Equals(Font, font) && FontChanged.HasDelegate)
        {
            await FontChanged.InvokeAsync(null);
        }

        if (backdrop is not null && Equals(Backdrop, backdrop) && BackdropChanged.HasDelegate)
        {
            await BackdropChanged.InvokeAsync(null);
        }
    }

    /// <summary>
    /// A theme drawn at random, other than the current one, and with it a palette other than the one in
    /// force when the palette is bound: any palette paints any theme. A font other than the one in force
    /// is drawn the same way when the font is bound, and a backdrop (the plain page included) other than the one
    /// in force, among those the new theme offers, when the backdrop is bound. Never the mode, the density, the sizes or the width.
    /// </summary>
    private Task RandomLookAsync() => InBatchAsync(() => AppearanceChoices.RandomLookAsync(Preset, Palette, Font, PresetChanged, PaletteChanged, FontChanged, Backdrop, BackdropChanged));

    private Task SetPaletteAsync(string? name) => PaletteChanged.InvokeAsync(AppearanceChoices.Palette(Preset, name));

    private Task ResetPaletteAsync() => PaletteChanged.InvokeAsync(null);

    private Task SetFontAsync(string? name) => FontChanged.InvokeAsync(AppearanceChoices.Font(Preset, name));

    private Task ResetFontAsync() => FontChanged.InvokeAsync(null);

    private Task SetBackdropAsync(string? name) => BackdropChanged.InvokeAsync(AppearanceChoices.Backdrop(Preset, name));

    private Task ResetBackdropAsync() => BackdropChanged.InvokeAsync(null);

    private Task SetBackdropMotionAsync(bool moves) => BackdropMotionChanged.InvokeAsync(moves);

    private Task SetDensityAsync(OmniDensity density) => DensityChanged.InvokeAsync(density);

    private Task SetFullWidthAsync(bool fullWidth) => FullWidthChanged.InvokeAsync(fullWidth);

    private static Task ChangeLevelAsync(ScaleSetting setting, int level) => setting.Changed.InvokeAsync(Math.Clamp(level, 1, 10));

    /// <summary>Every setting of the window at once, as the host holds them.</summary>
    private sealed record Look(
        OmniAppearance Appearance,
        OmniThemePreset? Preset,
        OmniThemePalette? Palette,
        OmniThemeFont? Font,
        OmniThemeBackdrop? Backdrop,
        bool BackdropMotion,
        int TextSizeLevel,
        OmniDensity Density,
        int ControlSizeLevel,
        bool FullWidth)
    {
        /// <summary>The look of a host that never chose anything: the parameters' own defaults.</summary>
        public static Look Default { get; } = new(OmniAppearance.System, null, null, null, null, true, 5, OmniDensity.Comfortable, 5, false);
    }

    /// <summary>What differs between the scale rows: ids, icon, texts, level and change.</summary>
    private sealed record ScaleSetting(
        string Key,
        OmniIconName Icon,
        string NameKey,
        string DecreaseKey,
        string IncreaseKey,
        int Level,
        EventCallback<int> Changed)
    {
        /// <summary>The level as a slider value, for the range input.</summary>
        public double LevelValue => Level;
    }
}
