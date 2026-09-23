using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Editor behind the DateRange filter type: a start and an end picker, dates only or dates and
/// times. The value it reads and writes is the encoded range of <see cref="OmniDataGridDateRange"/>,
/// so the filter travels as the same single string as any other until the grid resolves it.
/// </summary>
public partial class OmniDataGridFilterDateRange
{
    [Parameter]
    public string Value { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    /// <summary>Picks the hours as well as the day.</summary>
    [Parameter]
    public bool IncludesTime { get; set; }

    private string InputType => IncludesTime ? "datetime-local" : "date";

    private string Start => Adapt(OmniDataGridDateRange.Split(Value).Start);

    private string End => Adapt(OmniDataGridDateRange.Split(Value).End);

    // Each picker is bounded by the other so the two cannot cross; the browser compares on the
    // picker's own format, so a date bound is cut to its day.
    private string? StartLimit => string.IsNullOrEmpty(Start) ? null : Start;

    private string? EndLimit => string.IsNullOrEmpty(End) ? null : End;

    private Task OnStartChangedAsync(ChangeEventArgs args) =>
        ValueChanged.InvokeAsync(OmniDataGridDateRange.Join(args.Value?.ToString(), End));

    private Task OnEndChangedAsync(ChangeEventArgs args) =>
        ValueChanged.InvokeAsync(OmniDataGridDateRange.Join(Start, args.Value?.ToString()));

    /// <summary>
    /// A value saved with the other picker shape still shows: a date picker keeps the day of a
    /// date and time, a date and time picker opens a bare day at midnight.
    /// </summary>
    private string Adapt(string side)
    {
        if (string.IsNullOrEmpty(side))
        {
            return string.Empty;
        }

        var hasTime = side.Contains('T', StringComparison.Ordinal);
        if (IncludesTime)
        {
            return hasTime ? side : $"{side}T00:00";
        }

        return hasTime ? side[..side.IndexOf('T', StringComparison.Ordinal)] : side;
    }
}
