using System.Globalization;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A Gantt chart: one row per task, a bar from its first day to its last with the share done filled
/// in, optional groups with their span, finish-to-start arrows between dependent tasks, a two-tier time
/// header at the day, week or month zoom, and a line on today. Drawn in SVG with geometry attributes
/// only; each bar carries a named button, reached with Tab.
/// </summary>
/// <remarks>
/// Today is the date of the component clock (the host's registered <see cref="TimeProvider"/>,
/// the system clock otherwise) in <see cref="TimeZone"/>. <see cref="Culture"/>, when set, drives dates and texts alike: month
/// names, week numbers, the zoom picker and the names read to a screen reader are all in that culture.
/// </remarks>
public partial class OmniGantt
{
    private readonly string _generatedId = $"omni-gantt-{Guid.NewGuid():N}";
    private OmniCalendarView _view = OmniCalendarView.Week;
    private OmniCalendarView? _lastViewParameter;
    private string? _value;
    private string? _lastValueParameter;

    /// <summary>The tasks, in the order the rows list them (within their group when grouped).</summary>
    [Parameter] public IReadOnlyList<OmniGanttTask> Tasks { get; set; } = Array.Empty<OmniGanttTask>();

    /// <summary>The zoom: one column per day, per week or per month.</summary>
    [Parameter] public OmniCalendarView View { get; set; } = OmniCalendarView.Week;

    /// <summary>Raised with the zoom the reader picks in the zoom picker.</summary>
    [Parameter] public EventCallback<OmniCalendarView> ViewChanged { get; set; }

    /// <summary>Shows the day, week and month picker above the chart; a host with its own toolbar hides it.</summary>
    [Parameter] public bool ShowViewPicker { get; set; } = true;

    /// <summary>Lists the tasks under their <see cref="OmniGanttTask.Group"/>, each group with a summary bar.</summary>
    [Parameter] public bool ShowGroups { get; set; } = true;

    /// <summary>Draws an arrow from each task in <see cref="OmniGanttTask.DependsOn"/> to the task that waits for it.</summary>
    [Parameter] public bool ShowDependencies { get; set; } = true;

    /// <summary>Draws a line down the column of today, the date of the component clock in <see cref="TimeZone"/>.</summary>
    [Parameter] public bool ShowToday { get; set; } = true;

    /// <summary>Raised when a bar is clicked, or activated with Enter or Space.</summary>
    [Parameter] public EventCallback<OmniGanttTask> OnTaskClick { get; set; }

    /// <summary>
    /// The identifier of the chosen task: its bar is outlined, and pressed for a screen reader. Null (the
    /// default) chooses none. Binds with <c>@bind-Value</c>; works without a binding too, the chart then
    /// keeps the task the reader clicked.
    /// </summary>
    [Parameter] public string? Value { get; set; }

    /// <summary>Raised with the identifier of the task the reader clicks (or activates with Enter or Space) when it was not the chosen one.</summary>
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>Accessible name of the chart; null (the default) takes the localized "Gantt chart".</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// The culture of the dates, month names and week numbers, and of every text of the chart. Null (the
    /// default) follows the page: dates in the current culture, texts in the current UI culture.
    /// </summary>
    [Parameter] public CultureInfo? Culture { get; set; }

    /// <summary>
    /// The time zone that decides which day is today (the line on today): the component clock is an instant,
    /// and the day it falls on depends on where the reader is. The local zone of the process by default, which
    /// on a server is the server's, not the reader's: a server-rendered chart sets it, as <see cref="OmniScheduler"/> does.
    /// </summary>
    [Parameter] public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;

    /// <summary>The culture dates are written in: <see cref="Culture"/>, else the current culture.</summary>
    private CultureInfo Formats => Culture ?? CultureInfo.CurrentCulture;

    internal GanttLayout Layout { get; private set; } =
        GanttLayout.Build([], OmniCalendarView.Week, true, new DateOnly(2000, 1, 3), CultureInfo.InvariantCulture, week => week.ToString(CultureInfo.InvariantCulture));

    private string BaseId => Id ?? _generatedId;

    private string ArrowId => $"{BaseId}-arrow";

    private string ArrowReference => $"url(#{ArrowId})";

    /// <summary>The zoom drawn: the parameter, until the reader picks another one.</summary>
    internal OmniCalendarView CurrentView => _view;

    private string ViewCss => $"omni-gantt--{_view.ToString().ToLowerInvariant()}";

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Text("GanttLabel") : Label;

    private string ViewLabel => Text("GanttScale");

    private string EmptyText => Text("GanttEmpty");

