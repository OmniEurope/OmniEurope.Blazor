using OmniEurope.Blazor.Showcase.Examples.Layout;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Layout;

/// <summary>The Stacks page: one stack driven by its settings, then the scales, alignments, distributions, orientations, overflows and dividers.</summary>
public partial class StacksPage
{
    [Inject]
    private IStringLocalizer<LayoutStrings> Text { get; set; } = default!;

    private OmniOrientation Orientation { get; set; } = OmniOrientation.Horizontal;

    private OmniSpacing Gap { get; set; } = OmniSpacing.Medium;

    private OmniAlignment Align { get; set; } = OmniAlignment.Center;

    private OmniJustification Justify { get; set; } = OmniJustification.Start;

    private OmniStackOverflow Overflow { get; set; } = OmniStackOverflow.None;

    private double ItemCount { get; set; } = 4;

    private bool Narrow { get; set; }

    private double MaximumItems => StackItem.All.Count;

    private IReadOnlyList<OmniOption<OmniOrientation>> OrientationOptions =>
        [.. Enum.GetValues<OmniOrientation>().Select(value => new OmniOption<OmniOrientation>(value, Text[$"StacksOrientation{value}"]))];

    private IReadOnlyList<OmniOption<OmniSpacing>> GapOptions =>
        [.. Enum.GetValues<OmniSpacing>().Select(value => new OmniOption<OmniSpacing>(value, Text[$"StacksGap{value}"]))];

    private IReadOnlyList<OmniOption<OmniAlignment>> AlignOptions =>
        [.. Enum.GetValues<OmniAlignment>().Select(value => new OmniOption<OmniAlignment>(value, Text[$"StacksAlign{value}"]))];

    private IReadOnlyList<OmniOption<OmniJustification>> JustifyOptions =>
        [.. Enum.GetValues<OmniJustification>().Select(value => new OmniOption<OmniJustification>(value, Text[$"StacksJustify{value}"]))];

    private IReadOnlyList<OmniOption<OmniStackOverflow>> OverflowOptions =>
        [.. Enum.GetValues<OmniStackOverflow>().Select(value => new OmniOption<OmniStackOverflow>(value, Text[$"StacksOverflow{value}"]))];
}
