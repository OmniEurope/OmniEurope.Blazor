namespace OmniEurope.Blazor.Components;

public partial class OmniTimelineItem
{
    /// <summary>
    /// The item's heading. Optional: an entry whose content already names itself leaves it empty and
    /// gets no header at all, rather than an empty one.
    /// </summary>
    [Parameter] public string Title { get; set; } = string.Empty;

    [Parameter] public DateTimeOffset? Date { get; set; }

    [Parameter] public string? DateText { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    private bool HasHeader => !string.IsNullOrWhiteSpace(Title) || !string.IsNullOrWhiteSpace(DateLabel);

    private string? DateTimeValue => Date?.ToString("O", System.Globalization.CultureInfo.InvariantCulture);

    private string DateLabel => DateText ?? Date?.ToString("g", System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty;
}
