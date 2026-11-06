using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>The blocks of the time grid: their length, their lanes, their overlaps, and a grid crossing midnight.</summary>
public sealed class SchedulerGridBlockTests : OmniBunitContext
{
    private static readonly DateTimeOffset Monday = new(2026, 6, 8, 0, 0, 0, TimeSpan.Zero);

    private IRenderedComponent<OmniScheduler> RenderGrid(
        IReadOnlyList<OmniSchedulerAppointment> items,
        TimeOnly start,
        TimeOnly end,
        OmniCalendarView view = OmniCalendarView.Day,
        Action<ComponentParameterCollectionBuilder<OmniScheduler>>? more = null) =>
        Render<OmniScheduler>(parameters =>
        {
            parameters
                .Add(component => component.Date, Monday)
                .Add(component => component.View, view)
                .Add(component => component.TimeZone, TimeZoneInfo.Utc)
                .Add(component => component.Culture, CultureInfo.GetCultureInfo("fr-FR"))
                .Add(component => component.DayStart, start)
                .Add(component => component.DayEnd, end)
                .Add(component => component.SlotDuration, TimeSpan.FromMinutes(30))
                .Add(component => component.Items, items);
            more?.Invoke(parameters);
        });

    private static IElement Block(IRenderedComponent<OmniScheduler> scheduler, string id) =>
        scheduler.Find($"[data-omni-scheduler-appointment='{id}']");

    /// <summary>The row of the cell a block starts in, from the first slot.</summary>
    private static int Row(IElement block)
    {
        var row = block.ParentElement!.ParentElement!;
        return row.ParentElement!.Children.Index(row);
    }

    /// <summary>The column of the cell a block starts in, the column of the times being the first.</summary>
    private static int Column(IElement block)
    {
        var cell = block.ParentElement!;
        return cell.ParentElement!.Children.Index(cell);
    }

    private static IElement[] BlocksByColumn(IRenderedComponent<OmniScheduler> scheduler, string id) =>
        [.. scheduler.FindAll($"[data-omni-scheduler-appointment='{id}']").OrderBy(Column)];

    private static int Hundredths(IElement block, string name) => block.ClassList
        .Where(item => item.StartsWith($"omni-scheduler__{name}-", StringComparison.Ordinal))
        .Select(item => item.Split('-'))
        .Sum(parts => int.Parse(parts[^1], CultureInfo.InvariantCulture) * (int)Math.Pow(10, int.Parse(parts[^2], CultureInfo.InvariantCulture)));

