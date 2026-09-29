using System.Globalization;

namespace OmniEurope.Blazor.Components;

/// <summary>One entry of an <see cref="OmniTimeline"/>: a marker on the line, then a date, a heading and content.</summary>
public partial class OmniTimelineItem
{
    /// <summary>
    /// The entry's heading. Optional: an entry whose content already names itself leaves it null and,
    /// without a date either, gets no header at all rather than an empty one.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Heading level of <see cref="Title"/>; H3 by default. Set it to fit the outline of the page.</summary>
    [Parameter] public OmniHeadingLevel Level { get; set; } = OmniHeadingLevel.H3;

    /// <summary>When the entry happened: written in the current culture and given to the time element in ISO form.</summary>
    [Parameter] public DateTimeOffset? Date { get; set; }

    /// <summary>The date as the host words it ("yesterday"), shown instead of the formatted <see cref="Date"/>.</summary>
    [Parameter] public string? DateText { get; set; }

    /// <summary>The body of the entry.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private bool HasTitle => !string.IsNullOrWhiteSpace(Title);

    private bool HasHeader => HasTitle || !string.IsNullOrWhiteSpace(DateLabel);

    private string? DateTimeValue => Date?.ToString("O", CultureInfo.InvariantCulture);

    private string DateLabel => DateText ?? Date?.ToString("g", CultureInfo.CurrentCulture) ?? string.Empty;
}
