namespace OmniEurope.Blazor.Components;

public partial class OmniMonthView
{
    [Parameter] public DateTimeOffset Date { get; set; }
    [Parameter] public IReadOnlyList<OmniSchedulerAppointment> Appointments { get; set; } = Array.Empty<OmniSchedulerAppointment>();
    [Parameter] public System.Globalization.CultureInfo Culture { get; set; } = System.Globalization.CultureInfo.CurrentCulture;
    [Parameter] public string Label { get; set; } = string.Empty;

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localize("MonthViewLabel")
        : Label;

    private IReadOnlyList<string> DayNames
    {
        get
        {
            var names = Culture.DateTimeFormat.AbbreviatedDayNames;
            var first = (int)Culture.DateTimeFormat.FirstDayOfWeek;
            return Enumerable.Range(0, 7).Select(index => names[(first + index) % 7]).ToArray();
        }
    }

    private IReadOnlyList<DateOnly?> CalendarDays
    {
        get
        {
            var first = new DateOnly(Date.Year, Date.Month, 1);
            var leading = (7 + (int)first.DayOfWeek - (int)Culture.DateTimeFormat.FirstDayOfWeek) % 7;
            var days = DateTime.DaysInMonth(Date.Year, Date.Month);
            var cellCount = (int)Math.Ceiling((leading + days) / 7d) * 7;
            return Enumerable.Range(0, cellCount)
                .Select(index => index < leading || index >= leading + days
                    ? (DateOnly?)null
                    : first.AddDays(index - leading))
                .ToArray();
        }
    }
}
