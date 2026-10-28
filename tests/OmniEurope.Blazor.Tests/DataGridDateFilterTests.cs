using System.Globalization;
using Bunit;
using OmniEurope.Blazor.Components;
using static OmniEurope.Blazor.Tests.DataGridFilterKindsTestHost;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The Date filter of a date column: one lenient text input (a day, a month or a year), the From and To
/// range of the column's AdvancedFilter next to it, and the way the two edit one filter. The rows are
/// the host's four tickets: two on 24 August 2026, one on the 25th, one on the 30th.
/// </summary>
public sealed class DataGridDateFilterTests : OmniBunitContext
{
    private const string TextInRow = "tr.omni-data-grid__filters td[data-omni-col=\"Opened\"] input.omni-data-grid__date-text";
    private const string TextInHeader = "th[data-omni-col=\"Opened\"] input.omni-data-grid__date-text";
    private const string RangeMenu = "th[data-omni-col=\"Opened\"] details.omni-data-grid__filter-menu .omni-data-grid__date-range";

    private static IDisposable French()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        return new Restore(previous);
    }

    private sealed class Restore(CultureInfo culture) : IDisposable
    {
        public void Dispose() => CultureInfo.CurrentCulture = culture;
    }

    // A date column left at its default, filtered from the inline row.
    private IRenderedComponent<DataGridFilterKindsTestHost> RenderInline(bool advanced = false) =>
        Render<DataGridFilterKindsTestHost>(parameters => parameters
            .Add(component => component.DateFilter, OmniDataGridColumnFilterType.Text)
            .Add(component => component.HeaderMenu, false)
            .Add(component => component.AdvancedDate, advanced));

    [Fact]
    public void ADateColumnAtItsDefault_GetsTheTextInput_WithTheHintOfTheCulture()
    {
        using var _ = French();
        var grid = RenderInline();

        var input = grid.Find(TextInRow);
        Assert.Equal("dd/mm/yyyy", input.GetAttribute("placeholder"));
        Assert.Equal("text", input.GetAttribute("type"));
        Assert.Null(input.GetAttribute("aria-invalid"));
        Assert.Empty(grid.FindAll(".omni-data-grid__date-range"));
        Assert.Equal(4, grid.FindAll("tbody tr").Count);
    }

    [Fact]
    public void ADateColumnWithATimePart_KeepsThePickers()
    {
        var grid = Render<DataGridFilterKindsTestHost>(parameters => parameters
            .Add(component => component.DateFilter, OmniDataGridColumnFilterType.Text)
            .Add(component => component.HeaderMenu, false)
            .Add(component => component.IncludesTime, true));

        Assert.Empty(grid.FindAll(".omni-data-grid__date-text"));
        Assert.Equal("datetime-local", grid.Find(".omni-data-grid__date-range-start").GetAttribute("type"));
    }

    [Theory]
    [InlineData("24/08/2026", 2)]
    [InlineData("25/08/2026", 1)]
    [InlineData("5/8/2026", 0)]
    [InlineData("08/2026", 4)]
    [InlineData("09/2026", 0)]
    [InlineData("2026", 4)]
    [InlineData("2025", 0)]
    [InlineData("24.08.2026", 2)]
    [InlineData("24-08-2026", 2)]
    [InlineData("2026-08-30", 1)]
    public void ADayAMonthOrAYear_KeepsTheRowsOfThatPeriod(string text, int rows)
    {
        using var _ = French();
        var grid = RenderInline();

        grid.Find(TextInRow).Change(text);

        Assert.Null(grid.Find(TextInRow).GetAttribute("aria-invalid"));
        // No row is the grid's empty state, which is one row too.
        var kept = grid.FindAll("tbody tr").Count(row => row.QuerySelector(".omni-data-grid__state") is null);
        Assert.Equal(rows, kept);
    }

    [Theory]
    [InlineData("20000")]
    [InlineData("24/08/20000")]
    [InlineData("31/02/2026")]
    [InlineData("24/08/26")]
    [InlineData("demain")]
    public void ATextThatIsNoDate_MarksTheInputInvalid_AndFiltersNothing(string text)
    {
        using var _ = French();
        var grid = RenderInline();

        grid.Find(TextInRow).Change(text);

        var input = grid.Find(TextInRow);
        Assert.Equal("true", input.GetAttribute("aria-invalid"));
        // The text stays for the reader to correct, and every row stays.
        Assert.Equal(text, input.GetAttribute("value"));
        Assert.Equal(4, grid.FindAll("tbody tr").Count);
        Assert.Empty(grid.FindAll(".omni-data-grid__filter-reset"));
    }

    [Fact]
    public void AnInvalidTextOverAnAppliedFilter_RemovesTheFilter_ThenCorrectingItFiltersAgain()
    {
        using var _ = French();
        var grid = RenderInline();

        grid.Find(TextInRow).Change("24/08/2026");
        Assert.Equal(2, grid.FindAll("tbody tr").Count);

        grid.Find(TextInRow).Change("24/08/20260");
        Assert.Equal("true", grid.Find(TextInRow).GetAttribute("aria-invalid"));
        Assert.Equal(4, grid.FindAll("tbody tr").Count);

        grid.Find(TextInRow).Change("25/08/2026");
        Assert.Null(grid.Find(TextInRow).GetAttribute("aria-invalid"));
        Assert.Single(grid.FindAll("tbody tr"));
    }

    [Fact]
    public void ABlankText_ClearsTheFilter()
    {
        using var _ = French();
        var grid = RenderInline();
        grid.Find(TextInRow).Change("24/08/2026");

        grid.Find(TextInRow).Change("   ");

        Assert.Null(grid.Find(TextInRow).GetAttribute("aria-invalid"));
        Assert.Equal(4, grid.FindAll("tbody tr").Count);
    }

    [Fact]
    public void TheResetCross_EmptiesTheText()
    {
        using var _ = French();
        var grid = RenderInline();
        grid.Find(TextInRow).Change("24/08/2026");

        grid.Find("tr.omni-data-grid__filters td[data-omni-col=\"Opened\"] .omni-data-grid__filter-reset").Click();

        Assert.Equal(string.Empty, grid.Find(TextInRow).GetAttribute("value") ?? string.Empty);
        Assert.Equal(4, grid.FindAll("tbody tr").Count);
    }

    [Fact]
    public void WithoutAdvancedFilter_TheHeaderHasNoRangeMenu()
    {
        var grid = RenderInline();

        Assert.Empty(grid.FindAll(RangeMenu));
        Assert.Empty(grid.FindAll("th[data-omni-col=\"Opened\"] details"));
    }

    [Fact]
    public void WithAdvancedFilter_TheInlineTextStays_AndTheHeaderMenuHoldsTheRange()
    {
        using var _ = French();
        var grid = RenderInline(advanced: true);

        Assert.NotNull(grid.Find(TextInRow));
        Assert.NotNull(grid.Find(RangeMenu));
        Assert.Empty(grid.FindAll("th[data-omni-col=\"Opened\"] .omni-data-grid__date-text"));
        var toggle = grid.Find("th[data-omni-col=\"Opened\"] details.omni-data-grid__filter-menu > summary");
        Assert.False(string.IsNullOrWhiteSpace(toggle.GetAttribute("aria-label")));
        // Only the date column has a header menu: the inline filter row serves the others.
        Assert.Single(grid.FindAll("th details"));
    }

    [Fact]
    public void TheRangeOfTheMenu_AcceptsAFromAlone_AndTheTextShowsIt()
    {
        using var _ = French();
        var grid = RenderInline(advanced: true);

        grid.Find(RangeMenu + " .omni-data-grid__date-range-start").Change("2026-08-25");

        Assert.Equal(2, grid.FindAll("tbody tr").Count);
        Assert.Equal("25/08/2026 -", grid.Find(TextInRow).GetAttribute("value"));
    }

    [Fact]
    public void TheRangeOfTheMenu_AcceptsAToAlone()
    {
        using var _ = French();
        var grid = RenderInline(advanced: true);

        grid.Find(RangeMenu + " .omni-data-grid__date-range-end").Change("2026-08-24");

        Assert.Equal(2, grid.FindAll("tbody tr").Count);
        Assert.Equal("- 24/08/2026", grid.Find(TextInRow).GetAttribute("value"));
    }

    [Fact]
    public void TheTextAndTheRange_EditOneFilter_TheLastOneUsedWins()
    {
        using var _ = French();
        var grid = RenderInline(advanced: true);

        // A month typed in the row shows as its first and last day in the menu.
        grid.Find(TextInRow).Change("08/2026");
        Assert.Equal("2026-08-01", grid.Find(RangeMenu + " .omni-data-grid__date-range-start").GetAttribute("value"));
        Assert.Equal("2026-08-31", grid.Find(RangeMenu + " .omni-data-grid__date-range-end").GetAttribute("value"));
        Assert.Equal(4, grid.FindAll("tbody tr").Count);

        // Narrowing the range from the menu replaces the typed month, and the text shows the range.
        grid.Find(RangeMenu + " .omni-data-grid__date-range-end").Change("2026-08-25");
        Assert.Equal(3, grid.FindAll("tbody tr").Count);
        Assert.Equal("01/08/2026 - 25/08/2026", grid.Find(TextInRow).GetAttribute("value"));

        // A day typed in the row replaces the range.
        grid.Find(TextInRow).Change("30/08/2026");
        Assert.Single(grid.FindAll("tbody tr"));
        Assert.Equal("2026-08-30", grid.Find(RangeMenu + " .omni-data-grid__date-range-start").GetAttribute("value"));
        Assert.Equal("2026-08-30", grid.Find(RangeMenu + " .omni-data-grid__date-range-end").GetAttribute("value"));
    }

    [Fact]
    public void WithTheHeaderFilterMenu_ThePopoverHoldsTheTextAlone_OrTheTextAndTheRange()
    {
        var plain = Render<DataGridFilterKindsTestHost>(parameters => parameters
            .Add(component => component.DateFilter, OmniDataGridColumnFilterType.Text));
        Assert.NotNull(plain.Find(TextInHeader));
        Assert.Empty(plain.FindAll("th[data-omni-col=\"Opened\"] .omni-data-grid__date-range"));

        var advanced = Render<DataGridFilterKindsTestHost>(parameters => parameters
            .Add(component => component.DateFilter, OmniDataGridColumnFilterType.Text)
            .Add(component => component.AdvancedDate, true));
        Assert.NotNull(advanced.Find(TextInHeader));
        Assert.Single(advanced.FindAll("th[data-omni-col=\"Opened\"] .omni-data-grid__date-range"));
        // One popover, not a second menu beside it.
        Assert.Single(advanced.FindAll("th[data-omni-col=\"Opened\"] details"));
    }

    [Fact]
    public void InTheAdvancedMode_TheDateKeepsItsSingleEditor_AndAppliesAsItChanges()
    {
        using var _ = French();
        var grid = Render<DataGridFilterKindsTestHost>(parameters => parameters
            .Add(component => component.DateFilter, OmniDataGridColumnFilterType.Text)
            .Add(component => component.HeaderMenu, false)
            .Add(component => component.AdvancedDate, true)
            .Add(component => component.FilterMode, OmniDataGridFilterMode.Advanced));

        Assert.Empty(grid.FindAll("td[data-omni-col=\"Opened\"] .omni-data-grid__filter-operator"));
        Assert.Empty(grid.FindAll("th[data-omni-col=\"Opened\"] details"));
        // The row's popover holds the text and the range.
        Assert.Single(grid.FindAll("td[data-omni-col=\"Opened\"] .omni-data-grid__date-range"));

        grid.Find("td[data-omni-col=\"Opened\"] input.omni-data-grid__date-text").Change("25/08/2026");

        Assert.Single(grid.FindAll("tbody tr"));
    }

    [Fact]
    public void ADateColumnThatNamesTheRangeType_KeepsThePickersInline()
    {
        var grid = Render<DataGridFilterKindsTestHost>(parameters => parameters
            .Add(component => component.HeaderMenu, false));

        Assert.NotNull(grid.Find(".omni-data-grid__date-range-start"));
        Assert.Empty(grid.FindAll(".omni-data-grid__date-text"));
    }

    [Fact]
    public void APickerYearOfFiveDigits_IsFlaggedInvalid_AndTheSideFiltersNothing()
    {
        var grid = Render<DataGridFilterKindsTestHost>();
        var start = grid.Find("th[data-omni-col=\"Opened\"] .omni-data-grid__date-range-start");
        Assert.Equal("0001-01-01", start.GetAttribute("min"));
        Assert.Equal("9999-12-31", start.GetAttribute("max"));
        Assert.Null(start.GetAttribute("aria-invalid"));

        start.Change("20000-01-01");

        start = grid.Find("th[data-omni-col=\"Opened\"] .omni-data-grid__date-range-start");
        Assert.Equal("true", start.GetAttribute("aria-invalid"));
        Assert.Equal(4, grid.FindAll("tbody tr").Count);

        start.Change("2026-08-25");
        Assert.Null(grid.Find("th[data-omni-col=\"Opened\"] .omni-data-grid__date-range-start").GetAttribute("aria-invalid"));
        Assert.Equal(2, grid.FindAll("tbody tr").Count);
    }

    [Fact]
    public void ARemoteGrid_ReceivesTheTypedPeriodAsTwoBounds()
    {
        using var _ = French();
        var requests = new List<OmniDataGridLoadRequest>();
        var grid = Render<DataGridFilterKindsTestHost>(parameters => parameters
            .Add(component => component.DateFilter, OmniDataGridColumnFilterType.Text)
            .Add(component => component.HeaderMenu, false)
            .Add(component => component.Load, request =>
            {
                requests.Add(request);
                return Task.FromResult(new OmniDataGridResult<Ticket>([], 0));
            }));

        grid.Find(TextInRow).Change("08/2026");

        var filter = Assert.Single(requests[^1].Filters);
        Assert.Equal("Opened", filter.Key);
        Assert.Equal(OmniDataGridFilterOperator.GreaterThanOrEquals, filter.Operator);
        Assert.Equal("2026-08-01T00:00:00", filter.Value);
        Assert.Equal(OmniDataGridFilterOperator.LessThan, filter.SecondOperator);
        Assert.Equal("2026-09-01T00:00:00", filter.SecondValue);

        grid.Find(TextInRow).Change("20000");

        Assert.Empty(requests[^1].Filters);
    }

    [Fact]
    public async Task AFilterSetFromCode_ShowsInTheTextInput()
    {
        using var _ = French();
        var grid = RenderInline();

        await grid.InvokeAsync(() => grid.Instance.Grid!.SetFiltersAsync(
            new Dictionary<string, string?> { ["Opened"] = "2026-08-01/2026-08-31" }, replace: true));

        Assert.Equal("08/2026", grid.Find(TextInRow).GetAttribute("value"));
        Assert.Equal(4, grid.FindAll("tbody tr").Count);
    }
}