    [Fact]
    public void Block_StartsInItsCell_AndCarriesItsOffsetAndLengthInHundredthsOfASlot()
    {
        var scheduler = RenderGrid(
            [new("run", "Build", Monday.AddHours(9).AddMinutes(15), Monday.AddHours(10).AddMinutes(45))],
            new TimeOnly(8, 0),
            new TimeOnly(12, 0));

        var block = Block(scheduler, "run");
        Assert.Equal(2, Row(block));
        Assert.Equal(50, Hundredths(block, "at"));
        Assert.Equal(300, Hundredths(block, "len"));
        Assert.DoesNotContain("style=", scheduler.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Block_IsCutAtTheEdgesOfTheGrid()
    {
        var scheduler = RenderGrid(
            [
                new("before", "Avant", Monday.AddHours(7), Monday.AddHours(9)),
                new("after", "Après", Monday.AddHours(11), Monday.AddHours(14))
            ],
            new TimeOnly(8, 0),
            new TimeOnly(12, 0));

        Assert.Equal(0, Row(Block(scheduler, "before")));
        Assert.Equal(200, Hundredths(Block(scheduler, "before"), "len"));
        Assert.Equal(6, Row(Block(scheduler, "after")));
        Assert.Equal(200, Hundredths(Block(scheduler, "after"), "len"));
    }

    [Theory]
    [InlineData(6, 7)]
    [InlineData(13, 14)]
    public void AppointmentWhollyOutsideTheGrid_KeepsAShortBlockInTheNearestSlot(int startHour, int endHour)
    {
        var scheduler = RenderGrid(
            [new("outside", "Hors grille", Monday.AddHours(startHour), Monday.AddHours(endHour))],
            new TimeOnly(8, 0),
            new TimeOnly(12, 0));

        var block = Block(scheduler, "outside");
        Assert.Equal(startHour < 8 ? 0 : 7, Row(block));
        Assert.InRange(Hundredths(block, "len"), 0, 50);
    }

    [Fact]
    public void AppointmentsSharingTheirTime_StandSideBySide_AndTheOneAfterTheChainTakesTheWholeWidth()
    {
        var scheduler = RenderGrid(
            [
                new("a", "Premier", Monday.AddHours(8), Monday.AddHours(10)),
                new("b", "Deuxième", Monday.AddHours(9), Monday.AddHours(11)),
                new("c", "Troisième", Monday.AddHours(10), Monday.AddHours(11)),
                new("d", "Après", Monday.AddHours(11), Monday.AddHours(12))
            ],
            new TimeOnly(8, 0),
            new TimeOnly(12, 0));

        Assert.Contains("omni-scheduler__lanes-2", Block(scheduler, "a").ClassList);
        Assert.Contains("omni-scheduler__lanes-2", Block(scheduler, "b").ClassList);
        Assert.Contains("omni-scheduler__lane-1", Block(scheduler, "b").ClassList);
        // The third starts when the first ends: it takes the first lane again, in the same chain.
        Assert.Contains("omni-scheduler__lanes-2", Block(scheduler, "c").ClassList);
        Assert.DoesNotContain("omni-scheduler__lane-1", Block(scheduler, "c").ClassList);
        Assert.DoesNotContain(Block(scheduler, "d").ClassList, item => item.StartsWith("omni-scheduler__lane", StringComparison.Ordinal));
    }

    [Fact]
    public void Overlap_IsMarkedAndNamedToAssistiveTechnology_ButNotOnTouchingAppointments()
    {
        var scheduler = RenderGrid(
            [
                new("a", "Compilation", Monday.AddHours(8), Monday.AddHours(10)),
                new("b", "Tests", Monday.AddHours(9), Monday.AddHours(10)),
                new("c", "Paquet", Monday.AddHours(10), Monday.AddHours(11))
            ],
            new TimeOnly(8, 0),
            new TimeOnly(12, 0));

        Assert.Contains("omni-scheduler__appointment--overlapping", Block(scheduler, "a").ClassList);
        Assert.Equal("Chevauche Tests", Block(scheduler, "a").QuerySelector(".omni-visually-hidden")!.TextContent);
        Assert.Equal("Chevauche Compilation", Block(scheduler, "b").QuerySelector(".omni-visually-hidden")!.TextContent);
        Assert.DoesNotContain("omni-scheduler__appointment--overlapping", Block(scheduler, "c").ClassList);
        Assert.Null(Block(scheduler, "c").QuerySelector(".omni-visually-hidden"));
    }

    private static readonly OmniSchedulerAppointment[] Sharing =
    [
        new("a", "Compilation", Monday.AddHours(8), Monday.AddHours(10)),
        new("b", "Tests", Monday.AddHours(9), Monday.AddHours(10))
    ];

    /// <summary>The lane of a block and the lanes of its chain, from its classes (lane 0 and one lane write none).</summary>
    private static (int Lane, int Lanes) LanesOf(IElement block)
    {
        int Read(string prefix, int absent) => block.ClassList
            .Where(item => item.StartsWith(prefix, StringComparison.Ordinal))
            .Select(item => int.Parse(item[prefix.Length..], CultureInfo.InvariantCulture))
            .DefaultIfEmpty(absent)
            .Single();

        return (Read("omni-scheduler__lane-", 0), Read("omni-scheduler__lanes-", 1));
    }

    [Fact]
    public void ShowOverlapsOff_DropsTheMarkAndTheText_ButKeepsTheLanes()
    {
        var shown = RenderGrid(Sharing, new TimeOnly(8, 0), new TimeOnly(12, 0));
        var hidden = RenderGrid(Sharing, new TimeOnly(8, 0), new TimeOnly(12, 0), more: parameters => parameters.Add(component => component.ShowOverlaps, false));

        foreach (var id in new[] { "a", "b" })
        {
            Assert.DoesNotContain("omni-scheduler__appointment--overlapping", Block(hidden, id).ClassList);
            Assert.Null(Block(hidden, id).QuerySelector(".omni-visually-hidden"));
            Assert.DoesNotContain("Chevauche", Block(hidden, id).TextContent, StringComparison.Ordinal);
            Assert.Equal(LanesOf(Block(shown, id)), LanesOf(Block(hidden, id)));
        }

        Assert.Equal((1, 2), LanesOf(Block(hidden, "b")));
    }

    [Fact]
    public void OverlapText_ReplacesTheSchedulersOwn_AndMarksTheAppointment_EvenWithShowOverlapsOff()
    {
        foreach (var showOverlaps in new[] { true, false })
        {
            var scheduler = RenderGrid(
                [Sharing[0] with { OverlapText = "9 exécutions pour 8 exécuteurs" }, Sharing[1], new("c", "Paquet", Monday.AddHours(11), Monday.AddHours(12)) { OverlapText = "Attend un exécuteur" }],
                new TimeOnly(8, 0),
                new TimeOnly(12, 0),
                more: parameters => parameters.Add(component => component.ShowOverlaps, showOverlaps));

            Assert.Equal("9 exécutions pour 8 exécuteurs", Block(scheduler, "a").QuerySelector(".omni-visually-hidden")!.TextContent);
            Assert.Contains("omni-scheduler__appointment--overlapping", Block(scheduler, "a").ClassList);
            Assert.Equal("Attend un exécuteur", Block(scheduler, "c").QuerySelector(".omni-visually-hidden")!.TextContent);
            Assert.Contains("omni-scheduler__appointment--overlapping", Block(scheduler, "c").ClassList);
            Assert.Equal(showOverlaps, Block(scheduler, "b").ClassList.Contains("omni-scheduler__appointment--overlapping"));
            Assert.Equal(showOverlaps ? "Chevauche Compilation" : null, Block(scheduler, "b").QuerySelector(".omni-visually-hidden")?.TextContent);
        }
    }

    [Fact]
    public void BlankOverlapText_LeavesTheSchedulersOwnMark()
    {
        var scheduler = RenderGrid([Sharing[0] with { OverlapText = "  " }, Sharing[1]], new TimeOnly(8, 0), new TimeOnly(12, 0));

        Assert.Equal("Chevauche Tests", Block(scheduler, "a").QuerySelector(".omni-visually-hidden")!.TextContent);
    }

    [Fact]
    public void BriefAppointments_ThatDoNotOverlap_StillTakeTwoLanes_WithoutTheOverlapMark()
    {
        var scheduler = RenderGrid(
            [
                new("a", "Court", Monday.AddHours(8), Monday.AddHours(8).AddMinutes(5)),
                new("b", "Suivant", Monday.AddHours(8).AddMinutes(5), Monday.AddHours(8).AddMinutes(10))
            ],
            new TimeOnly(8, 0),
            new TimeOnly(12, 0));

        Assert.Contains("omni-scheduler__lanes-2", Block(scheduler, "a").ClassList);
        Assert.DoesNotContain("omni-scheduler__appointment--overlapping", Block(scheduler, "a").ClassList);
    }

    [Theory]
    [InlineData(15, "omni-scheduler__appointment--brief")]
    [InlineData(30, "omni-scheduler__appointment--short")]
    [InlineData(59, "omni-scheduler__appointment--short")]
    [InlineData(60, null)]
    public void Block_SaysHowMuchTextItsHeightHolds(int minutes, string? expected)
    {
        var scheduler = RenderGrid(
            [new("run", "Build", Monday.AddHours(9), Monday.AddHours(9).AddMinutes(minutes))],
            new TimeOnly(8, 0),
            new TimeOnly(12, 0));

        var sizes = Block(scheduler, "run").ClassList.Where(item => item is "omni-scheduler__appointment--brief" or "omni-scheduler__appointment--short");
        Assert.Equal(expected is null ? [] : [expected], sizes);
    }

    [Fact]
    public void AppointmentFromTheDayBefore_GoesOnFromTheTopOfTheColumn_BesideThoseItOverlaps()
    {
        var scheduler = RenderGrid(
            [
                new("night", "Nuit", Monday.AddHours(-2), Monday.AddHours(10)),
                new("morning", "Matin", Monday.AddHours(9), Monday.AddHours(11))
            ],
            new TimeOnly(8, 0),
            new TimeOnly(18, 0));

        var block = Block(scheduler, "night");
        Assert.Equal(0, Row(block));
        Assert.Equal(400, Hundredths(block, "len"));
        Assert.Contains("omni-scheduler__lanes-2", block.ClassList);
        Assert.Contains("omni-scheduler__lane-1", Block(scheduler, "morning").ClassList);
    }

    [Fact]
    public void WeekView_DrawsAnAppointmentInTheColumnOfItsStart_AndInTheNextOne()
    {
        var scheduler = RenderGrid(
            [new("night", "Nuit", Monday.AddHours(22), Monday.AddHours(34))],
            new TimeOnly(8, 0),
            new TimeOnly(18, 0),
            OmniCalendarView.Week);

        var blocks = BlocksByColumn(scheduler, "night");
        Assert.Equal([1, 2], blocks.Select(Column));
        Assert.Equal(19, Row(blocks[0]));
        Assert.Equal(0, Row(blocks[1]));
        Assert.Equal(400, Hundredths(blocks[1], "len"));
    }

    [Fact]
    public void NightGrid_DrawsARunOfSeveralNights_InEachColumnItReaches_AndNotInTheNextEvening()
    {
        var scheduler = RenderGrid(
            [new("long", "Longue", Monday.AddHours(23), Monday.AddHours(49))],
            new TimeOnly(20, 0),
            new TimeOnly(8, 0),
            OmniCalendarView.Week);

        var blocks = BlocksByColumn(scheduler, "long");
        Assert.Equal([1, 2], blocks.Select(Column));
        Assert.Equal(6, Row(blocks[0]));
        Assert.Equal(1800, Hundredths(blocks[0], "len"));
        Assert.Equal(0, Row(blocks[1]));
        Assert.Equal(1000, Hundredths(blocks[1], "len"));
    }

    [Fact]
    public void AppointmentEndingAsTheColumnOpens_DrawsNoBlockThere()
    {
        var scheduler = RenderGrid(
            [new("night", "Nuit", Monday.AddHours(-2), Monday.AddHours(8))],
            new TimeOnly(8, 0),
            new TimeOnly(18, 0));

        Assert.Empty(scheduler.FindAll("[data-omni-scheduler-appointment='night']"));
    }

    [Fact]
    public void BlockClasses_AllExistInTheStylesheet()
    {
        foreach (var name in new[] { "at-0-1", "at-1-9", "len-0-1", "len-4-9", "lanes-2", "lanes-8", "lane-1", "lane-7" })
        {
            Assert.NotEmpty(ShippedLookTests.Body($".omni-scheduler__{name}"));
        }
    }

    [Fact]
    public void Find_GivesEveryOverlappingPair_WithTheRuleTheGridDraws()
    {
        OmniSchedulerAppointment first = new("a", "A", Monday.AddHours(8), Monday.AddHours(10));
        OmniSchedulerAppointment second = new("b", "B", Monday.AddHours(9), Monday.AddHours(9).AddMinutes(30));
        OmniSchedulerAppointment third = new("c", "C", Monday.AddHours(9).AddMinutes(15), Monday.AddHours(11));
        OmniSchedulerAppointment touching = new("d", "D", Monday.AddHours(11), Monday.AddHours(12));

        var overlaps = OmniSchedulerOverlap.Find([touching, third, second, first]);

        Assert.Equal(
            [new OmniSchedulerOverlap(first, second), new OmniSchedulerOverlap(first, third), new OmniSchedulerOverlap(second, third)],
            overlaps);
    }

    [Fact]
    public void Find_ComparesInstants_AcrossOffsets()
    {
        var paris = new DateTimeOffset(2026, 6, 8, 11, 0, 0, TimeSpan.FromHours(2));
        OmniSchedulerAppointment utc = new("a", "A", Monday.AddHours(8), Monday.AddHours(10));
        OmniSchedulerAppointment local = new("b", "B", paris, paris.AddHours(1));

        Assert.Single(OmniSchedulerOverlap.Find([utc, local]));
    }

    [Fact]
    public void NightGrid_DrawsEverySlotFromTheEveningToTheMorning_AndMarksTheFirstRowOfTheNextDate()
    {
        var scheduler = RenderGrid([], new TimeOnly(20, 0), new TimeOnly(8, 0));

        var rows = scheduler.FindAll(".omni-scheduler-grid__table tbody tr");
        Assert.Equal(24, rows.Count);
        Assert.Equal("20:00", rows[0].QuerySelector("time")!.GetAttribute("datetime"));
        Assert.Equal("00:00", rows[8].QuerySelector("time")!.GetAttribute("datetime"));
        Assert.Equal("07:30", rows[23].QuerySelector("time")!.GetAttribute("datetime"));
        Assert.Equal([8], rows.Select((row, index) => (row, index)).Where(item => item.row.ClassList.Contains("omni-scheduler-grid__row--next-day")).Select(item => item.index));
    }

    [Fact]
    public void NightGrid_KeepsARunCrossingMidnight_AndTheMorningAfter_InTheColumnOfTheEvening()
    {
        var scheduler = RenderGrid(
            [
                new("crossing", "Nuit", Monday.AddHours(23), Monday.AddHours(26)),
                new("morning", "Matin", Monday.AddHours(29), Monday.AddHours(30)),
                new("next-evening", "Soir suivant", Monday.AddHours(45), Monday.AddHours(46)),
                new("sunday-night", "Veille", Monday.AddHours(2), Monday.AddHours(3))
            ],
            new TimeOnly(20, 0),
            new TimeOnly(8, 0));

        Assert.Equal(6, Row(Block(scheduler, "crossing")));
        Assert.Equal(600, Hundredths(Block(scheduler, "crossing"), "len"));
        Assert.Equal(18, Row(Block(scheduler, "morning")));
        // The other columns are not shown: the night of Tuesday and the end of the night of Sunday.
        Assert.Empty(scheduler.FindAll("[data-omni-scheduler-appointment='next-evening']"));
        Assert.Empty(scheduler.FindAll("[data-omni-scheduler-appointment='sunday-night']"));
    }

    [Fact]
    public void NightGrid_ReportsASlotPastMidnightOnTheNextDate_AndTheLastSlotCappedByDayEnd()
    {
        OmniSchedulerSlot? clicked = null;
        var scheduler = RenderGrid(
            [],
            new TimeOnly(20, 0),
            new TimeOnly(7, 45),
            more: parameters => parameters.Add(component => component.OnSlotClick, (OmniSchedulerSlot slot) => clicked = slot));

        var rows = scheduler.FindAll(".omni-scheduler-grid__table tbody tr");
        rows[8].QuerySelector(".omni-scheduler__slot")!.Click();
        Assert.Equal(Monday.AddHours(24), clicked!.Start);
        Assert.Equal(Monday.AddHours(24.5), clicked.End);

        scheduler.FindAll(".omni-scheduler-grid__table tbody tr")[^1].QuerySelector(".omni-scheduler__slot")!.Click();
        Assert.Equal(Monday.AddHours(31.5), clicked!.Start);
        Assert.Equal(Monday.AddHours(31.75), clicked.End);
    }

    [Fact]
    public void NightGrid_MovesARunToASlotPastMidnight_OnTheNextDate_KeepingItsDuration()
    {
        OmniSchedulerAppointmentMove? moved = null;
        var scheduler = RenderGrid(
            [new("run", "Nuit", Monday.AddHours(20), Monday.AddHours(21).AddMinutes(30))],
            new TimeOnly(20, 0),
            new TimeOnly(8, 0),
            more: parameters => parameters.Add(component => component.OnAppointmentMove, (OmniSchedulerAppointmentMove move) => moved = move));

        scheduler.Find(".omni-scheduler__move").Click();
        scheduler.FindAll(".omni-scheduler-grid__table tbody tr")[10].QuerySelector(".omni-scheduler__place")!.Click();

        Assert.Equal(Monday.AddHours(25), moved!.Start);
        Assert.Equal(Monday.AddHours(26.5), moved.End);
    }

    [Theory]
    [InlineData(OmniCalendarView.Day, 32)]
    [InlineData(OmniCalendarView.Week, 176)]
    public void NightGrid_LoadsTheMorningAfterTheLastColumn(OmniCalendarView view, int hours)
    {
        var ranges = new List<(DateTimeOffset Start, DateTimeOffset End)>();
        RenderGrid([], new TimeOnly(20, 0), new TimeOnly(8, 0), view, parameters => parameters.Add(
            component => component.Load,
            (start, end, _) =>
            {
                ranges.Add((start, end));
                return Task.FromResult<IReadOnlyList<OmniSchedulerAppointment>>([]);
            }));

        var range = Assert.Single(ranges);
        Assert.Equal(Monday, range.Start);
        Assert.Equal(Monday.AddHours(hours), range.End);
    }

    [Fact]
    public void GridOfNoLength_DrawsNoGrid()
    {
        var scheduler = RenderGrid([], new TimeOnly(8, 0), new TimeOnly(8, 0));

        Assert.Empty(scheduler.FindAll(".omni-scheduler-grid"));
    }

    [Fact]
    public void WhileAnAppointmentIsDragged_TheRootSaysSo_SoTheBlocksLetThePointerThrough()
    {
        var scheduler = RenderGrid(
            [new("run", "Build", Monday.AddHours(8), Monday.AddHours(10))],
            new TimeOnly(8, 0),
            new TimeOnly(12, 0),
            more: parameters => parameters.Add(component => component.OnAppointmentMove, (OmniSchedulerAppointmentMove _) => { }));

        Block(scheduler, "run").DragStart();
        Assert.Contains("omni-scheduler--dragging", scheduler.Find(".omni-scheduler").ClassList);

        Block(scheduler, "run").DragEnd();
        Assert.DoesNotContain("omni-scheduler--dragging", scheduler.Find(".omni-scheduler").ClassList);
    }
}
