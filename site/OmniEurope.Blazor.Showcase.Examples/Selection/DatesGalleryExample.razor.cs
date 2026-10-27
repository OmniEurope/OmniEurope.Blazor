using System.Globalization;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>Date pickers with no value, read only, disabled, to the second, and bounded to a week.</summary>
public partial class DatesGalleryExample
{
    private static readonly DateTime WeekStart = DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek + 6) % 7));

    private static readonly DateTime WeekEnd = WeekStart.AddDays(5).AddMinutes(-1);

    private DateOnly? EmptyDate { get; set; }

    private DateOnly? FrozenDate { get; set; } = new DateOnly(2026, 1, 1);

    private DateTime? PreciseMoment { get; set; } = DateTime.Today.AddHours(14).AddMinutes(5).AddSeconds(42);

    private DateTime? MeetingMoment { get; set; } = WeekStart.AddHours(10);

    private static string WeekText => string.Format(
        CultureInfo.CurrentCulture,
        "Minimum: {0}. Maximum: {1}.",
        WeekStart.ToString("g", CultureInfo.CurrentCulture),
        WeekEnd.ToString("g", CultureInfo.CurrentCulture));

    private static string FormatIso(DateOnly? value) =>
        value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "null";

    private static string FormatIso(DateTime? value) =>
        value?.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture) ?? "null";
}
