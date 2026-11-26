using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The link of an appearance component (<see cref="IOmniAppearanceTarget"/>) to the user's
/// <see cref="OmniAppearanceState"/>: decided once from the parameters of its first render, as
/// <see cref="AppearanceFollower"/> does, then every value taken from the state and every change sent to it.
/// </summary>
internal sealed class AppearanceLink : IDisposable
{
    /// <summary>The appearance parameters, values and change callbacks: a host that writes any of them controls the component.</summary>
    private static readonly string[] AppearanceParameters =
    [
        nameof(IOmniAppearanceTarget.Appearance), nameof(IOmniAppearanceTarget.AppearanceChanged),
        nameof(IOmniAppearanceTarget.Preset), nameof(IOmniAppearanceTarget.PresetChanged),
        nameof(IOmniAppearanceTarget.Palette), nameof(IOmniAppearanceTarget.PaletteChanged),
        nameof(IOmniAppearanceTarget.Font), nameof(IOmniAppearanceTarget.FontChanged),
        nameof(IOmniAppearanceTarget.Backdrop), nameof(IOmniAppearanceTarget.BackdropChanged),
        nameof(IOmniAppearanceTarget.BackdropMotion), nameof(IOmniAppearanceTarget.BackdropMotionChanged),
        nameof(IOmniAppearanceTarget.TextSizeLevel), nameof(IOmniAppearanceTarget.TextSizeLevelChanged),
        nameof(IOmniAppearanceTarget.Density), nameof(IOmniAppearanceTarget.DensityChanged),
        nameof(IOmniAppearanceTarget.ControlSizeLevel), nameof(IOmniAppearanceTarget.ControlSizeLevelChanged),
        nameof(IOmniAppearanceTarget.FullWidth), nameof(IOmniAppearanceTarget.FullWidthChanged)
    ];

    private readonly IOmniAppearanceTarget _target;
    private AppearanceFollower? _shared;
    private bool _decided;

    internal AppearanceLink(IOmniAppearanceTarget target) => _target = target;

    /// <summary>The state followed, or null when the host controls the component or registered none.</summary>
    internal OmniAppearanceState? State => _shared?.State;

    /// <summary>
    /// On the first render only, decides from <paramref name="parameters"/> whether the component follows
    /// <paramref name="state"/>; each change of the state then refreshes it through <paramref name="invokeAsync"/>.
    /// </summary>
    internal void Decide(OmniAppearanceState? state, ParameterView parameters, Func<Action, Task> invokeAsync, Action render)
    {
        if (_decided)
        {
            return;
        }

        _decided = true;
        _shared = AppearanceFollower.Start(state, parameters, AppearanceParameters, invokeAsync, Follow, render);
    }

    /// <summary>Following the state, reads the preference before showing it.</summary>
    internal Task LoadAsync() => _shared?.State.LoadAsync() ?? Task.CompletedTask;

    /// <summary>Following the state, takes every value from it and sends every change to it.</summary>
    internal void Follow()
    {
        if (_shared?.State is not { } state)
        {
            return;
        }

        var target = _target;
        target.Appearance = state.Appearance;
        target.AppearanceChanged = EventCallback.Factory.Create<OmniAppearance>(target, state.SetAppearanceAsync);
        target.Preset = state.Preset;
        target.PresetChanged = EventCallback.Factory.Create<OmniThemePreset?>(target, state.SetPresetAsync);
        target.Palette = state.Palette;
        target.PaletteChanged = EventCallback.Factory.Create<OmniThemePalette?>(target, state.SetPaletteAsync);
        target.Font = state.Font;
        target.FontChanged = EventCallback.Factory.Create<OmniThemeFont?>(target, state.SetFontAsync);
        target.Backdrop = state.Backdrop;
        target.BackdropChanged = EventCallback.Factory.Create<OmniThemeBackdrop?>(target, state.SetBackdropAsync);
        target.BackdropMotion = state.BackdropMotion;
        target.BackdropMotionChanged = EventCallback.Factory.Create<bool>(target, state.SetBackdropMotionAsync);
        target.TextSizeLevel = state.TextSizeLevel;
        target.TextSizeLevelChanged = EventCallback.Factory.Create<int>(target, state.SetTextSizeLevelAsync);
        target.Density = state.Density;
        target.DensityChanged = EventCallback.Factory.Create<OmniDensity>(target, state.SetDensityAsync);
        target.ControlSizeLevel = state.ControlSizeLevel;
        target.ControlSizeLevelChanged = EventCallback.Factory.Create<int>(target, state.SetControlSizeLevelAsync);
        target.FullWidth = state.FullWidth;
        target.FullWidthChanged = EventCallback.Factory.Create<bool>(target, state.SetFullWidthAsync);
    }

    /// <summary>Stops following the state.</summary>
    public void Dispose() => _shared?.Dispose();
}
