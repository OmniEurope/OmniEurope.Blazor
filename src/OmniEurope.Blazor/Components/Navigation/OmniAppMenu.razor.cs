namespace OmniEurope.Blazor.Components;

/// <summary>
/// The menu at the end of an app bar, one standard for every application: the signed-in user and their
/// role, the application's own rows, the language, the mode (light, dark, system), the theme, the settings,
/// the version and signing out. The mode and the theme are always offered; every other row shows only when
/// the host gives what it needs. The menu keeps no setting of its own: the host stores and applies the
/// mode and the language it raises, and opens the appearance window (<see cref="OmniAppearanceWindow"/>)
/// on <see cref="OnTheme"/>.
/// </summary>
public partial class OmniAppMenu
{
    private readonly string _idPrefix = $"omni-app-menu-{Guid.NewGuid():N}";
    private bool _open;

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

    /// <summary>Raised with the mode picked; the host stores and applies it.</summary>
    [Parameter, EditorRequired]
    public EventCallback<OmniAppearance> AppearanceChanged { get; set; }

    /// <summary>The theme in force: a theme drawn in dark mode only (<see cref="OmniThemePreset.DarkOnly"/>) fixes the mode.</summary>
    [Parameter]
    public OmniThemePreset? Preset { get; set; }

    /// <summary>Raised by the Theme row, after the menu closes: the host opens its appearance window.</summary>
    [Parameter, EditorRequired]
    public EventCallback OnTheme { get; set; }

    /// <summary>Raised by the Settings row, after the menu closes; bound, it shows that row.</summary>
    [Parameter]
    public EventCallback OnSettings { get; set; }

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

    private string EffectiveLabel => LocalizeOr(Label, "AppMenuLabel");

    private bool DarkOnly => Preset?.DarkOnly == true;

    private OmniAppearance ShownAppearance => DarkOnly ? OmniAppearance.Dark : Appearance;

    private bool ShowsLanguages => Languages is { Count: > 1 };

    // Both are read only in the language row, drawn when ShowsLanguages: Languages is then a list.
    private OmniAppMenuLanguage? CurrentLanguage => Languages!.FirstOrDefault(language => language.Code == Language);

    private IReadOnlyList<OmniOption<string>> LanguageOptions =>
        [.. Languages!.Select(language => new OmniOption<string>(language.Code, language.Name))];

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

    private string RowId(string row) => $"{_idPrefix}-{row}";

    private async Task SetOpenAsync(bool open)
    {
        _open = open;
        await OpenChanged.InvokeAsync(open);
    }

    // A dark-only theme disables the mode buttons, and a disabled OmniButton raises no click.
    private Task SetAppearanceAsync(OmniAppearance mode) => AppearanceChanged.InvokeAsync(mode);

    // The list offers only the codes of Languages, and no empty choice.
    private Task SetLanguageAsync(string? code) => LanguageChanged.InvokeAsync(code!);

    private async Task OpenThemeAsync()
    {
        await SetOpenAsync(false);
        await OnTheme.InvokeAsync();
    }

    private async Task OpenSettingsAsync()
    {
        await SetOpenAsync(false);
        await OnSettings.InvokeAsync();
    }

    private async Task SignOutAsync()
    {
        await SetOpenAsync(false);
        await OnSignOut.InvokeAsync();
    }
}
