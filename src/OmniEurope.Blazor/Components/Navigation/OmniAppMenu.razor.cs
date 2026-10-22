namespace OmniEurope.Blazor.Components;

/// <summary>
/// The menu at the end of an app bar, one standard for every application: the signed-in user and their
/// role, the application's own rows, the language, the mode (light, dark, system), the theme, the settings,
/// the version and signing out. The mode and the theme are always offered; every other row shows only when
/// the host gives what it needs. The menu keeps no setting of its own: the host stores and applies the
/// mode and the language it raises, and opens the appearance window (<see cref="OmniAppearanceWindow"/>)
/// on <see cref="OnThemeOpen"/>.
/// </summary>
/// <remarks>
/// A menu whose markup writes none of <see cref="Appearance"/>, <see cref="AppearanceChanged"/> and
/// <see cref="Preset"/> follows the user's <see cref="OmniAppearanceState"/>: the mode and the theme in
/// force come from it and the mode picked is written to it. Writing any of them keeps the menu
/// controlled by the host, as before. The choice is made at the first render; <see cref="OnThemeOpen"/>
/// still opens the host's appearance window, which can follow the state too.
/// </remarks>
public partial class OmniAppMenu : IDisposable
{
    private bool _open;
    private AppearanceFollower? _shared;
    private bool _decided;

    /// <summary>Accessible name of the trigger and of the panel; the localized "Application menu" when null or blank.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// The signed-in user. Set, the trigger shows the name and the menu opens on it; null, the default,
    /// for an application without accounts: the trigger is an icon and the identity row is left out.
    /// </summary>
    [Parameter]
    public string? UserName { get; set; }

    /// <summary>The user's role, shown as a badge beside the name; left out when null or blank.</summary>
    [Parameter]
    public string? Role { get; set; }

    /// <summary>The application's own rows (an organisation picker...), under the identity.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The languages offered; the language row shows only when there are two or more.</summary>
    [Parameter]
    public IReadOnlyList<OmniAppMenuLanguage>? Languages { get; set; }

    /// <summary>The code of the language in force, one of <see cref="Languages"/>.</summary>
    [Parameter]
    public string? Language { get; set; }

    /// <summary>Raised with the code of the language picked; the host applies it (often by reloading).</summary>
    [Parameter]
    public EventCallback<string> LanguageChanged { get; set; }

    /// <summary>Whether the language row shows the flag of the language in force, when it has one. False by default.</summary>
    [Parameter]
    public bool ShowFlags { get; set; }

    /// <summary>The mode in force: light, dark, or following the system.</summary>
    [Parameter]
    public OmniAppearance Appearance { get; set; }

    /// <summary>
    /// Raised with the mode picked; the host stores and applies it. Left out with the mode and the theme, the menu follows the
    /// user's <see cref="OmniAppearanceState"/> (see the remarks).
    /// </summary>
    [Parameter]
    public EventCallback<OmniAppearance> AppearanceChanged { get; set; }

    /// <summary>The theme in force: a theme drawn in dark mode only (<see cref="OmniThemePreset.DarkOnly"/>) fixes the mode.</summary>
    [Parameter]
    public OmniThemePreset? Preset { get; set; }

    /// <summary>Raised by the Theme row, after the menu closes: the host opens its appearance window.</summary>
    [Parameter, EditorRequired]
    public EventCallback OnThemeOpen { get; set; }

    /// <summary>Raised by the Settings row, after the menu closes; bound, it shows that row.</summary>
    [Parameter]
    public EventCallback OnSettingsOpen { get; set; }

    /// <summary>The version line at the bottom of the menu, as the host words it; left out when null or blank.</summary>
    [Parameter]
    public string? Version { get; set; }

    /// <summary>Raised by the sign-out button, after the menu closes; bound, it shows that button.</summary>
    [Parameter]
    public EventCallback OnSignOut { get; set; }

    /// <summary>Whether the menu cannot open. False by default.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Whether the menu is open, when the host controls it; null, the default, lets the menu hold it.</summary>
    [Parameter]
    public bool? Open { get; set; }

    /// <summary>Raised when the menu opens or closes.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    private bool IsOpen => Open ?? _open;

