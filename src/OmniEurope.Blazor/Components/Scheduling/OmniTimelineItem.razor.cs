namespace OmniEurope.Blazor.Components;

public partial class OmniTimelineItem
{
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;
    [Parameter] public DateTimeOffset? Date { get; set; }
    [Parameter] public string? DateText { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    private string? DateTimeValue => Date?.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
    private string DateLabel => DateText ?? Date?.ToString("g", System.Globalization.CultureInfo.CurrentCulture) ?? string.Empty;
}
