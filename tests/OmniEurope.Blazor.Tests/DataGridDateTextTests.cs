using System.Globalization;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The text of a Date filter: which days, months and years it reads, in which order, and what it
/// refuses. Pure: the culture is always passed in.
/// </summary>
public sealed class DataGridDateTextTests
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");
    private static readonly CultureInfo American = CultureInfo.GetCultureInfo("en-US");
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo Swedish = CultureInfo.GetCultureInfo("sv-SE");

    private static string? Read(string? text, CultureInfo culture) =>
        OmniDataGridDateText.TryParse(text, culture, out var start, out var end)
            ? $"{start:yyyy-MM-dd}|{end:yyyy-MM-dd}"
            : null;

    [Theory]
    [InlineData("05/03/2026", "2026-03-05|2026-03-06")]
    [InlineData("5/3/2026", "2026-03-05|2026-03-06")]
    [InlineData("31/12/2026", "2026-12-31|2027-01-01")]
    [InlineData("29/02/2028", "2028-02-29|2028-03-01")]
    [InlineData("01/01/0001", "0001-01-01|0001-01-02")]
    public void ADay_CoversThatDayOnly(string text, string expected) =>
        Assert.Equal(expected, Read(text, French));

    [Theory]
    [InlineData("03/2026", "2026-03-01|2026-04-01")]
    [InlineData("3/2026", "2026-03-01|2026-04-01")]
    [InlineData("12/2026", "2026-12-01|2027-01-01")]
    [InlineData("02/2028", "2028-02-01|2028-03-01")]
    public void AMonth_CoversTheWholeMonth(string text, string expected) =>
        Assert.Equal(expected, Read(text, French));

    [Theory]
    [InlineData("2026", "2026-01-01|2027-01-01")]
    [InlineData("0001", "0001-01-01|0002-01-01")]
    [InlineData("1999", "1999-01-01|2000-01-01")]
    public void AYear_CoversTheWholeYear(string text, string expected) =>
        Assert.Equal(expected, Read(text, French));

    [Theory]
    [InlineData("05/03/2026")]
    [InlineData("05.03.2026")]
    [InlineData("05-03-2026")]
    [InlineData("05/03.2026")]
    [InlineData(" 05 / 03 / 2026 ")]
    [InlineData("\t05/03/2026\n")]
    public void TheSeparatorsAreTheSlash_TheDot_AndTheHyphen_WhateverSurroundsThem(string text) =>
        Assert.Equal("2026-03-05|2026-03-06", Read(text, French));

    [Fact]
    public void TheDayAndTheMonthFollowTheOrderOfTheCulture()
    {
        Assert.Equal("2026-03-05|2026-03-06", Read("05/03/2026", French));
        Assert.Equal("2026-03-05|2026-03-06", Read("03/05/2026", American));
        Assert.Equal("2026-03-05|2026-03-06", Read("05.03.2026", German));
        // The same text, two readings.
        Assert.Equal("2026-05-03|2026-05-04", Read("03/05/2026", French));
        // 13 is no month in a month-first culture, and no day-first reading is attempted.
        Assert.Null(Read("13/05/2026", American));
        Assert.Equal("2026-05-13|2026-05-14", Read("13/05/2026", French));
    }

    [Theory]
    [InlineData("2026-03-05")]
    [InlineData("2026/03/05")]
    [InlineData("2026.3.5")]
    public void AYearWrittenFirstIsReadYearMonthDay_WhateverTheCulture(string text)
    {
        Assert.Equal("2026-03-05|2026-03-06", Read(text, French));
        Assert.Equal("2026-03-05|2026-03-06", Read(text, American));
        Assert.Equal("2026-03-05|2026-03-06", Read(text, Swedish));
    }

    [Theory]
    [InlineData("2026-03", "2026-03-01|2026-04-01")]
    [InlineData("2026/3", "2026-03-01|2026-04-01")]
    public void AMonthWithTheYearFirstIsReadToo(string text, string expected) =>
        Assert.Equal(expected, Read(text, French));

    [Fact]
    public void ADayTheMonthDoesNotHaveIsNotADate()
    {
        Assert.Null(Read("31/02/2026", French));
        Assert.Null(Read("29/02/2026", French));
        Assert.Null(Read("31/04/2026", French));
        Assert.Null(Read("00/03/2026", French));
        Assert.Null(Read("32/03/2026", French));
        Assert.Null(Read("05/13/2026", French));
        Assert.Null(Read("05/00/2026", French));
        Assert.Null(Read("00/2026", French));
        Assert.Null(Read("13/2026", French));
    }

    [Theory]
    [InlineData("20000")]
    [InlineData("12/03/20000")]
    [InlineData("03/20000")]
    [InlineData("200")]
    [InlineData("0000")]
    [InlineData("0000-01-01")]
    public void TheYearHasFourDigits_AndIsNotZero(string text) =>
        Assert.Null(Read(text, French));

    [Theory]
    [InlineData("26")]
    [InlineData("12/03/26")]
    [InlineData("03/26")]
    [InlineData("1/1/99")]
    public void ATwoDigitYearIsRefused(string text) =>
        Assert.Null(Read(text, French));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void BlankIsNotADate(string? text) =>
        Assert.Null(Read(text, French));

    [Theory]
    [InlineData("demain")]
    [InlineData("12/mars/2026")]
    [InlineData("12/03/2026 10:30")]
    [InlineData("12//2026")]
    [InlineData("/03/2026")]
    [InlineData("12/03/2026/")]
    [InlineData("1/2/3/2026")]
    [InlineData("12 03 2026")]
    [InlineData("-3/2026")]
    [InlineData("+3/2026")]
    [InlineData("1,5/2026")]
    [InlineData("٣/٢٠٢٦")]
    [InlineData("2026-2026")]
    [InlineData("2026/2026/2026")]
    public void AnythingElseIsNotADate(string text) =>
        Assert.Null(Read(text, French));

    [Fact]
    public void TheLastYearReachesTheLastInstantWithoutOverflowing()
    {
        Assert.True(OmniDataGridDateText.TryParse("9999", French, out var start, out var end));
        Assert.Equal(new DateTime(9999, 1, 1), start);
        Assert.Equal(DateTime.MaxValue, end);
        Assert.True(OmniDataGridDateText.TryParse("31/12/9999", French, out _, out end));
        Assert.Equal(DateTime.MaxValue, end);
        Assert.True(OmniDataGridDateText.TryParse("12/9999", French, out _, out end));
        Assert.Equal(DateTime.MaxValue, end);
    }

    [Theory]
    [InlineData("05/03/2026", "2026-03-05/2026-03-05")]
    [InlineData("03/2026", "2026-03-01/2026-03-31")]
    [InlineData("02/2028", "2028-02-01/2028-02-29")]
    [InlineData("2026", "2026-01-01/2026-12-31")]
    [InlineData("31/12/9999", "9999-12-31/9999-12-31")]
    [InlineData("9999", "9999-01-01/9999-12-31")]
    public void ThePeriodIsKeptAsWholeDays_TheEndDayIncluded(string text, string expected)
    {
        Assert.True(OmniDataGridDateText.TryParse(text, French, out var start, out var end));
        Assert.Equal(expected, OmniDataGridDateText.ToValue(start, end));
    }

    [Theory]
    [InlineData("05/03/2026")]
    [InlineData("03/2026")]
    [InlineData("2026")]
    [InlineData("31/12/9999")]
    [InlineData("12/9999")]
    [InlineData("9999")]
    public void TheValueResolvesToTheSamePeriod_AndReadsBackAsTheText(string text)
    {
        Assert.True(OmniDataGridDateText.TryParse(text, French, out var start, out var end));
        var value = OmniDataGridDateText.ToValue(start, end);

        Assert.Equal((start, end), OmniDataGridDateRange.Resolve(value));
        Assert.Equal(text, OmniDataGridDateText.Format(value, French));
    }

    [Fact]
    public void AValueThatIsNoSinglePeriodReadsAsItsTwoDays_AnOpenSideLeftOpen()
    {
        Assert.Equal("05/03/2026 - 20/03/2026", OmniDataGridDateText.Format("2026-03-05/2026-03-20", French));
        Assert.Equal("01/03/2026 - 30/03/2026", OmniDataGridDateText.Format("2026-03-01/2026-03-30", French));
        Assert.Equal("05/03/2026 -", OmniDataGridDateText.Format("2026-03-05/", French));
        Assert.Equal("- 20/03/2026", OmniDataGridDateText.Format("/2026-03-20", French));
        Assert.Equal(string.Empty, OmniDataGridDateText.Format("/", French));
        Assert.Equal(string.Empty, OmniDataGridDateText.Format(null, French));
        Assert.Equal(string.Empty, OmniDataGridDateText.Format("pas une date", French));
        // A time part is not shown: the range is given by its days.
        Assert.Equal("05/03/2026 - 05/03/2026", OmniDataGridDateText.Format("2026-03-05T10:00/2026-03-05T12:30", French));
    }

    [Fact]
    public void TheTextIsWrittenInTheOrderAndWithTheSeparatorOfTheCulture()
    {
        Assert.Equal("03/05/2026", OmniDataGridDateText.Format("2026-03-05/2026-03-05", American));
        Assert.Equal("03/2026", OmniDataGridDateText.Format("2026-03-01/2026-03-31", American));
        Assert.Equal("05.03.2026", OmniDataGridDateText.Format("2026-03-05/2026-03-05", German));
        Assert.Equal("2026-03-05", OmniDataGridDateText.Format("2026-03-05/2026-03-05", Swedish));
        Assert.Equal("2026-03", OmniDataGridDateText.Format("2026-03-01/2026-03-31", Swedish));
    }

    [Fact]
    public void TheHintFollowsTheCulture()
    {
        Assert.Equal("dd/mm/yyyy", OmniDataGridDateText.Pattern(French, "dd", "mm", "yyyy"));
        Assert.Equal("jj/mm/aaaa", OmniDataGridDateText.Pattern(French, "jj", "mm", "aaaa"));
        Assert.Equal("mm/dd/yyyy", OmniDataGridDateText.Pattern(American, "dd", "mm", "yyyy"));
        Assert.Equal("TT.MM.JJJJ", OmniDataGridDateText.Pattern(German, "TT", "MM", "JJJJ"));
        Assert.Equal("åååå-mm-dd", OmniDataGridDateText.Pattern(Swedish, "dd", "mm", "åååå"));
    }

    [Fact]
    public void ANullCultureIsRefused()
    {
        Assert.Throws<ArgumentNullException>(() => OmniDataGridDateText.TryParse("2026", null!, out _, out _));
        Assert.Throws<ArgumentNullException>(() => OmniDataGridDateText.Format("2026-03-05/", null!));
        Assert.Throws<ArgumentNullException>(() => OmniDataGridDateText.Pattern(null!, "dd", "mm", "yyyy"));
        Assert.ThrowsAny<ArgumentException>(() => OmniDataGridDateText.Pattern(French, " ", "mm", "yyyy"));
    }

    [Fact]
    public void ARangeEndingOnTheLastDayOf9999ResolvesWithoutOverflowing()
    {
        // The native picker reaches 9999-12-31, whose next day does not exist.
        Assert.Equal(DateTime.MaxValue, OmniDataGridDateRange.Resolve("/9999-12-31").EndExclusive);
        Assert.Equal(DateTime.MaxValue, OmniDataGridDateRange.Resolve("/9999-12-31T23:59").EndExclusive);
    }
}
