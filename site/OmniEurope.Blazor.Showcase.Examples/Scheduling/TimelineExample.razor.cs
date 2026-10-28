using System.Globalization;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Scheduling;

/// <summary>A timeline whose side, date, titles, details, length and order are parameters.</summary>
public partial class TimelineExample
{
    private static readonly DateTimeOffset Today = new(DateTime.Today);

    private static readonly IReadOnlyList<TimelineEntry> Entries =
    [
        new("filed", "File filed", "Filed online with eleven documents.", Today.AddDays(-9).AddHours(10)),
        new("complete", "File complete", "The three missing documents arrived by post.", Today.AddDays(-7).AddHours(14.5)),
        new("reviewed", "Review finished", "The reviewer recommends acceptance.", Today.AddDays(-4).AddHours(15)),
        new("decided", "Decision signed", "Signed by the head of department.", Today.AddDays(-1).AddHours(11)),
        new("notified", "Applicant notified", "Sent by post and by email.", Today.AddHours(9)),
        new("archived", "Archiving planned", "The file will move to the archive once the appeal period is over.", Today.AddDays(30).AddHours(9))
    ];

    /// <summary>Which side of the line the entries sit on, or whether they take turns.</summary>
    [Parameter]
    public OmniTimelineLayout Layout { get; set; } = OmniTimelineLayout.End;

    /// <summary>How the date of an entry is written.</summary>
    [Parameter]
    public TimelineDateMode DateMode { get; set; } = TimelineDateMode.Automatic;

    /// <summary>Whether each entry has a title.</summary>
    [Parameter]
    public bool ShowTitles { get; set; } = true;

    /// <summary>Whether each entry has its content under the header.</summary>
    [Parameter]
    public bool ShowDetails { get; set; } = true;

    /// <summary>How many of the first entries are kept.</summary>
    [Parameter]
    public double ItemCount { get; set; } = 5;

    /// <summary>Whether the order of the entries is reversed.</summary>
    [Parameter]
    public bool NewestFirst { get; set; }

    private IReadOnlyList<TimelineEntry> VisibleEntries
    {
        get
        {
            var visible = Entries.Take(Math.Clamp((int)ItemCount, 1, Entries.Count));
            return [.. NewestFirst ? visible.Reverse() : visible];
        }
    }

    // The machine date stays in the datetime attribute even when the visible text is a relative one.
    private DateTimeOffset? DateFor(TimelineEntry entry) => DateMode == TimelineDateMode.None ? null : entry.Date;

    private string? DateTextFor(TimelineEntry entry) => DateMode == TimelineDateMode.Relative ? RelativeText(entry.Date) : null;

    private static string RelativeText(DateTimeOffset date)
    {
        var days = (int)Math.Round((date.Date - Today.Date).TotalDays);
        return days switch
        {
            0 => "Today",
            -1 => "Yesterday",
            < 0 => string.Format(CultureInfo.CurrentCulture, "{0} days ago", -days),
            _ => string.Format(CultureInfo.CurrentCulture, "In {0} days", days)
        };
    }

    private sealed record TimelineEntry(string Key, string Title, string Detail, DateTimeOffset Date);
}
