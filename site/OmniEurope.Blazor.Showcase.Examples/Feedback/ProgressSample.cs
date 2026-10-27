using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Feedback;

/// <summary>One gauge tone with a value and a name that suit its meaning.</summary>
/// <param name="Tone">The tone of the gauge.</param>
/// <param name="Value">The value, from 0 to 100.</param>
/// <param name="Label">The accessible name, also shown with the gauge.</param>
public sealed record ProgressSample(OmniTone Tone, double Value, string Label)
{
    /// <summary>The six tones, each with a sample that suits its meaning.</summary>
    public static IReadOnlyList<ProgressSample> All { get; } =
    [
        new(OmniTone.Accent, 62, "Upload"),
        new(OmniTone.Neutral, 40, "Indexing"),
        new(OmniTone.Info, 55, "Synchronisation"),
        new(OmniTone.Success, 88, "Files processed"),
        new(OmniTone.Warning, 76, "Quota nearly reached"),
        new(OmniTone.Danger, 12, "Import failures")
    ];
}
