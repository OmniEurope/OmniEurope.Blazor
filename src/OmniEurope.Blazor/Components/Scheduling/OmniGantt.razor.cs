using System.Globalization;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A Gantt chart: one row per task, a bar from its first day to its last with the share done filled
/// in, optional groups with their span, finish-to-start arrows between dependent tasks, a two-tier time
/// header at the day, week or month zoom, and a line on today. Drawn in SVG with geometry attributes
/// only; each bar carries a named button, reached with Tab.
/// </summary>
public partial class OmniGantt
{
    private readonly string _generatedId = $"omni-gantt-{Guid.NewGuid():N}";
    private OmniGanttScale _scale = OmniGanttScale.Week;
    private OmniGanttScale? _lastScaleParameter;

    /// <summary>The tasks, in the order the rows list them (within their group when grouped).</summary>
    [Parameter] public IReadOnlyList<OmniGanttTask> Tasks { get; set; } = Array.Empty<OmniGanttTask>();

    /// <summary>The zoom: one column per day, per week or per month.</summary>
    [Parameter] public OmniGanttScale Scale { get; set; } = OmniGanttScale.Week;

    [Parameter] public EventCallback<OmniGanttScale> ScaleChanged { get; set; }

    /// <summary>Shows the day, week and month picker above the chart; a host with its own toolbar hides it.</summary>
    [Parameter] public bool ShowScalePicker { get; set; } = true;

    /// <summary>Lists the tasks under their <see cref="OmniGanttTask.Group"/>, each group with a summary bar.</summary>
    [Parameter] public bool ShowGroups { get; set; } = true;

    /// <summary>Draws an arrow from each task in <see cref="OmniGanttTask.DependsOn"/> to the task that waits for it.</summary>
    [Parameter] public bool ShowDependencies { get; set; } = true;

    /// <summary>Draws a line down the column of <see cref="Today"/>.</summary>
    [Parameter] public bool ShowToday { get; set; } = true;

    /// <summary>The day the today line marks; null takes the current date.</summary>
    [Parameter] public DateOnly? Today { get; set; }

    /// <summary>Raised when a bar is clicked, or activated with Enter or Space.</summary>
    [Parameter] public EventCallback<OmniGanttTask> TaskClicked { get; set; }

    /// <summary>The task shown as chosen, by identifier: its bar is outlined, and pressed for a screen reader.</summary>
    [Parameter] public string? SelectedTaskId { get; set; }

    /// <summary>Accessible name of the chart; the localized GanttLabel by default.</summary>
    [Parameter] public string Label { get; set; } = string.Empty;

    /// <summary>The culture month names, week numbers and dates are written in.</summary>
    [Parameter] public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;

    internal GanttLayout Layout { get; private set; } =
        GanttLayout.Build([], OmniGanttScale.Week, true, new DateOnly(2000, 1, 3), CultureInfo.InvariantCulture);

    private string BaseId => Id ?? _generatedId;

    private string ArrowId => $"{BaseId}-arrow";

    private string ArrowReference => $"url(#{ArrowId})";

    /// <summary>The zoom drawn: the parameter, until the reader picks another one.</summary>
    internal OmniGanttScale CurrentScale => _scale;

    private string ScaleCss => $"omni-gantt--{_scale.ToString().ToLowerInvariant()}";

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Localize("GanttLabel") : Label;

    private string ScaleLabel => Localize("GanttScale");

    private string EmptyText => Localize("GanttEmpty");

    private string TaskColumnText => Localize("GanttTaskColumn");

    private string TodayText => Localize("Today");

    private IReadOnlyList<OmniOption<OmniGanttScale>> ScaleOptions =>
    [
        new(OmniGanttScale.Day, Localize("GanttScaleDay")),
        new(OmniGanttScale.Week, Localize("GanttScaleWeek")),
        new(OmniGanttScale.Month, Localize("GanttScaleMonth"))
    ];

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (_lastScaleParameter != Scale)
        {
            _lastScaleParameter = Scale;
            _scale = Scale;
        }

        Layout = BuildLayout();
    }

    private GanttLayout BuildLayout() =>
        GanttLayout.Build(Tasks, _scale, ShowGroups, Today ?? DateOnly.FromDateTime(DateTime.Today), Culture);

    private async Task SetScaleAsync(OmniGanttScale scale)
    {
        if (scale == _scale)
        {
            return;
        }

        // Works without a binding too: the chart then keeps the zoom the reader picked.
        _scale = scale;
        Layout = BuildLayout();
        await ScaleChanged.InvokeAsync(scale);
    }

    private Task OnTaskClickedAsync(OmniGanttTask task) => TaskClicked.InvokeAsync(task);

    private bool IsSelected(OmniGanttTask task) => string.Equals(SelectedTaskId, task.Id, StringComparison.Ordinal);

    // Pressed only means something when the host listens for clicks; otherwise the bar is a plain button.
    private string? Pressed(OmniGanttTask task) => TaskClicked.HasDelegate ? (IsSelected(task) ? "true" : "false") : null;

    private string TaskCss(OmniGanttTask task) => CssClassBuilder.Combine(
    [
        "omni-gantt__bar",
        $"omni-chart-color-{Math.Abs(task.ColorIndex) % 8}",
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
        var text = Localize(
            "GanttTaskDescription",
            task.Title,
            task.Start.ToString("d MMMM yyyy", Culture),
            task.End.ToString("d MMMM yyyy", Culture),
            progress.ToString("P0", Culture));
        var before = task.DependsOn
            .Select(id => Tasks.FirstOrDefault(other => string.Equals(other.Id, id, StringComparison.Ordinal))?.Title)
            .OfType<string>()
            .ToArray();
        return before.Length == 0 ? text : $"{text}, {Localize("GanttTaskAfter", string.Join(", ", before))}";
    }

    private static string N(double value) => GanttLayout.N(value);
}
