using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The value of a date range filter: how its two sides are written and read back, the bounds it stands
/// for, and the cells it keeps.
/// </summary>
public sealed class DataGridDateRangeTests
{
    [Theory]
    [InlineData(null, null, "")]
    [InlineData("  ", "", "")]
    [InlineData(" 2026-08-24 ", null, "2026-08-24/")]
    [InlineData(null, "2026-08-30", "/2026-08-30")]
    [InlineData("2026-08-24", "2026-08-30", "2026-08-24/2026-08-30")]
    public void Join_WritesTheSidesAsPicked(string? start, string? end, string expected) =>
        Assert.Equal(expected, OmniDataGridDateRange.Join(start, end));

    [Theory]
    [InlineData(null, "", "")]
    [InlineData(" ", "", "")]
    [InlineData(" 2026-08-24 ", "2026-08-24", "")]
    [InlineData("2026-08-24 / 2026-08-30", "2026-08-24", "2026-08-30")]
    [InlineData("/2026-08-30", "", "2026-08-30")]
    public void Split_ReadsTheSidesBack(string? value, string start, string end) =>
        Assert.Equal((start, end), OmniDataGridDateRange.Split(value));

    [Fact]
    public void Resolve_CoversWholeDays_AndPickedMinutesToTheirEnd()
    {
        Assert.Equal((new DateTime(2026, 8, 24), new DateTime(2026, 8, 31)), OmniDataGridDateRange.Resolve("2026-08-24/2026-08-30"));
        Assert.Equal((new DateTime(2026, 8, 24, 10, 0, 0), new DateTime(2026, 8, 24, 12, 31, 0)), OmniDataGridDateRange.Resolve("2026-08-24T10:00/2026-08-24T12:30"));
        // Seconds in the end are dropped: the end still covers its whole minute.
        Assert.Equal(new DateTime(2026, 8, 24, 12, 31, 0), OmniDataGridDateRange.Resolve("/2026-08-24T12:30:45").EndExclusive);
    }

    [Fact]
    public void Resolve_LeavesAnUnreadableOrOpenSideOpen()
    {
        Assert.Equal((null, new DateTime(2026, 9, 1)), OmniDataGridDateRange.Resolve("demain/2026-08-31"));
        Assert.Equal((new DateTime(2026, 8, 24), (DateTime?)null), OmniDataGridDateRange.Resolve("2026-08-24/n'importe"));
        Assert.Equal(((DateTime?)null, (DateTime?)null), OmniDataGridDateRange.Resolve(null));
    }

    public static TheoryData<object?, bool> Cells => new()
    {
        { new DateTime(2026, 8, 24, 23, 59, 0), true },
        { new DateTime(2026, 8, 31), false },
        { new DateTimeOffset(2026, 8, 30, 23, 0, 0, TimeSpan.FromHours(-5)), true },
        { new DateTimeOffset(2026, 8, 23, 23, 0, 0, TimeSpan.FromHours(5)), false },
        { new DateOnly(2026, 8, 30), true },
        { new DateOnly(2026, 8, 31), false },
        { "2026-08-25", false },
        { null, false },
    };

    [Theory]
    [MemberData(nameof(Cells))]
    public void Contains_KeepsTheDatesOfTheRange_OnTheirShownClockTime(object? cell, bool kept) =>
        Assert.Equal(kept, OmniDataGridDateRange.Contains("2026-08-24/2026-08-30", cell));

    [Fact]
    public void Contains_WithOneSide_BoundsThatSideOnly_AndWithoutAnyKeepsEverything()
    {
        Assert.True(OmniDataGridDateRange.Contains("2026-08-24/", new DateTime(2099, 1, 1)));
        Assert.False(OmniDataGridDateRange.Contains("2026-08-24/", new DateTime(2000, 1, 1)));
        Assert.True(OmniDataGridDateRange.Contains("/2026-08-30", new DateTime(2000, 1, 1)));
        Assert.True(OmniDataGridDateRange.Contains("x/y", "pas une date"));
        Assert.True(OmniDataGridDateRange.Contains(null, null));
    }

    [Fact]
    public void FormatBound_IsInvariantAndSortable() =>
        Assert.Equal("2026-08-24T10:05:00", OmniDataGridDateRange.FormatBound(new DateTime(2026, 8, 24, 10, 5, 0)));
}
