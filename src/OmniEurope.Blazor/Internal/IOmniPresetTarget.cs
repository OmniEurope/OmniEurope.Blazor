namespace OmniEurope.Blazor.Internal;

/// <summary>
/// A package component that takes a preset: <see cref="Components.OmniComponentBase"/> and
/// <see cref="Components.OmniInputBase{TValue}"/>. The preset registry recognises a package component
/// through it without naming either base.
/// </summary>
internal interface IOmniPresetTarget
{
    /// <summary>The preset the component takes; the one parameter a preset can never set.</summary>
    string? PresetName { get; }
}
