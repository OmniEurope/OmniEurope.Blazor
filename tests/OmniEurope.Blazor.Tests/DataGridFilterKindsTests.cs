using Bunit;
using OmniEurope.Blazor.Components;
using static OmniEurope.Blazor.Tests.DataGridFilterKindsTestHost;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Covers the filter additions a remote, translated application needs: every enum member as a
/// candidate with its own text, a default filter shown in the column, filters set from code, and
/// the date range with its whole-day rule, locally and as bounds sent to a loader.
/// </summary>
public sealed class DataGridFilterKindsTests : OmniBunitContext
{
    private const string StatusOptions = "th[data-omni-col=\"Status\"] .omni-multi-select__option";

    [Fact]
    public void EnumColumn_OffersEveryMember_WithTheColumnText()
    {
        var grid = Render<DataGridFilterKindsTestHost>();

        // Pending is on no row, and is a valid choice all the same; members keep declaration order.
        var options = grid.FindAll(StatusOptions).Select(option => option.TextContent.Trim()).ToArray();
        Assert.Equal(["T:Open", "T:Pending", "T:Closed"], options);
    }

    [Fact]
    public void TickingATranslatedCandidate_FiltersOnItsValue()
    {
        var grid = Render<DataGridFilterKindsTestHost>();

        grid.FindAll("th[data-omni-col=\"Status\"] .omni-multi-select__checkbox")[2].Change(true);

        Assert.Equal(2, grid.FindAll("tbody tr").Count);
        Assert.All(grid.FindAll("tbody tr"), row => Assert.Contains("Closed", row.TextContent, StringComparison.Ordinal));
    }

    [Fact]
    public void DefaultFilter_IsAppliedAndShownInTheColumn_AndCanBeCleared()
    {
        var grid = Render<DataGridFilterKindsTestHost>(parameters => parameters
            .Add(component => component.StatusDefault, nameof(TicketStatus.Open)));

        Assert.Equal(2, grid.FindAll("tbody tr").Count);
        Assert.Contains("omni-data-grid__filter-menu-toggle--active",
            grid.Find("th[data-omni-col=\"Status\"] .omni-data-grid__filter-menu-toggle").ClassName, StringComparison.Ordinal);
        Assert.Equal("true", grid.FindAll("th[data-omni-col=\"Status\"] .omni-multi-select__option")[0].GetAttribute("aria-selected"));

        grid.Find("th[data-omni-col=\"Status\"] .omni-data-grid__filter-reset").Click();

        Assert.Equal(4, grid.FindAll("tbody tr").Count);
    }

    [Fact]
    public async Task SetFiltersAsync_ReplacesTheFilters_AsTheHeadersWould()
    {
        var grid = Render<DataGridFilterKindsTestHost>(parameters => parameters
            .Add(component => component.StatusDefault, nameof(TicketStatus.Open)));

        await grid.InvokeAsync(() => grid.Instance.Grid!.SetFiltersAsync(
            new Dictionary<string, string?> { ["Opened"] = "2026-08-30/" }, replace: true));

        var row = Assert.Single(grid.FindAll("tbody tr"));
        Assert.Contains("Closed", row.TextContent, StringComparison.Ordinal);
        // The status default is gone with the replace, and does not come back on the next render.
        grid.Render();
        Assert.Single(grid.FindAll("tbody tr"));
    }

    [Fact]
    public void DateRange_ADayAlone_CoversTheWholeDay()
    {
        var grid = Render<DataGridFilterKindsTestHost>();

        grid.Find("th[data-omni-col=\"Opened\"] .omni-data-grid__date-range-start").Change("2026-08-24");
        grid.Find("th[data-omni-col=\"Opened\"] .omni-data-grid__date-range-end").Change("2026-08-24");

        // Midnight and 23:30 of the 24th are in; midnight of the 25th is not.
        Assert.Equal(2, grid.FindAll("tbody tr").Count);
    }

    [Fact]
    public void DateRange_InAdvancedMode_KeepsItsOwnEditor_AndAppliesAsItChanges()
    {
        var grid = Render<DataGridFilterKindsTestHost>(parameters => parameters
            .Add(component => component.FilterMode, OmniDataGridFilterMode.Advanced));

        Assert.Empty(grid.FindAll("th[data-omni-col=\"Opened\"] .omni-data-grid__filter-operator"));
        Assert.Empty(grid.FindAll("th[data-omni-col=\"Status\"] .omni-data-grid__filter-operator"));

        grid.Find("th[data-omni-col=\"Opened\"] .omni-data-grid__date-range-start").Change("2026-08-25");

        Assert.Equal(2, grid.FindAll("tbody tr").Count);
    }

