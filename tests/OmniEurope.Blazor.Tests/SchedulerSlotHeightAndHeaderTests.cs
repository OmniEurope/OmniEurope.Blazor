using System.Globalization;
using System.Text.RegularExpressions;
using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The slot height of the time grid (a class on the root, a variable the cells read), the day header
/// format, and the title of the period buttons.
/// </summary>
public sealed class SchedulerSlotHeightAndHeaderTests : OmniBunitContext
{
    // A Monday: the week starts on it in French and on the day before in English (United States).
    private static readonly DateTimeOffset Monday = new(2026, 6, 8, 0, 0, 0, TimeSpan.Zero);

    private IRenderedComponent<OmniScheduler> RenderScheduler(
        OmniCalendarView view,
        string culture = "fr-FR",
        bool timeGrid = true,
        Action<ComponentParameterCollectionBuilder<OmniScheduler>>? more = null) =>
        Render<OmniScheduler>(parameters =>
        {
            parameters
                .Add(component => component.Date, Monday)
                .Add(component => component.View, view)
                .Add(component => component.TimeZone, TimeZoneInfo.Utc)
                .Add(component => component.Culture, CultureInfo.GetCultureInfo(culture));
            if (timeGrid)
            {
                parameters
                    .Add(component => component.DayStart, new TimeOnly(8, 0))
                    .Add(component => component.DayEnd, new TimeOnly(18, 0));
            }

            more?.Invoke(parameters);
        });

    // The ids Blazor gives each render to its event handlers and element references differ from one render to the next.
    private static string WithoutEventIds(string markup) => Regex.Replace(markup, @"\sblazor:\w+=""[^""]*""", string.Empty);

    [Fact]
    public void Standard_IsTheDefault_AndAddsNoClass()
    {
        var scheduler = RenderScheduler(OmniCalendarView.Week);

        Assert.Equal(OmniSchedulerSlotHeight.Standard, scheduler.Instance.SlotHeight);
        Assert.DoesNotContain("omni-scheduler--slot-compact", scheduler.Find("section.omni-scheduler").ClassList);
    }

    [Fact]
    public void Compact_AddsItsClassToTheRoot_AlongsideTheOnesTheHostGives()
    {
        var scheduler = RenderScheduler(OmniCalendarView.Week, more: parameters => parameters
            .Add(component => component.SlotHeight, OmniSchedulerSlotHeight.Compact)
            .Add(component => component.Class, "host-class"));

        var root = scheduler.Find("section.omni-scheduler");
        Assert.Contains("omni-scheduler--slot-compact", root.ClassList);
        Assert.Contains("host-class", root.ClassList);
    }

