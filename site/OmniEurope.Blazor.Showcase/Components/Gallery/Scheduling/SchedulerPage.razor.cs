using System.Globalization;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Scheduling;

/// <summary>The Scheduler page: the view, culture, time grid, weekend and moves of a scheduler, with the date it is on.</summary>
public partial class SchedulerPage
{
    private const string French = "fr-FR";
    private const string English = "en-US";
    private const string German = "de-DE";

    [Inject]
    private IStringLocalizer<SchedulingStrings> Text { get; set; } = default!;

    private DateTimeOffset Anchor { get; set; } = Today;

    private OmniCalendarView View { get; set; } = OmniCalendarView.Week;

    /// <summary>The culture of the scheduler: the language of the page when the scheduler is offered in it.</summary>
    private string CultureName { get; set; } = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
    {
        "fr" => French,
        "de" => German,
        _ => English
    };

    private bool MarkPastSlots { get; set; } = true;

    private bool IncludeWeekend { get; set; } = true;

    private bool TimeGrid { get; set; } = true;

    private bool Movable { get; set; } = true;

    private static DateTimeOffset Today => new(DateTime.Today);

    private CultureInfo Culture => CultureInfo.GetCultureInfo(CultureName);

    private IReadOnlyList<OmniOption<OmniCalendarView>> ViewOptions =>
    [
        new(OmniCalendarView.Day, Text["SchedulerViewDay"]),
        new(OmniCalendarView.Week, Text["SchedulerViewWeek"]),
        new(OmniCalendarView.Month, Text["SchedulerViewMonth"])
    ];

    private IReadOnlyList<OmniOption<string>> CultureOptions =>
    [
        new(French, Text["SchedulerCultureFrench"]),
        new(English, Text["SchedulerCultureEnglish"]),
        new(German, Text["SchedulerCultureGerman"])
    ];

    private void BackToToday() => Anchor = Today;
}