    // The trigger's text: the user's name and the open state, or nothing, and the trigger is then its
    // icon alone, square like every icon-only button.
    private RenderFragment? TriggerText => UserName is null ? null : builder =>
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", "omni-app-menu__trigger-name");
        builder.AddContent(2, UserName);
        builder.CloseElement();
        builder.OpenComponent<OmniIcon>(3);
        builder.AddComponentParameter(4, nameof(OmniIcon.Name), IsOpen ? OmniIconName.ChevronUp : OmniIconName.ChevronDown);
        builder.CloseComponent();
    };

    private string EffectiveLabel => LocalizeOr(Label, "AppMenuLabel");

    /// <summary>The mode a theme drawn in one mode only fixes, or null.</summary>
    private OmniAppearance? FixedMode => Preset?.FixedAppearance;

    private bool ModeFixed => FixedMode is not null;

    private string? ModeFixedTitle => ModeFixed ? Localize("SettingsDarkOnly") : null;

    private OmniAppearance ShownAppearance => FixedMode ?? Appearance;

    private bool ShowsLanguages => Languages is { Count: > 1 };

    // Both are read only in the language row, drawn when ShowsLanguages: Languages is then a list.
    private OmniAppMenuLanguage? CurrentLanguage => Languages!.FirstOrDefault(language => language.Code == Language);

    private IReadOnlyList<OmniOption<string>> LanguageOptions =>
        [.. Languages!.Select(language => new OmniOption<string>(language.Code, language.Name))];

    private string ModeName(OmniAppearance mode) => mode switch
    {
        OmniAppearance.Light => Localize("AppearanceLight"),
        OmniAppearance.Dark => Localize("AppearanceDark"),
        _ => Localize("AppearanceSystem")
    };

    private static OmniIconName ModeIcon(OmniAppearance mode) => mode switch
    {
        OmniAppearance.Light => OmniIconName.ThemeLight,
        OmniAppearance.Dark => OmniIconName.ThemeDark,
        _ => OmniIconName.ThemeSystem
    };

    /// <summary>
    /// Decides, from the parameters of the first render, whether the menu follows the user's
    /// <see cref="OmniAppearanceState"/> (none of <see cref="Appearance"/>, <see cref="AppearanceChanged"/> and <see cref="Preset"/> written).
    /// </summary>
    /// <param name="parameters">The parameters given by the renderer.</param>
    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (!_decided)
        {
            _decided = true;
            _shared = AppearanceFollower.Start(SharedAppearance, AppearanceFollower.Sets(parameters, nameof(Appearance), nameof(AppearanceChanged), nameof(Preset)),
                () => InvokeAsync(() =>
                {
                    FollowState();
                    StateHasChanged();
                }));
        }

        return base.SetParametersAsync(parameters);
    }

    /// <summary>Following the state, reads the preference before showing it.</summary>
    protected override Task OnInitializedAsync() => _shared?.State.LoadAsync() ?? Task.CompletedTask;

    /// <summary>Following the state, takes the mode and the theme from it.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        FollowState();
    }

    /// <summary>Stops following the state.</summary>
    public void Dispose() => _shared?.Dispose();

    private void FollowState()
    {
        if (_shared?.State is not { } state)
        {
            return;
        }

        Appearance = state.Appearance;
        Preset = state.Preset;
        AppearanceChanged = EventCallback.Factory.Create<OmniAppearance>(this, state.SetAppearanceAsync);
    }

    private async Task SetOpenAsync(bool open)
    {
        _open = open;
        await OpenChanged.InvokeAsync(open);
    }

    // The Mode row steps through light, dark, system, then light again. A dark-only theme disables the
    // row, and a disabled OmniButton raises no click.
    private Task CycleAppearanceAsync() => AppearanceChanged.InvokeAsync(NextMode(ShownAppearance));

    private static OmniAppearance NextMode(OmniAppearance mode) => mode switch
    {
        OmniAppearance.Light => OmniAppearance.Dark,
        OmniAppearance.Dark => OmniAppearance.System,
        _ => OmniAppearance.Light
    };

    // The list offers only the codes of Languages, and no empty choice.
    private Task SetLanguageAsync(string? code) => LanguageChanged.InvokeAsync(code!);

    private async Task OpenThemeAsync()
    {
        await SetOpenAsync(false);
        await OnThemeOpen.InvokeAsync();
    }

    private async Task OpenSettingsAsync()
    {
        await SetOpenAsync(false);
        await OnSettingsOpen.InvokeAsync();
    }

    private async Task SignOutAsync()
    {
        await SetOpenAsync(false);
        await OnSignOut.InvokeAsync();
    }
}
