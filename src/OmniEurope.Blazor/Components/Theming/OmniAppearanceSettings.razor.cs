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
/// <para>
/// Settings whose markup writes none of the eighteen appearance parameters (the nine values, from
/// <see cref="Appearance"/> to <see cref="FullWidth"/>, and their change callbacks) follow the user's
/// <see cref="OmniAppearanceState"/> instead, with every setting of the window: values read from the
/// state, changes written to it. Writing any of them keeps the component controlled by the host, as
/// before. The choice is made at the first render.
/// </para>
/// </remarks>
public partial class OmniAppearanceSettings : IDisposable
{
    private AppearanceFollower? _shared;
    private bool _decided;

    /// <summary>The appearance parameters, values and change callbacks: a host that writes any of them controls the component.</summary>
    private static readonly string[] AppearanceParameters =
    [
        nameof(Appearance), nameof(AppearanceChanged), nameof(Preset), nameof(PresetChanged), nameof(Palette), nameof(PaletteChanged),
        nameof(Font), nameof(FontChanged), nameof(BackdropMotion), nameof(BackdropMotionChanged), nameof(TextSizeLevel),
        nameof(TextSizeLevelChanged), nameof(Density), nameof(DensityChanged), nameof(ControlSizeLevel), nameof(ControlSizeLevelChanged),
        nameof(FullWidth), nameof(FullWidthChanged)
    ];

    /// <summary>The mode a theme drawn in dark mode only fixes, or null: then it cannot change.</summary>
    private OmniAppearance? FixedMode => Preset?.FixedAppearance;

    private bool ModeFixed => FixedMode is not null;

    private string? ModeFixedTitle => ModeFixed ? Localize("SettingsDarkOnly") : null;

    private OmniAppearance ShownAppearance => FixedMode ?? Appearance;

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
    /// Whether the page content takes the whole width rather than the host's centred column; the host
    /// applies it, for example through <see cref="OmniMain.ContentWidth"/>. False (centred) by default.
    /// </summary>
    [Parameter] public bool FullWidth { get; set; }

    /// <summary>Raised with the width picked; bound, the window shows the content width row.</summary>
    [Parameter] public EventCallback<bool> FullWidthChanged { get; set; }

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

    /// <summary>The window gets the width change only when the host binds it, as it shows the row only then.</summary>
    private EventCallback<bool> WindowFullWidthChanged => FullWidthChanged.HasDelegate
        ? EventCallback.Factory.Create<bool>(this, ChangeFullWidthAsync)
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

    private static OmniIconName ModeIcon(OmniAppearance mode) => mode switch
    {
        OmniAppearance.Light => OmniIconName.ThemeLight,
        OmniAppearance.Dark => OmniIconName.ThemeDark,
        _ => OmniIconName.ThemeSystem
    };

    private string RowId(string row) => $"{_idPrefix}-{row}";

    /// <summary>
    /// Decides, from the parameters of the first render, whether the settings follow the user's
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

    /// <summary>Adopts the open state the host sets, whenever it changes it.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        FollowState();
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

    private Task ChangeFullWidthAsync(bool fullWidth) => FullWidthChanged.InvokeAsync(fullWidth);
}
