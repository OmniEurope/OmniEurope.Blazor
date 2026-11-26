using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The appearance parameters, values and change callbacks, of a component that may follow the user's
/// <see cref="OmniAppearanceState"/> (<see cref="OmniAppearanceSettings"/>, <see cref="OmniAppearanceWindow"/>):
/// what <see cref="AppearanceLink"/> reads and writes for it.
/// </summary>
internal interface IOmniAppearanceTarget : IHandleEvent
{
    OmniAppearance Appearance { get; set; }

    EventCallback<OmniAppearance> AppearanceChanged { get; set; }

    OmniThemePreset? Preset { get; set; }

    EventCallback<OmniThemePreset?> PresetChanged { get; set; }

    OmniThemePalette? Palette { get; set; }

    EventCallback<OmniThemePalette?> PaletteChanged { get; set; }

    OmniThemeFont? Font { get; set; }

    EventCallback<OmniThemeFont?> FontChanged { get; set; }

    OmniThemeBackdrop? Backdrop { get; set; }

    EventCallback<OmniThemeBackdrop?> BackdropChanged { get; set; }

    bool BackdropMotion { get; set; }

    EventCallback<bool> BackdropMotionChanged { get; set; }

    int TextSizeLevel { get; set; }

    EventCallback<int> TextSizeLevelChanged { get; set; }

    OmniDensity Density { get; set; }

    EventCallback<OmniDensity> DensityChanged { get; set; }

    int ControlSizeLevel { get; set; }

    EventCallback<int> ControlSizeLevelChanged { get; set; }

    bool FullWidth { get; set; }

    EventCallback<bool> FullWidthChanged { get; set; }
}
