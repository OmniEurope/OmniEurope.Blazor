using OmniEurope.Blazor.Showcase.Resources;
using OmniEurope.Blazor.Showcase.Resources.Gallery;
using OmniEurope.Blazor.Showcase.Theming;

namespace OmniEurope.Blazor.Showcase.Components.Gallery;

/// <summary>
/// Theme and palette pickers and the light or dark toggle of a gallery page, bound to the showcase's own
/// <see cref="ThemeState"/>.
/// </summary>
public partial class GalleryThemePicker : IDisposable
{
    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    [Inject]
    private IStringLocalizer<FoundationsStrings> Foundations { get; set; } = default!;

    [Inject]
    private ThemeState Theme { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>The themes of the catalogue, the shipped one marked as the default.</summary>
    private IReadOnlyList<OmniOption<string>> ThemeOptions =>
        [.. OmniThemePresets.All.Select(theme => new OmniOption<string>(
            theme.Name,
            ReferenceEquals(theme, OmniThemePresets.All[0]) ? WithDefaultMark(theme.Name) : theme.Name))];

    /// <summary>The palettes of the catalogue, the one the selected theme comes with marked as the default.</summary>
    private IReadOnlyList<OmniOption<string>> PaletteOptions
    {
        get
        {
            var themePalette = ThemeState.DefaultPaletteOf(Theme.Theme);
            return [.. OmniThemePalettes.All.Select(palette => new OmniOption<string>(
                palette.Name,
                ReferenceEquals(palette, themePalette) ? WithDefaultMark(palette.Name) : palette.Name))];
        }
    }

    /// <summary>Whether the page is drawn dark; the system mode reads as light, as in the site header.</summary>
    private bool IsDark => Theme.EffectiveMode is ThemeMode.Dark;

    private string ModeLabel => IsDark ? Text["ModeLight"] : Text["ModeDark"];

    protected override void OnInitialized() => Theme.Changed += OnThemeChanged;

    public void Dispose()
    {
        Theme.Changed -= OnThemeChanged;
        GC.SuppressFinalize(this);
    }

    private Task SelectThemeAsync(string? name) =>
        OmniThemePresets.All.FirstOrDefault(theme => theme.Name == name) is { } theme
            ? Theme.SelectThemeAsync(theme)
            : Task.CompletedTask;

    private Task SelectPaletteAsync(string? name) =>
        OmniThemePalettes.All.FirstOrDefault(palette => palette.Name == name) is { } palette
            ? Theme.SelectPaletteAsync(palette)
            : Task.CompletedTask;

    private Task ToggleModeAsync() => Theme.SetModeAsync(IsDark ? ThemeMode.Light : ThemeMode.Dark);

    private string WithDefaultMark(string name) => $"{name} ({Text["GalleryDefaultSuffix"]})";

    // The theme page carries its own settings, so the shortcut to it is pointless there.
    private bool OnThemePage => Navigation.ToBaseRelativePath(Navigation.Uri)
        .StartsWith("components/theme", StringComparison.OrdinalIgnoreCase);

    private void OpenThemePage() => Navigation.NavigateTo("components/theme");

    private void OpenCustomizer() => Navigation.NavigateTo("customizer");

    private void OnThemeChanged() => InvokeAsync(StateHasChanged);
}
