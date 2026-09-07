namespace OmniEurope.Blazor.Components;

public partial class OmniWeekView
{
    [Parameter] public DateTimeOffset Date { get; set; }
    [Parameter] public IReadOnlyList<OmniSchedulerAppointment> Appointments { get; set; } = Array.Empty<OmniSchedulerAppointment>();
    [Parameter] public System.Globalization.CultureInfo Culture { get; set; } = System.Globalization.CultureInfo.CurrentCulture;
    [Parameter] public string Label { get; set; } = string.Empty;
    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localize("WeekViewLabel")
        : Label;
    private DateTimeOffset WeekStart => Date.AddDays(-((7 + (int)Date.DayOfWeek - (int)Culture.DateTimeFormat.FirstDayOfWeek) % 7));
}
