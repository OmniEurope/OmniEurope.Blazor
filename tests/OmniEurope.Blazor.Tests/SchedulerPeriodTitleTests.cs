using System.Globalization;
using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>The title of the period an <see cref="OmniScheduler"/> shows, asked with ShowPeriodTitle.</summary>
public sealed class SchedulerPeriodTitleTests : OmniBunitContext
{
    private IRenderedComponent<OmniScheduler> RenderScheduler(DateTimeOffset date, OmniCalendarView view, string culture, bool show = true) =>
        Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, date)
            .Add(component => component.View, view)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.Culture, CultureInfo.GetCultureInfo(culture))
            .Add(component => component.ShowPeriodTitle, show));

    private static DateTimeOffset On(int year, int month, int day) => new(year, month, day, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void WithoutShowPeriodTitle_TheHeaderHasNoTitle()
    {
        var scheduler = RenderScheduler(On(2026, 11, 3), OmniCalendarView.Week, "fr-FR", show: false);

        Assert.Empty(scheduler.FindAll(".omni-scheduler__title"));
    }

    [Theory]
    [InlineData(OmniCalendarView.Day, 2026, 11, 3, "fr-FR", "mardi 3 novembre 2026")]
    [InlineData(OmniCalendarView.Month, 2026, 11, 3, "fr-FR", "novembre 2026")]
    [InlineData(OmniCalendarView.Week, 2026, 11, 3, "fr-FR", "2 au 8 novembre 2026")]
    [InlineData(OmniCalendarView.Week, 2026, 12, 2, "fr-FR", "30 novembre au 6 décembre 2026")]
    [InlineData(OmniCalendarView.Week, 2026, 12, 31, "fr-FR", "28 décembre 2026 au 3 janvier 2027")]
    [InlineData(OmniCalendarView.Day, 2026, 11, 3, "en-US", "Tuesday, November 3, 2026")]
    [InlineData(OmniCalendarView.Month, 2026, 11, 3, "en-US", "November 2026")]
    [InlineData(OmniCalendarView.Week, 2026, 11, 3, "en-US", "November 1 – November 7, 2026")]
    [InlineData(OmniCalendarView.Week, 2026, 11, 3, "de-DE", "2. – 8. November 2026")]
    [InlineData(OmniCalendarView.Week, 2026, 11, 3, "es-ES", "2 – 8 de noviembre de 2026")]
    [InlineData(OmniCalendarView.Week, 2026, 12, 2, "es-ES", "30 de noviembre – 6 de diciembre de 2026")]
    [InlineData(OmniCalendarView.Week, 2026, 11, 3, "bg-BG", "2 – 8 ноември 2026 г.")]
    [InlineData(OmniCalendarView.Week, 2026, 11, 3, "hu-HU", "2. – 2026. november 8.")]
    [InlineData(OmniCalendarView.Week, 2026, 12, 2, "lt-LT", "lapkričio 30 d. – 2026 m. gruodžio 6 d.")]
    [InlineData(OmniCalendarView.Week, 2026, 12, 31, "lt-LT", "2026 m. gruodžio 28 d. – 2027 m. sausio 3 d.")]
    [InlineData(OmniCalendarView.Week, 2026, 12, 31, "da-DK", "den 28. december 2026 – den 3. januar 2027")]
    public void TheTitle_IsTheDay_TheWeek_OrTheMonth_InTheCultureOfTheScheduler(OmniCalendarView view, int year, int month, int day, string culture, string expected)
    {
        var scheduler = RenderScheduler(On(year, month, day), view, culture);

        var title = scheduler.Find(".omni-scheduler__title");
        Assert.Equal(expected, title.TextContent);
        Assert.Equal("polite", title.GetAttribute("aria-live"));
    }

    [Fact]
    public void AWeek_InACultureWhoseLongDateHasNoWeekday_KeepsItsPatternWhole()
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("fr-FR").Clone();
        culture.DateTimeFormat.LongDatePattern = "d MMMM yyyy";
        var scheduler = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.Date, On(2026, 11, 3))
            .Add(component => component.View, OmniCalendarView.Week)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.Culture, culture)
            .Add(component => component.ShowPeriodTitle, true));

        Assert.Equal("2 au 8 novembre 2026", scheduler.Find(".omni-scheduler__title").TextContent);
    }

    [Fact]
    public void TheTitle_FollowsTheNavigation()
    {
        var scheduler = RenderScheduler(On(2026, 11, 3), OmniCalendarView.Month, "fr-FR");

        scheduler.Find("button[aria-label='Période suivante']").Click();

        Assert.Equal("décembre 2026", scheduler.Find(".omni-scheduler__title").TextContent);
    }

    [Fact]
    public void TheTitle_IsStyledInTheShippedSheet_WithoutInlineStyle()
    {
        var scheduler = RenderScheduler(On(2026, 11, 3), OmniCalendarView.Day, "fr-FR");

        Assert.Null(scheduler.Find(".omni-scheduler__title").GetAttribute("style"));
        Assert.Equal("650", ShippedLookTests.Value(ShippedLookTests.Body(".omni-scheduler__title"), "font-weight"));
    }
}