    [Fact]
    public void DateRange_WithTime_UsesDateTimePickers_AndIncludesThePickedMinute()
    {
        var grid = Render<DataGridFilterKindsTestHost>(parameters => parameters
            .Add(component => component.IncludesTime, true));

        var end = grid.Find("th[data-omni-col=\"Opened\"] .omni-data-grid__date-range-end");
        Assert.Equal("datetime-local", end.GetAttribute("type"));
        end.Change("2026-08-24T23:30");

        Assert.Equal(2, grid.FindAll("tbody tr").Count);
    }

    [Fact]
    public void RemoteGrid_ReceivesTheDateRangeAsTwoBounds_AndEnumsStillListEveryMember()
    {
        var requests = new List<OmniDataGridLoadRequest>();
        var grid = Render<DataGridFilterKindsTestHost>(parameters => parameters
            .Add(component => component.Load, request =>
            {
                requests.Add(request);
                return Task.FromResult(new OmniDataGridResult<Ticket>([], 0));
            }));

        Assert.Equal(3, grid.FindAll(StatusOptions).Count);

        grid.Find("th[data-omni-col=\"Opened\"] .omni-data-grid__date-range-start").Change("2026-08-24");
        grid.Find("th[data-omni-col=\"Opened\"] .omni-data-grid__date-range-end").Change("2026-08-30");

        var filter = Assert.Single(requests[^1].Filters);
        Assert.Equal("Opened", filter.Key);
        Assert.Equal(OmniDataGridFilterOperator.GreaterThanOrEquals, filter.Operator);
        Assert.Equal("2026-08-24T00:00:00", filter.Value);
        Assert.Equal(OmniDataGridFilterOperator.LessThan, filter.SecondOperator);
        Assert.Equal("2026-08-31T00:00:00", filter.SecondValue);
    }

    [Fact]
    public void AdvancedMenu_OffersOnlyTheOperatorsTheColumnTypeSupports()
    {
        var grid = Render<DataGridFilterKindsTestHost>(parameters => parameters
            .Add(component => component.FilterMode, OmniDataGridFilterMode.Advanced));

        var offered = grid.FindAll("th[data-omni-col=\"Id\"] .omni-data-grid__filter-operator")[0]
            .QuerySelectorAll("option").Select(option => option.GetAttribute("value")).ToArray();

        // A number is ordered, never searched as text, and never a checkable list.
        Assert.Contains(nameof(OmniDataGridFilterOperator.GreaterThan), offered);
        Assert.DoesNotContain(nameof(OmniDataGridFilterOperator.Contains), offered);
        Assert.DoesNotContain(nameof(OmniDataGridFilterOperator.In), offered);
        // And its default condition is one it offers.
        Assert.Equal(nameof(OmniDataGridFilterOperator.Equals),
            grid.FindAll("th[data-omni-col=\"Id\"] .omni-data-grid__filter-operator option[selected]")[0].GetAttribute("value"));
    }

    [Theory]
    [InlineData("2026-08-24/2026-08-24", "2026-08-24T00:00:00", "2026-08-25T00:00:00")]
    [InlineData("2026-08-24/", "2026-08-24T00:00:00", null)]
    [InlineData("/2026-08-30", null, "2026-08-31T00:00:00")]
    [InlineData("2026-08-24T10:00/2026-08-24T12:30", "2026-08-24T10:00:00", "2026-08-24T12:31:00")]
    [InlineData("garbage/2026-08-30", null, "2026-08-31T00:00:00")]
    public void DateRange_Resolve_AppliesTheWholeDayAndWholeMinuteRules(string value, string? start, string? endExclusive)
    {
        var (from, to) = OmniDataGridDateRange.Resolve(value);

        Assert.Equal(start, from is { } f ? OmniDataGridDateRange.FormatBound(f) : null);
        Assert.Equal(endExclusive, to is { } t ? OmniDataGridDateRange.FormatBound(t) : null);
    }

    [Fact]
    public void DateRange_JoinAndSplit_KeepAMissingSide()
    {
        Assert.Equal(string.Empty, OmniDataGridDateRange.Join(null, " "));
        Assert.Equal("/2026-08-30", OmniDataGridDateRange.Join(null, "2026-08-30"));
        Assert.Equal((string.Empty, "2026-08-30"), OmniDataGridDateRange.Split("/2026-08-30"));
    }
}
