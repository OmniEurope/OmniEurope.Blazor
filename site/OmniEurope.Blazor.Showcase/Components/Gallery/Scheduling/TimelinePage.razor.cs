using OmniEurope.Blazor.Showcase.Examples.Scheduling;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Scheduling;

/// <summary>The Timeline page: the side, date, titles, details, length and order of a timeline, then more timelines.</summary>
public partial class TimelinePage
{
    [Inject]
    private IStringLocalizer<SchedulingStrings> Text { get; set; } = default!;

    private TimelineDateMode DateMode { get; set; } = TimelineDateMode.Automatic;

    private OmniTimelineLayout Layout { get; set; } = OmniTimelineLayout.End;

    private bool ShowTitles { get; set; } = true;

    private bool ShowDetails { get; set; } = true;

    private double ItemCount { get; set; } = 5;

    private bool NewestFirst { get; set; }

    private IReadOnlyList<OmniOption<TimelineDateMode>> DateModeOptions =>
    [
        new(TimelineDateMode.Automatic, Text["TimelineDateAutomatic"]),
        new(TimelineDateMode.Relative, Text["TimelineDateRelative"]),
        new(TimelineDateMode.None, Text["TimelineDateNone"])
    ];

    private IReadOnlyList<OmniOption<OmniTimelineLayout>> LayoutOptions =>
    [
        new(OmniTimelineLayout.Start, Text["TimelineLayoutStart"]),
        new(OmniTimelineLayout.End, Text["TimelineLayoutEnd"]),
        new(OmniTimelineLayout.AlternateEnd, Text["TimelineLayoutAlternateEnd"]),
        new(OmniTimelineLayout.AlternateStart, Text["TimelineLayoutAlternateStart"])
    ];
}
