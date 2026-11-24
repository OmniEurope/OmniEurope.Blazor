using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Version 3 of the saved grid state: the page size, the groups and the frozen columns the reader chose
/// survive a reload (a new circuit over the same browser storage), a state of version 2 still reads without
/// loss, and the acceptance case of the client application (sort, unfreeze the first frozen column, reload) comes back identical.
/// </summary>
public sealed class DataGridStateVersion3Tests : OmniBunitContext
{
    public sealed record Row(string Name, int Amount, string City);

    private static readonly IReadOnlyList<Row> Rows =
        [.. Enumerable.Range(1, 30).Select(index => new Row($"n{index:00}", 31 - index, index % 2 == 0 ? "Lyon" : "Gand"))];

    /// <summary>A second circuit over the same browser storage: what a reload of the page sees.</summary>
    private sealed class Reloaded : OmniBunitContext;

    private static RenderFragment Columns(bool nameFrozen = false, bool amountFrozen = false) =>
        DataGridTestSetup.PropertyColumns<Row>(
            ("Name", [(nameof(OmniDataGridColumn<Row>.Frozen), nameFrozen)]),
            ("Amount", [(nameof(OmniDataGridColumn<Row>.Frozen), amountFrozen)]),
            ("City", []));

    private static IRenderedComponent<OmniDataGrid<Row>> RenderGrid(
        BunitContext context,
        Action<ComponentParameterCollectionBuilder<OmniDataGrid<Row>>>? more = null,
        RenderFragment? columns = null) =>
        context.Render<OmniDataGrid<Row>>(parameters =>
        {
            parameters
                .Add(component => component.Items, Rows)
                .Add(component => component.KeyOf, row => row.Name)
                .Add(component => component.StateKey, "orders")
                .Add(component => component.Columns, columns ?? Columns());
            more?.Invoke(parameters);
        });

    /// <summary>The same browser storage in a new circuit, as after a reload.</summary>
    private static (Reloaded Context, GridPreferenceHarness Preferences) Reload(GridPreferenceHarness before)
    {
        var context = new Reloaded();
        var preferences = new GridPreferenceHarness(context.Services);
        foreach (var (key, value) in before.Browser.Storage)
        {
            preferences.Browser.Storage[key] = value;
        }

        return (context, preferences);
    }

    private static string[] HeaderKeys(IRenderedComponent<OmniDataGrid<Row>> grid) =>
        [.. grid.FindAll("thead tr:first-child th[data-omni-col]").Select(cell => cell.GetAttribute("data-omni-col")!)];

    private static string[] FrozenHeaderKeys(IRenderedComponent<OmniDataGrid<Row>> grid) =>
        [.. grid.FindAll("thead tr:first-child th[data-omni-col].omni-data-grid__column--frozen").Select(cell => cell.GetAttribute("data-omni-col")!)];

    private static string[] FirstColumn(IRenderedComponent<OmniDataGrid<Row>> grid) =>
        [.. grid.FindAll("tbody tr[data-omni-row-index] td[data-omni-col='Name']").Select(cell => cell.TextContent.Trim())];

    [Fact]
    public async Task ThePageSizeTheReaderPicked_IsSaved_AndComesBackAfterAReload_ReportedToTheHost()
    {
        var preferences = new GridPreferenceHarness(Services);
        var grid = RenderGrid(this, parameters => parameters
            .Add(component => component.PageSize, 10)
            .Add(component => component.PageSizeOptions, [10, 20]));

        await grid.InvokeAsync(() => grid.Instance.Paging.ChangePageSizeAsync(20));

        var saved = preferences.Saved("orders")!.Value;
        Assert.Equal(3, saved.GetProperty("version").GetInt32());
        Assert.Equal(20, saved.GetProperty("pageSize").GetInt32());

        var (reloaded, _) = Reload(preferences);
        await using (reloaded)
        {
            var reported = new List<int>();
            var again = RenderGrid(reloaded, parameters => parameters
                .Add(component => component.PageSize, 10)
                .Add(component => component.PageSizeOptions, [10, 20])
                .Add(component => component.PageSizeChanged, size => reported.Add(size)));

            Assert.Equal(20, again.FindAll("tbody tr[data-omni-row-index]").Count);
            Assert.Equal([20], reported);
        }
    }

    [Fact]
    public void ASavedPageSize_ThePagerDoesNotOffer_IsLeftAside()
    {
        var preferences = new GridPreferenceHarness(Services);
        preferences.Seed("orders", """{"version":3,"pageSize":50}""");

        var grid = RenderGrid(this, parameters => parameters
            .Add(component => component.PageSize, 10)
            .Add(component => component.PageSizeOptions, [10, 20]));

        Assert.Equal(10, grid.FindAll("tbody tr[data-omni-row-index]").Count);
    }

    [Fact]
    public void AnUntouchedPageSize_IsNotSaved_SoTheHostsDefaultStillRules()
    {
        var preferences = new GridPreferenceHarness(Services);
        var grid = RenderGrid(this, parameters => parameters
            .Add(component => component.PageSize, 10)
            .Add(component => component.PageSizeOptions, [10, 20]));

        grid.Find("thead th[data-omni-col='Amount'] .omni-data-grid__sort").Click();

        var saved = preferences.Saved("orders")!.Value;
        Assert.Equal(JsonValueKind.Null, saved.GetProperty("pageSize").ValueKind);
        Assert.Equal(JsonValueKind.Null, saved.GetProperty("groups").ValueKind);
    }

