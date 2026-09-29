using System.Globalization;
using Bunit;
using OmniEurope.Blazor.Components;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

public sealed class GanttComponentTests : OmniBunitContext
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    private static string WeekLabel(int week) => "S" + week.ToString(CultureInfo.InvariantCulture);

    /// <summary>Registers a clock whose local date is <paramref name="day"/> (noon UTC, the same date in every time zone the tests run in).</summary>
    private void UseToday(DateOnly day) =>
        Services.AddSingleton<TimeProvider>(new FixedTimeProvider(new DateTimeOffset(day.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero)));

    // Monday 2 March 2026 to Friday 20 March 2026, a group of two and a task without a group.
    private static IReadOnlyList<OmniGanttTask> Tasks() =>
    [
        new() { Id = "kickoff", Title = "Lancement", Start = new(2026, 3, 2), End = new(2026, 3, 2), Progress = 1 },
        new() { Id = "design", Title = "Maquettes", Start = new(2026, 3, 3), End = new(2026, 3, 9), Progress = 0.5, Group = "Conception", DependsOn = ["kickoff"] },
        new() { Id = "review", Title = "Relecture", Start = new(2026, 3, 10), End = new(2026, 3, 12), Group = "Conception", DependsOn = ["design"] },
        new() { Id = "build", Title = "Développement", Start = new(2026, 3, 9), End = new(2026, 3, 20), Progress = 0.25, Group = "Réalisation", DependsOn = ["design", "missing"], ColorIndex = 2 }
    ];

    [Theory]
    [InlineData(OmniCalendarView.Day, "2026-02-28", "2026-03-23")]
    [InlineData(OmniCalendarView.Week, "2026-02-23", "2026-03-30")]
    [InlineData(OmniCalendarView.Month, "2026-03-01", "2026-04-01")]
    public void Range_AddsAMargin_AndStartsOnWholeWeeksOrMonths(OmniCalendarView scale, string start, string end)
    {
        var (from, to) = GanttLayout.Range(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 20), scale);

        Assert.Equal(DateOnly.Parse(start, CultureInfo.InvariantCulture), from);
        Assert.Equal(DateOnly.Parse(end, CultureInfo.InvariantCulture), to);
    }

    [Fact]
    public void Rows_ListUngroupedTasksFirst_ThenEachGroupUnderItsHeading()
    {
        var grouped = GanttLayout.Build(Tasks(), OmniCalendarView.Day, true, new DateOnly(2026, 3, 11), French, WeekLabel);
        var flat = GanttLayout.Build(Tasks(), OmniCalendarView.Day, false, new DateOnly(2026, 3, 11), French, WeekLabel);

        Assert.Equal(["Lancement", "Conception", "Maquettes", "Relecture", "Réalisation", "Développement"], grouped.Rows.Select(row => row.Label));
        Assert.Equal([false, true, false, false, true, false], grouped.Rows.Select(row => row.IsGroup));
        Assert.Equal(["Lancement", "Maquettes", "Relecture", "Développement"], flat.Rows.Select(row => row.Label));

        // The group's summary bar spans its tasks, from 3 to 12 March inclusive: ten days of 32 px.
        var conception = grouped.Rows[1].Bar!;
        Assert.Equal(grouped.X(new DateOnly(2026, 3, 3)), conception.X);
        Assert.Equal(10 * 32, conception.Width);
    }

    [Fact]
    public void Bars_RunFromTheFirstDayToTheEndOfTheLast_WithTheShareDoneFilled()
    {
        var layout = GanttLayout.Build(Tasks(), OmniCalendarView.Day, false, new DateOnly(2026, 3, 11), French, WeekLabel);

        var design = layout.Rows[1].Bar!;
        Assert.Equal(3 * 32, design.X);                      // 28 Feb, 1 and 2 March come first.
        Assert.Equal(7 * 32, design.Width);                  // 3 to 9 March inclusive.
        Assert.Equal(3.5 * 32, design.ProgressWidth);
        Assert.Equal(GanttLayout.HeaderHeight + GanttLayout.RowHeight + ((GanttLayout.RowHeight - GanttLayout.BarHeight) / 2), design.Y);
        Assert.True(design.LabelInside);
        Assert.False(layout.Rows[0].Bar!.LabelInside);        // "Lancement" does not fit in one day.
    }

    [Fact]
    public void Dependencies_RunFromTheEndOfTheTaskWaitedForToTheStartOfTheTaskWaiting()
    {
        var layout = GanttLayout.Build(Tasks(), OmniCalendarView.Day, false, new DateOnly(2026, 3, 11), French, WeekLabel);

        // Three known links; "missing" is ignored.
        Assert.Equal(3, layout.Dependencies.Count);

        // Maquettes ends at x 320, Relecture starts at x 320: too close for a straight elbow, so the
        // arrow steps back along the top of the Relecture row before coming in from the left.
        Assert.Equal("M 320 98 H 328 V 116 H 312 V 134 H 320", layout.Dependencies[1]);

        // Lancement ends on the very x Maquettes starts on, one row down: the same step back.
        Assert.Equal("M 96 62 H 104 V 80 H 88 V 98 H 96", layout.Dependencies[0]);
    }

    [Fact]
    public void Header_NamesMonthsAndWeeks_AndShadesWeekEndsAtTheDayZoom()
    {
        var days = GanttLayout.Build(Tasks(), OmniCalendarView.Day, true, new DateOnly(2026, 3, 11), French, WeekLabel);
        var weeks = GanttLayout.Build(Tasks(), OmniCalendarView.Week, true, new DateOnly(2026, 3, 11), French, WeekLabel);
        var months = GanttLayout.Build(Tasks(), OmniCalendarView.Month, true, new DateOnly(2026, 3, 11), French, WeekLabel);

        Assert.Equal(["février 2026", "mars 2026"], days.TopTier.Select(cell => cell.Label));
        Assert.Equal(23, days.BottomTier.Count);
        Assert.Equal(8, days.WeekEnds.Count);
        Assert.Equal(["S9", "S10", "S11", "S12", "S13"], weeks.BottomTier.Select(cell => cell.Label));
        Assert.Empty(weeks.WeekEnds);
        Assert.Equal(["2026"], months.TopTier.Select(cell => cell.Label));
        Assert.Equal(["mars"], months.BottomTier.Select(cell => cell.Label));
    }

    [Fact]
    public void Today_IsMarkedInTheMiddleOfItsColumn_OnlyWhenTheChartShowsIt()
    {
        var inside = GanttLayout.Build(Tasks(), OmniCalendarView.Day, true, new DateOnly(2026, 3, 11), French, WeekLabel);
        var outside = GanttLayout.Build(Tasks(), OmniCalendarView.Day, true, new DateOnly(2027, 1, 1), French, WeekLabel);

        Assert.Equal(inside.X(new DateOnly(2026, 3, 11)) + 16, inside.TodayX);
        Assert.Null(outside.TodayX);
    }

    [Fact]
    public void Render_GivesEveryBarANamedButton_AndReportsTheTaskClicked()
    {
        UseToday(new DateOnly(2026, 3, 11));
        OmniGanttTask? clicked = null;
        var gantt = Render<OmniGantt>(parameters => parameters
            .Add(component => component.Tasks, Tasks())
            .Add(component => component.Scale, OmniCalendarView.Day)
            .Add(component => component.Culture, French)
            .Add(component => component.SelectedTaskId, "review")
            .Add(component => component.OnTaskClick, task => clicked = task));

        var buttons = gantt.FindAll("button.omni-gantt__hit");
        Assert.Equal(4, buttons.Count);
        var build = buttons.Single(button => button.GetAttribute("data-task-id") == "build");
        Assert.Equal($"Développement, du 9 mars 2026 au 20 mars 2026, {0.25.ToString("P0", French)} fait, après Maquettes", build.GetAttribute("aria-label"));
        Assert.Equal("false", build.GetAttribute("aria-pressed"));
        Assert.Equal("true", buttons.Single(button => button.GetAttribute("data-task-id") == "review").GetAttribute("aria-pressed"));

        build.Click();

        Assert.Equal("build", clicked?.Id);
        Assert.Equal("Diagramme de Gantt", gantt.Find("section").GetAttribute("aria-label"));
        Assert.Equal(3, gantt.FindAll(".omni-gantt__dependencies path").Count);
        Assert.Single(gantt.FindAll(".omni-gantt__today"));
        Assert.Contains("omni-chart-color-2", gantt.FindAll(".omni-gantt__bar")[3].ClassList);
        Assert.DoesNotContain("style=", gantt.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Render_KeepsNamesAndBarsRowForRow()
    {
        UseToday(new DateOnly(2026, 3, 11));
        var gantt = Render<OmniGantt>(parameters => parameters
            .Add(component => component.Tasks, Tasks())
);

        Assert.Equal(
            ["Lancement", "Conception", "Maquettes", "Relecture", "Réalisation", "Développement"],
            gantt.FindAll(".omni-gantt__name").Select(name => name.TextContent));
        Assert.Equal(6, gantt.FindAll(".omni-gantt__grid .omni-gantt__row").Count);
        Assert.Equal(2, gantt.FindAll(".omni-gantt__summary").Count);
    }

    [Fact]
    public void ScalePicker_ZoomsWithoutABinding_AndReportsTheNewScale()
    {
        var reported = new List<OmniCalendarView>();
        var gantt = Render<OmniGantt>(parameters => parameters
            .Add(component => component.Tasks, Tasks())
            .Add(component => component.ScaleChanged, scale => reported.Add(scale)));
        var weekWidth = gantt.Find("svg.omni-gantt__svg").GetAttribute("width");

        gantt.FindAll(".omni-select-bar [role=radio]")[0].Click();

        Assert.Equal([OmniCalendarView.Day], reported);
        Assert.Equal(OmniCalendarView.Day, gantt.Instance.CurrentScale);
        Assert.Contains("omni-gantt--day", gantt.Find("section").ClassList);
        Assert.NotEqual(weekWidth, gantt.Find("svg.omni-gantt__svg").GetAttribute("width"));
    }

    [Fact]
    public void TwoCharts_NameTheirArrowHeadsApart_AndAnEmptyChartSaysSo()
    {
        var first = Render<OmniGantt>(parameters => parameters.Add(component => component.Tasks, Tasks()));
        var second = Render<OmniGantt>(parameters => parameters.Add(component => component.Tasks, Tasks()));
        var empty = Render<OmniGantt>(parameters => parameters.Add(component => component.ShowScalePicker, false));

        Assert.NotEqual(first.Find("marker").Id, second.Find("marker").Id);
        Assert.Equal($"url(#{first.Find("marker").Id})", first.Find(".omni-gantt__dependencies path").GetAttribute("marker-end"));
        Assert.Equal("Aucune tâche à afficher.", empty.Find(".omni-gantt__empty").TextContent);
        Assert.Empty(empty.FindAll(".omni-select-bar"));
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
