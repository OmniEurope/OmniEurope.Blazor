using System.Globalization;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Encoding and meaning of a <see cref="OmniDataGridColumnFilterType.DateRange"/> filter. The value
/// keeps what the user picked, <c>start/end</c> in ISO 8601 interval notation with either side
/// optional (<c>2026-08-24/</c>, <c>/2026-08-30</c>, <c>2026-08-24T10:00/2026-08-24T12:30</c>), so
/// the editor shows it back as typed. <see cref="Resolve"/> is the only place that turns it into
/// bounds: a day alone covers the whole day, an end day is included up to its last instant, and
/// a picked time is included up to the end of its minute.
/// </summary>
public static class OmniDataGridDateRange
{
    private const char Separator = '/';

    private static readonly string[] Formats = ["yyyy-MM-dd", "yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss"];

    /// <summary>The encoded value of a range; empty when neither side is set.</summary>
    public static string Join(string? start, string? end)
    {
        var from = start?.Trim() ?? string.Empty;
        var to = end?.Trim() ?? string.Empty;
        return from.Length == 0 && to.Length == 0 ? string.Empty : $"{from}{Separator}{to}";
    }

    /// <summary>The two sides as picked, each possibly empty.</summary>
    public static (string Start, string End) Split(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return (string.Empty, string.Empty);
        }

        var index = value.IndexOf(Separator, StringComparison.Ordinal);
        return index < 0
            ? (value.Trim(), string.Empty)
            : (value[..index].Trim(), value[(index + 1)..].Trim());
    }

    /// <summary>
    /// The inclusive start and the exclusive end the value stands for. A side that is empty or
    /// unreadable is null, so a half-typed range narrows by the side that is complete.
    /// </summary>
    public static (DateTime? Start, DateTime? EndExclusive) Resolve(string? value)
    {
        var (start, end) = Split(value);
        DateTime? from = TryParse(start, out var startValue, out _) ? startValue : null;
        DateTime? to = TryParse(end, out var endValue, out var endHasTime)
            ? endHasTime
                ? endValue.AddSeconds(-endValue.Second).AddMinutes(1)
                : endValue.Date.AddDays(1)
            : null;
        return (from, to);
    }

    /// <summary>
    /// Whether a cell value lies in the range. Dates are compared on the clock time the cell shows:
    /// a <see cref="DateTimeOffset"/> by its own date and time, a <see cref="DateOnly"/> as its day.
    /// A value that is not a date matches no active range.
    /// </summary>
    public static bool Contains(string? value, object? candidate)
    {
        var (start, endExclusive) = Resolve(value);
        if (start is null && endExclusive is null)
        {
            return true;
        }

        DateTime? moment = candidate switch
        {
            DateTime dateTime => dateTime,
            DateTimeOffset offset => offset.DateTime,
            DateOnly day => day.ToDateTime(TimeOnly.MinValue),
            _ => null
        };
        return moment is { } at
            && (start is null || at >= start)
            && (endExclusive is null || at < endExclusive);
    }

    /// <summary>How a bound travels to a remote loader: invariant, sortable, without offset.</summary>
    public static string FormatBound(DateTime value) =>
        value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

    private static bool TryParse(string text, out DateTime value, out bool hasTime)
    {
        hasTime = text.Contains('T', StringComparison.Ordinal);
        return DateTime.TryParseExact(text, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }
}
