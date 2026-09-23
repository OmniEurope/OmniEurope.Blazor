using OmniEurope.Blazor.Showcase.Resources;
using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class ThemesDemo
{
    private OmniAppearance _appearance = OmniAppearance.System;
    private OmniThemePreset? _preset;
    private OmniThemePalette? _palette;
    private int _textSizeLevel = 5;
    private int _densityLevel = 5;
    private Sample LiveSample => new(_preset, _palette, _appearance);
    private int _appliedTextSizeLevel;
    private IJSObjectReference? _appearanceModule;

    [Inject] private IJSRuntime JavaScript { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_textSizeLevel == _appliedTextSizeLevel)
        {
            return;
        }

        _appearanceModule ??= await JavaScript.InvokeAsync<IJSObjectReference>(
            "import", "./_content/OmniEurope.Blazor/omni-appearance.js");
        await _appearanceModule.InvokeVoidAsync("setTextSizeLevel", _textSizeLevel);
        _appliedTextSizeLevel = _textSizeLevel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_appearanceModule is null)
        {
            return;
        }

        try
        {
            await _appearanceModule.InvokeVoidAsync("clearTextSizeLevel");
            await _appearanceModule.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }

        GC.SuppressFinalize(this);
    }
    private OmniDensity DemoDensity => _densityLevel switch
    {
        <= 3 => OmniDensity.Compact,
        >= 8 => OmniDensity.Spacious,
        _ => OmniDensity.Comfortable,
    };

    /// <summary>
    /// A theme with a palette that is not its own, the same in dark, a theme with its own palette,
    /// and a palette alone over the shipped shape. Looked up by name so a renamed catalogue entry
    /// fails loudly here instead of quietly showing something else.
    /// </summary>
    private static readonly Sample[] Samples =
    [
        new(Theme("Galet"), Palette("Braise"), OmniAppearance.Light),
        new(Theme("Néon"), Palette("Essentiel"), OmniAppearance.Dark),
        new(Theme("Papier"), null, OmniAppearance.Light),
        new(null, Palette("Lagune"), OmniAppearance.System)
    ];

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private string Caption(Sample sample) => (sample.Theme, sample.Palette) switch
    {
        ({ } theme, { } palette) => Text["ThemesThemeWithPalette", theme.Name, palette.Name],
        ({ } theme, null) => Text["ThemesThemeAlone", theme.Name],
        (null, { } palette) => Text["ThemesPaletteAlone", palette.Name],
        _ => Text["ThemesShipped"]
    };

    private static OmniThemePreset Theme(string name) => OmniThemePresets.All.Single(theme => theme.Name == name);

    private static OmniThemePalette Palette(string name) => OmniThemePalettes.All.Single(palette => palette.Name == name);

    /// <summary>One scope of the demonstration.</summary>
    private sealed record Sample(OmniThemePreset? Theme, OmniThemePalette? Palette, OmniAppearance Appearance);
}
