using System.Globalization;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>A date, a time and a date-time picker bound to one set of values, with a summary of what they hold.</summary>
public partial class DatesFormExample
{
    private static readonly DateOnly Floor = new(2020, 1, 1);

    private static readonly DateOnly Ceiling = new(2035, 12, 31);

    private static readonly DateTime AppointmentFloor = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

    private static readonly DateTime AppointmentCeiling = new(2035, 12, 31, 23, 59, 0, DateTimeKind.Unspecified);

    private DateOnly? Date { get; set; } = new(2026, 3, 2);

    private TimeOnly? Start { get; set; } = new(14, 30);

    private DateTime? Appointment { get; set; } = new(2026, 3, 2, 14, 30, 0, DateTimeKind.Unspecified);

    /// <summary>The three bound values, read back so that a choice is visibly applied.</summary>
    private string PickedSummary => string.Format(
        CultureInfo.CurrentCulture,
        "Date: {0}; time: {1}; appointment: {2}",
        Date?.ToString("d", CultureInfo.CurrentCulture) ?? "(no date)",
        Start?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? "(no time)",
        Appointment?.ToString("g", CultureInfo.CurrentCulture) ?? "(no appointment)");
}
