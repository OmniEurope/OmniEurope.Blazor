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
/// Today is the local date of the component clock (the host's registered <see cref="TimeProvider"/>,
/// the system clock otherwise). <see cref="Culture"/>, when set, drives dates and texts alike: month
/// names, week numbers, the zoom picker and the names read to a screen reader are all in that culture.
/// </remarks>
public partial class OmniGantt
{
    private readonly string _generatedId = $"omni-gantt-{Guid.NewGuid():N}";
    private OmniCalendarView _scale = OmniCalendarView.Week;
    private OmniCalendarView? _lastScaleParameter;

    /// <summary>The tasks, in the order the rows list them (within their group when grouped).</summary>
    [Parameter] public IReadOnlyList<OmniGanttTask> Tasks { get; set; } = Array.Empty<OmniGanttTask>();

    /// <summary>The zoom: one column per day, per week or per month.</summary>
    [Parameter] public OmniCalendarView Scale { get; set; } = OmniCalendarView.Week;

    /// <summary>Raised with the zoom the reader picks in the zoom picker.</summary>
    [Parameter] public EventCallback<OmniCalendarView> ScaleChanged { get; set; }

    /// <summary>Shows the day, week and month picker above the chart; a host with its own toolbar hides it.</summary>
    [Parameter] public bool ShowScalePicker { get; set; } = true;

    /// <summary>Lists the tasks under their <see cref="OmniGanttTask.Group"/>, each group with a summary bar.</summary>
    [Parameter] public bool ShowGroups { get; set; } = true;

    /// <summary>Draws an arrow from each task in <see cref="OmniGanttTask.DependsOn"/> to the task that waits for it.</summary>
    [Parameter] public bool ShowDependencies { get; set; } = true;

    /// <summary>Draws a line down the column of today, the local date of the component clock.</summary>
    [Parameter] public bool ShowToday { get; set; } = true;

    /// <summary>Raised when a bar is clicked, or activated with Enter or Space.</summary>
    [Parameter] public EventCallback<OmniGanttTask> OnTaskClick { get; set; }

    /// <summary>The task shown as chosen, by identifier: its bar is outlined, and pressed for a screen reader.</summary>
    [Parameter] public string? SelectedTaskId { get; set; }

    /// <summary>Accessible name of the chart; null (the default) takes the localized "Gantt chart".</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// The culture of the dates, month names and week numbers, and of every text of the chart. Null (the
    /// default) follows the page: dates in the current culture, texts in the current UI culture.
    /// </summary>
    [Parameter] public CultureInfo? Culture { get; set; }

    /// <summary>The culture dates are written in: <see cref="Culture"/>, else the current culture.</summary>
    private CultureInfo Formats => Culture ?? CultureInfo.CurrentCulture;

    internal GanttLayout Layout { get; private set; } =
        GanttLayout.Build([], OmniCalendarView.Week, true, new DateOnly(2000, 1, 3), CultureInfo.InvariantCulture, week => week.ToString(CultureInfo.InvariantCulture));

    private string BaseId => Id ?? _generatedId;

    private string ArrowId => $"{BaseId}-arrow";

    private string ArrowReference => $"url(#{ArrowId})";

    /// <summary>The zoom drawn: the parameter, until the reader picks another one.</summary>
    internal OmniCalendarView CurrentScale => _scale;

    private string ScaleCss => $"omni-gantt--{_scale.ToString().ToLowerInvariant()}";

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Text("GanttLabel") : Label;

    private string ScaleLabel => Text("GanttScale");

    private string EmptyText => Text("GanttEmpty");

    private string TaskColumnText => Text("GanttTaskColumn");

    private string TodayText => Text("Today");

    private IReadOnlyList<OmniOption<OmniCalendarView>> ScaleOptions =>
    [
        new(OmniCalendarView.Day, Text("GanttScaleDay")),
        new(OmniCalendarView.Week, Text("GanttScaleWeek")),
        new(OmniCalendarView.Month, Text("GanttScaleMonth"))
    ];

    /// <summary>A library text in <see cref="Culture"/> when it is set, else in the current UI culture.</summary>
    private string Text(string key, params object[] arguments) => CultureText.In(Culture, () => Localize(key, arguments));

    /// <summary>
    /// Adopts a new <see cref="Scale"/> from the host (a zoom chosen by the user stays while the host
    /// passes the same value), then lays the tasks out again, today taken from the component clock.
    /// </summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (_lastScaleParameter is not { } last || last != Scale)
        {
            _lastScaleParameter = Scale;
            _scale = Scale;
        }

        Layout = BuildLayout();
    }

    private GanttLayout BuildLayout() =>
        GanttLayout.Build(Tasks, _scale, ShowGroups, DateOnly.FromDateTime(Clock.GetLocalNow().Date), Formats, WeekLabel);

    /// <summary>The short name of an ISO week in the week zoom header: "S12" in French, "W12" in English.</summary>
    private string WeekLabel(int week) => Text("GanttWeekNumber", week.ToString(Formats));

    // The scale bar is an OmniSelectBar, which reports a choice only when it differs from its value.
    private async Task SetScaleAsync(OmniCalendarView scale)
    {
        // Works without a binding too: the chart then keeps the zoom the reader picked.
        _scale = scale;
        Layout = BuildLayout();
        await ScaleChanged.InvokeAsync(scale);
    }

    private Task ClickTaskAsync(OmniGanttTask task) => OnTaskClick.InvokeAsync(task);

    private bool IsSelected(OmniGanttTask task) => string.Equals(SelectedTaskId, task.Id, StringComparison.Ordinal);

    // Pressed only means something when the host listens for clicks; otherwise the bar is a plain button.
    private string? Pressed(OmniGanttTask task) => OnTaskClick.HasDelegate ? (IsSelected(task) ? "true" : "false") : null;

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
