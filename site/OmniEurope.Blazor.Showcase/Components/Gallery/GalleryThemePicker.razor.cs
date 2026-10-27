using OmniEurope.Blazor.Showcase.Resources;
using OmniEurope.Blazor.Showcase.Theming;

namespace OmniEurope.Blazor.Showcase.Components.Gallery;

/// <summary>
/// Theme and palette pickers of a gallery page, bound to the showcase's own <see cref="ThemeState"/>.
/// </summary>
public partial class GalleryThemePicker : IDisposable
{
    private static readonly IReadOnlyList<OmniOption<string>> ThemeOptions =
        [.. OmniThemePresets.All.Select(theme => new OmniOption<string>(theme.Name, theme.Name))];

    private static readonly IReadOnlyList<OmniOption<string>> PaletteOptions =
        [.. OmniThemePalettes.All.Select(palette => new OmniOption<string>(palette.Name, palette.Name))];

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    [Inject]
    private ThemeState Theme { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

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

    private void OpenCustomizer() => Navigation.NavigateTo("customizer");

    private void OnThemeChanged() => InvokeAsync(StateHasChanged);
}
