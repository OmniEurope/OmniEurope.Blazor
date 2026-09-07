namespace OmniEurope.Blazor.Components;

public partial class OmniDayView
{
    [Parameter] public DateTimeOffset Date { get; set; }
    [Parameter] public IReadOnlyList<OmniSchedulerAppointment> Appointments { get; set; } = Array.Empty<OmniSchedulerAppointment>();
    [Parameter] public System.Globalization.CultureInfo Culture { get; set; } = System.Globalization.CultureInfo.CurrentCulture;
    [Parameter] public string Label { get; set; } = string.Empty;
    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localize("DayViewLabel")
        : Label;
    private IEnumerable<OmniSchedulerAppointment> DayItems => Appointments.Where(item => item.Start.Date == Date.Date);
}