    [Fact]
    public void TheStylesheet_GivesCompactHalfOfTheStandardRow_AndTheCellsReadIt()
    {
        var standard = ShippedLookTests.Value(ShippedLookTests.Body(".omni-scheduler"), "--omni-scheduler-slot-height");
        var compact = ShippedLookTests.Value(ShippedLookTests.Body(".omni-scheduler--slot-compact"), "--omni-scheduler-slot-height");

        Assert.Equal("calc(var(--omni-control-height) + var(--omni-space-sm))", standard);
        Assert.Equal("calc((var(--omni-control-height) + var(--omni-space-sm) - var(--omni-cell-pad-y) - var(--omni-border-width)) / 2)", compact);
        Assert.Equal("var(--omni-scheduler-slot-height)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-scheduler-grid__cell"), "block-size"));
    }

    [Fact]
    public void ABlockDrawnOnItsDuration_IsSizedInPercentOfItsCell_SoItFollowsTheSlotHeight()
    {
        var block = ShippedLookTests.Body(".omni-scheduler-grid .omni-scheduler-grid__cell > .omni-scheduler__appointment");

        // No length of its own: its height and its place are fractions of the cell it starts in.
        foreach (var property in new[] { "block-size", "inset-block-start", "min-block-size" })
        {
            var value = ShippedLookTests.Value(block, property);
            Assert.DoesNotMatch(@"\d(rem|px|em)\b|--omni-control-height|--omni-space", value);
        }

        Assert.Contains("--omni-scheduler-row: calc(100% + var(--omni-border-width))", Regex.Replace(block, @"\s+", " "), StringComparison.Ordinal);
    }

    [Fact]
    public void NoOtherRuleSizesTheCellsOfTheGrid()
    {
        var sizing = ShippedLookTests.Rules()
            .Where(rule => rule.Selector.EndsWith("omni-scheduler-grid__cell", StringComparison.Ordinal) && Regex.IsMatch(rule.Body, @"(?:^|;|\{)\s*(block-size|height|min-block-size|min-height)\s*:"))
            .Select(rule => rule.Selector)
            .ToArray();

        Assert.Equal([".omni-scheduler-grid__cell"], sizing);
    }

    [Fact]
    public void ACompactBlock_IsTightToItsEdges_AndItsHourLabelIsOneLineTall()
    {
        var tight = ShippedLookTests.Body(".omni-scheduler--slot-compact .omni-scheduler-grid .omni-scheduler__appointment");
        Assert.Equal("0", ShippedLookTests.Value(tight, "padding-block"));
        Assert.Equal("1.2", ShippedLookTests.Value(tight, "line-height"));
        Assert.Equal("1", ShippedLookTests.Value(ShippedLookTests.Body(".omni-scheduler--slot-compact .omni-scheduler-grid__table tbody .omni-scheduler-grid__time"), "line-height"));
    }

    // Standard: under one slot a block has only its title (brief), under two its time and one line of title
    // (short). A compact slot is half as high, so the same heights are reached at twice the slots.
    [Theory]
    [InlineData(OmniSchedulerSlotHeight.Standard, 30, "omni-scheduler__appointment--brief")]
    [InlineData(OmniSchedulerSlotHeight.Standard, 60, "omni-scheduler__appointment--short")]
    [InlineData(OmniSchedulerSlotHeight.Standard, 120, null)]
    [InlineData(OmniSchedulerSlotHeight.Compact, 60, "omni-scheduler__appointment--brief")]
    [InlineData(OmniSchedulerSlotHeight.Compact, 90, "omni-scheduler__appointment--brief")]
    [InlineData(OmniSchedulerSlotHeight.Compact, 120, "omni-scheduler__appointment--short")]
    [InlineData(OmniSchedulerSlotHeight.Compact, 210, "omni-scheduler__appointment--short")]
    [InlineData(OmniSchedulerSlotHeight.Compact, 240, null)]
    public void TheLookOfABlock_FollowsItsHeightInPixels_NotItsNumberOfSlots(OmniSchedulerSlotHeight height, int minutes, string? expected)
    {
        var item = new OmniSchedulerAppointment("block", "Revue", Monday.AddHours(9), Monday.AddHours(9).AddMinutes(minutes));
        var scheduler = RenderScheduler(OmniCalendarView.Day, more: parameters => parameters
            .Add(component => component.SlotHeight, height)
            .Add(component => component.Items, [item]));

        var classes = scheduler.Find("[data-omni-scheduler-appointment='block']").ClassList;
        var looks = new[] { "omni-scheduler__appointment--brief", "omni-scheduler__appointment--short" }.Where(classes.Contains).ToArray();
        Assert.Equal(expected is null ? [] : [expected], looks);
    }

    [Fact]
    public void ACompactBlock_StillCarriesItsTimeAndTitle_InTheMarkup()
    {
        var item = new OmniSchedulerAppointment("one-slot", "Revue", Monday.AddHours(9), Monday.AddHours(10));
        var scheduler = RenderScheduler(OmniCalendarView.Day, more: parameters => parameters
            .Add(component => component.SlotHeight, OmniSchedulerSlotHeight.Compact)
            .Add(component => component.Items, [item]));

        var block = scheduler.Find("[data-omni-scheduler-appointment='one-slot']");
        Assert.Equal("Revue", block.QuerySelector("strong")!.TextContent);
        Assert.Equal("09:00", block.QuerySelector("time")!.TextContent);
        Assert.Equal("09:00–10:00 Revue", block.GetAttribute("title"));
    }

    [Theory]
    [InlineData("fr-FR", OmniCalendarView.Week, "lun. 8", "mar. 9")]
    [InlineData("en-US", OmniCalendarView.Week, "Sun 7", "Mon 8")]
    public void WithoutADayHeaderFormat_TheGridHeaderIsTheAbbreviatedDayAndNumber(string culture, OmniCalendarView view, string first, string second)
    {
        var headers = RenderScheduler(view, culture).FindAll("thead th.omni-scheduler-grid__day").Select(header => header.TextContent).ToArray();

        Assert.Equal(7, headers.Length);
        Assert.Equal(first, headers[0].Replace(" ", " ", StringComparison.Ordinal).Trim());
        Assert.Equal(second, headers[1].Replace(" ", " ", StringComparison.Ordinal).Trim());
    }

    [Theory]
    [InlineData("fr-FR", "ddd d")]
    [InlineData("en-US", "ddd d")]
    public void ANullDayHeaderFormat_IsExactlyTheExplicitDefaultPattern(string culture, string pattern)
    {
        foreach (var timeGrid in new[] { true, false })
        {
            var byDefault = RenderScheduler(OmniCalendarView.Week, culture, timeGrid);
            var explicitly = RenderScheduler(OmniCalendarView.Week, culture, timeGrid, parameters => parameters.Add(component => component.DayHeaderFormat, pattern));

            Assert.Equal(WithoutEventIds(byDefault.Markup), WithoutEventIds(explicitly.Markup));
        }
    }

    [Theory]
    [InlineData("fr-FR", "lundi 8 juin", "mardi 9 juin")]
    [InlineData("en-US", "Sunday 7 June", "Monday 8 June")]
    public void ADayHeaderFormat_WritesEachDayOfTheGridInTheCulture(string culture, string first, string second)
    {
        var scheduler = RenderScheduler(OmniCalendarView.Week, culture, more: parameters => parameters.Add(component => component.DayHeaderFormat, "dddd d MMMM"));

        var headers = scheduler.FindAll("thead th.omni-scheduler-grid__day").Select(header => header.TextContent).ToArray();
        Assert.Equal(7, headers.Length);
        Assert.Equal([first, second], headers.Take(2));
    }

    [Theory]
    [InlineData("fr-FR", "lundi 8 juin")]
    [InlineData("en-US", "Monday 8 June")]
    public void ADayHeaderFormat_WritesEachDayOfTheWeekList_ButNotTheMonthDayNumbers(string culture, string monday)
    {
        var week = RenderScheduler(OmniCalendarView.Week, culture, timeGrid: false, parameters => parameters.Add(component => component.DayHeaderFormat, "dddd d MMMM"));
        Assert.Contains(monday, week.FindAll(".omni-week-view__day > time.omni-scheduler__date").Select(date => date.TextContent));

        var month = RenderScheduler(OmniCalendarView.Month, culture, timeGrid: false, parameters => parameters.Add(component => component.DayHeaderFormat, "dddd d MMMM"));
        Assert.Equal("8", month.Find(".omni-month-view__day[data-date='2026-06-08'] time.omni-scheduler__date").TextContent);
    }

    [Fact]
    public void ADayHeaderFormatWithATimePart_WritesZeros_InsteadOfFailing()
    {
        var scheduler = RenderScheduler(OmniCalendarView.Day, more: parameters => parameters.Add(component => component.DayHeaderFormat, "ddd d HH:mm"));

        Assert.Equal("lun. 8 00:00", scheduler.Find("thead th.omni-scheduler-grid__day").TextContent.Replace(" ", " ", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("fr-FR", OmniCalendarView.Day, "Jour précédent", "Jour suivant")]
    [InlineData("fr-FR", OmniCalendarView.Week, "Semaine précédente", "Semaine suivante")]
    [InlineData("fr-FR", OmniCalendarView.Month, "Mois précédent", "Mois suivant")]
    [InlineData("en-US", OmniCalendarView.Day, "Previous day", "Next day")]
    [InlineData("en-US", OmniCalendarView.Week, "Previous week", "Next week")]
    [InlineData("en-US", OmniCalendarView.Month, "Previous month", "Next month")]
    public void ThePeriodButtons_TitleTheViewTheyLeadTo_AndKeepTheirAccessibleName(string culture, OmniCalendarView view, string previous, string next)
    {
        var scheduler = RenderScheduler(view, culture, timeGrid: false);

        var buttons = scheduler.FindAll(".omni-scheduler__header > button[aria-label]");
        Assert.Equal(2, buttons.Count);
        Assert.Equal(previous, buttons[0].GetAttribute("title"));
        Assert.Equal(next, buttons[1].GetAttribute("title"));
        Assert.Equal(culture == "fr-FR" ? "Période précédente" : "Previous period", buttons[0].GetAttribute("aria-label"));
        Assert.Equal(culture == "fr-FR" ? "Période suivante" : "Next period", buttons[1].GetAttribute("aria-label"));
    }

    [Fact]
    public async Task ThePeriodButtonTitles_FollowTheViewThePickerChooses()
    {
        var scheduler = RenderScheduler(OmniCalendarView.Month, timeGrid: false);
        Assert.Equal("Mois précédent", scheduler.Find(".omni-scheduler__header > button[aria-label='Période précédente']").GetAttribute("title"));

        await scheduler.InvokeAsync(() => scheduler.FindAll("[role='radio']")[0].Click());

        Assert.Equal("Jour précédent", scheduler.Find(".omni-scheduler__header > button[aria-label='Période précédente']").GetAttribute("title"));
        Assert.Equal("Jour suivant", scheduler.Find(".omni-scheduler__header > button[aria-label='Période suivante']").GetAttribute("title"));
    }
}
