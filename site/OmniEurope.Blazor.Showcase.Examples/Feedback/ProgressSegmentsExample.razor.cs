using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Feedback;

/// <summary>A progress bar that stacks three values on one total, as a line and as a ring.</summary>
public partial class ProgressSegmentsExample
{
    private static readonly OmniProgressSegment[] Lesson =
    [
        new(12, "Discovered", OmniTone.Info),
        new(8, "Known", OmniTone.Accent),
        new(3, "Mastered", OmniTone.Success)
    ];
}