    private string TaskColumnText => Text("GanttTaskColumn");

    private string TodayText => Text("Today");

    private IReadOnlyList<OmniOption<OmniCalendarView>> ViewOptions =>
    [
        new(OmniCalendarView.Day, Text("GanttScaleDay")),
        new(OmniCalendarView.Week, Text("GanttScaleWeek")),
        new(OmniCalendarView.Month, Text("GanttScaleMonth"))
    ];

    /// <summary>A library text in <see cref="Culture"/> when it is set, else in the current UI culture.</summary>
    private string Text(string key, params object[] arguments) => CultureText.In(Culture, () => Localize(key, arguments));

    /// <summary>
    /// Adopts a new <see cref="View"/> from the host (a zoom chosen by the user stays while the host
    /// passes the same value), then lays the tasks out again, today taken from the component clock.
    /// </summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(TimeZone);
        if (_lastViewParameter is not { } last || last != View)
        {
            _lastViewParameter = View;
            _view = View;
        }

        if (!string.Equals(_lastValueParameter, Value, StringComparison.Ordinal))
        {
            _lastValueParameter = Value;
            _value = Value;
        }

        Layout = BuildLayout();
    }

    private GanttLayout BuildLayout() =>
        GanttLayout.Build(Tasks, _view, ShowGroups, DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(Clock.GetUtcNow(), TimeZone).Date), Formats, WeekLabel);

    /// <summary>The short name of an ISO week in the week zoom header: "S12" in French, "W12" in English.</summary>
    private string WeekLabel(int week) => Text("GanttWeekNumber", week.ToString(Formats));

    // The view bar is an OmniSelectBar, which reports a choice only when it differs from its value.
    private async Task SetViewAsync(OmniCalendarView view)
    {
        // Works without a binding too: the chart then keeps the zoom the reader picked.
        _view = view;
        Layout = BuildLayout();
        await ViewChanged.InvokeAsync(view);
    }

    private async Task ClickTaskAsync(OmniGanttTask task)
    {
        if (!string.Equals(_value, task.Id, StringComparison.Ordinal))
        {
            _value = task.Id;
            await ValueChanged.InvokeAsync(task.Id);
        }

        await OnTaskClick.InvokeAsync(task);
    }

    private bool IsSelected(OmniGanttTask task) => string.Equals(_value, task.Id, StringComparison.Ordinal);

    // Pressed only means something when the host listens for clicks or for the choice; otherwise the bar is a plain button.
    private string? Pressed(OmniGanttTask task) =>
        OnTaskClick.HasDelegate || ValueChanged.HasDelegate ? (IsSelected(task) ? "true" : "false") : null;

    private string TaskCss(OmniGanttTask task) => CssClassBuilder.Combine(
    [
        "omni-gantt__bar",
        ChartColor.Class(task.ColorIndex),
        IsSelected(task) ? "omni-gantt__bar--selected" : null
    ]);

    private static string NameCss(GanttRow row) => row.IsGroup ? "omni-gantt__name omni-gantt__name--group" : "omni-gantt__name";

    private static string RowCss(GanttRow row, int index) =>
        row.IsGroup ? "omni-gantt__row omni-gantt__row--group" : index % 2 == 1 ? "omni-gantt__row omni-gantt__row--odd" : "omni-gantt__row";

    private static string LabelCss(GanttBar bar) => bar.LabelInside ? "omni-gantt__label omni-gantt__label--inside" : "omni-gantt__label";

    // Inside the bar when the title fits, just past its end otherwise.
    private static double LabelX(GanttBar bar) => bar.LabelInside ? bar.X + 6 : bar.X + bar.Width + 6;

    /// <summary>The name a screen reader gives a bar: title, first and last day, share done, and what it waits for.</summary>
    internal string Describe(OmniGanttTask task)
    {
        var progress = double.IsFinite(task.Progress) ? Math.Clamp(task.Progress, 0, 1) : 0;
        var text = Text(
            "GanttTaskDescription",
            task.Title,
            task.Start.ToString("d MMMM yyyy", Formats),
            task.End.ToString("d MMMM yyyy", Formats),
            progress.ToString("P0", Formats));
        var before = task.DependsOn
            .Select(id => Tasks.FirstOrDefault(other => string.Equals(other.Id, id, StringComparison.Ordinal))?.Title)
            .OfType<string>()
            .ToArray();
        return before.Length == 0 ? text : $"{text}, {Text("GanttTaskAfter", string.Join(", ", before))}";
    }

    private static string N(double value) => GanttLayout.N(value);
}