    [Fact]
    public async Task TheGroupsTheReaderSet_AreSaved_AndComeBackAfterAReload_ReportedToTheHost()
    {
        var preferences = new GridPreferenceHarness(Services);
        var grid = RenderGrid(this, parameters => parameters.Add(component => component.AllowGrouping, true));

        // No @bind-Groups: the grid keeps its own groups.
        grid.Find("thead th[data-omni-col='City'] .omni-data-grid__group-toggle").Click();

        Assert.Equal([new OmniDataGridGroup("City")], grid.Instance.Grouping.ActiveGroups);
        var groups = preferences.Saved("orders")!.Value.GetProperty("groups");
        Assert.Equal("City", groups[0].GetProperty("key").GetString());

        var (reloaded, _) = Reload(preferences);
        await using (reloaded)
        {
            var reported = new List<IReadOnlyList<OmniDataGridGroup>>();
            var again = RenderGrid(reloaded, parameters => parameters
                .Add(component => component.AllowGrouping, true)
                .Add(component => component.GroupsChanged, value => reported.Add(value)));

            Assert.Equal([new OmniDataGridGroup("City")], again.Instance.Grouping.ActiveGroups);
            Assert.Equal([new OmniDataGridGroup("City")], Assert.Single(reported));
            Assert.Equal("true", again.Find("thead th[data-omni-col='City'] .omni-data-grid__group-toggle").GetAttribute("aria-pressed"));
        }
    }

    [Fact]
    public void SavedGroups_AreNotApplied_WhileTheGridDoesNotGroup()
    {
        var preferences = new GridPreferenceHarness(Services);
        preferences.Seed("orders", """{"version":3,"groups":[{"key":"City","descending":false}]}""");

        var grid = RenderGrid(this);

        Assert.Empty(grid.Instance.Grouping.ActiveGroups);
    }

    [Fact]
    public void AStateOfVersion2_ReadsWithoutLoss_AndKeepsThePageSizeGroupsAndFrozenColumnsDeclared()
    {
        var preferences = new GridPreferenceHarness(Services);
        preferences.Seed("orders", """
            {"version":2,"filters":{},"sorts":[{"key":"Amount","descending":false}],"columnWidths":{"City":"180px"},
             "columnOrder":["City","Amount","Name"],"columnVisibility":{"Amount":false}}
            """);

        var grid = RenderGrid(this, parameters => parameters
            .Add(component => component.PageSize, 10)
            .Add(component => component.PageSizeOptions, [10, 20])
            .Add(component => component.AllowGrouping, true)
            .Add(component => component.AllowColumnFreeze, true)
            .Add(component => component.AllowColumnHide, true)
            .Add(component => component.AllowColumnReorder, true),
            Columns(nameFrozen: true));

        // The order and the visibility of version 2 come back, the declared frozen column keeps its place.
        Assert.Equal(["Name", "City"], HeaderKeys(grid));
        Assert.Equal(["Name"], FrozenHeaderKeys(grid));
        Assert.Equal("n30", FirstColumn(grid)[0]);
        Assert.Equal("180px", grid.Instance.ColumnLayout.Widths["City"]);
        Assert.Equal(10, grid.FindAll("tbody tr[data-omni-row-index]").Count);
        Assert.Empty(grid.Instance.Grouping.ActiveGroups);
    }

    [Fact]
    public async Task SortThenUnfreezeTheFirstFrozenColumn_ThenReload_GivesTheSameGrid()
    {
        // Acceptance case of the client application.
        var preferences = new GridPreferenceHarness(Services);
        Action<ComponentParameterCollectionBuilder<OmniDataGrid<Row>>> options = parameters => parameters
            .Add(component => component.AllowColumnFreeze, true);
        var grid = RenderGrid(this, options, Columns(nameFrozen: true, amountFrozen: true));
        Assert.Equal(["Name", "Amount"], FrozenHeaderKeys(grid));

        grid.Find("thead th[data-omni-col='Amount'] .omni-data-grid__sort").Click();
        grid.Find(".omni-data-grid__chooser-toggle").Click();
        grid.Find(".omni-data-grid__chooser-freeze[data-omni-col='Name']").Click();

        var headers = HeaderKeys(grid);
        var frozen = FrozenHeaderKeys(grid);
        var rows = FirstColumn(grid);
        Assert.Equal(["Amount", "Name", "City"], headers);
        Assert.Equal(["Amount"], frozen);
        Assert.Equal("n30", rows[0]);

        var (reloaded, _) = Reload(preferences);
        await using (reloaded)
        {
            var again = RenderGrid(reloaded, options, Columns(nameFrozen: true, amountFrozen: true));

            Assert.Equal(headers, HeaderKeys(again));
            Assert.Equal(frozen, FrozenHeaderKeys(again));
            Assert.Equal(rows, FirstColumn(again));
            Assert.Equal("Amount", again.Instance.Query.Sorts.Single().Key);
        }
    }

    [Fact]
    public void ASavedFrozenChoice_FreezesNothing_WhileTheGridDoesNotAllowIt()
    {
        var preferences = new GridPreferenceHarness(Services);
        preferences.Seed("orders", """{"version":3,"columnFrozen":{"City":true,"Name":false}}""");

        var grid = RenderGrid(this, columns: Columns(nameFrozen: true));

        Assert.Equal(["Name", "Amount", "City"], HeaderKeys(grid));
        Assert.Equal(["Name"], FrozenHeaderKeys(grid));

        grid.Render(parameters => parameters.Add(component => component.AllowColumnFreeze, true));

        // Frozen first, then the others in their order.
        Assert.Equal(["City", "Name", "Amount"], HeaderKeys(grid));
        Assert.Equal(["City"], FrozenHeaderKeys(grid));
    }
}
