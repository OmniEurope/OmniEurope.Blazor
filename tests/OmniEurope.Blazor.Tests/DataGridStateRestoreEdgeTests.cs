using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

using Row = OmniEurope.Blazor.Tests.OrderRow;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The edges of the saved grid state of version 3: a host without a route to key the state by, a saved page
/// size or saved groups equal to what the grid already shows, groups the host replaces after the reader chose
/// others, grids that cannot take saved groups, groups written on another device, and freeze requests that
/// change nothing.
/// </summary>
public sealed class DataGridStateRestoreEdgeTests : OmniBunitContext
{
    private static readonly IReadOnlyList<Row> Rows = DataGridTestSetup.ThirtyOrders;

    private static readonly RenderFragment Columns = DataGridTestSetup.NameFrozenOrderColumns;

    private const string CityGroup = """{"version":3,"groups":[{"key":"City","descending":false}]}""";

    private IRenderedComponent<OmniDataGrid<Row>> RenderGrid(Action<ComponentParameterCollectionBuilder<OmniDataGrid<Row>>>? more = null) =>
        Render<OmniDataGrid<Row>>(parameters =>
        {
            parameters
                .Add(component => component.Items, Rows)
                .Add(component => component.KeyOf, row => row.Name)
                .Add(component => component.StateKey, "orders")
                .Add(component => component.Columns, Columns);
            more?.Invoke(parameters);
        });

    /// <summary>A navigation manager the host never initialized: reading its address throws.</summary>
    private sealed class UninitializedNavigation : NavigationManager;

    [Fact]
    public void WithoutANavigationOrWithAnUninitializedOne_NoKeyIsDerived()
    {
        Assert.Null(GridStateKey.Derive(null, "main", () => throw new InvalidOperationException("not read")));
        Assert.Null(GridStateKey.Derive(new UninitializedNavigation(), "main", () => throw new InvalidOperationException("not read")));
    }

    [Fact]
    public void ASavedPageSize_EqualToTheDeclaredOne_IsKeptAsTheChoice_WithoutAChange()
    {
        var preferences = new GridPreferenceHarness(Services);
        preferences.Seed("orders", """{"version":3,"pageSize":10}""");
        var reported = new List<int>();

        var grid = RenderGrid(parameters => parameters
            .Add(component => component.PageSize, 10)
            .Add(component => component.PageSizeOptions, [10, 20])
            .Add(component => component.PageSizeChanged, size => reported.Add(size)));

        Assert.Equal(10, grid.Instance.Paging.ChosenPageSize);
        Assert.Equal(10, grid.FindAll("tbody tr[data-omni-row-index]").Count);
        Assert.Empty(reported);
    }

    [Fact]
    public void SavedGroups_EqualToTheHostsGroups_AreKeptAsTheChoice_WithoutAChange()
    {
        var preferences = new GridPreferenceHarness(Services);
        preferences.Seed("orders", CityGroup);
        var reported = new List<IReadOnlyList<OmniDataGridGroup>>();

        var grid = RenderGrid(parameters => parameters
            .Add(component => component.AllowGrouping, true)
            .Add(component => component.Groups, [new OmniDataGridGroup("City")])
            .Add(component => component.GroupsChanged, value => reported.Add(value)));

        Assert.Equal([new OmniDataGridGroup("City")], grid.Instance.Grouping.ActiveGroups);
        Assert.Equal([new OmniDataGridGroup("City")], grid.Instance.Grouping.ChosenGroups);
        Assert.Empty(reported);
    }

    [Fact]
    public void NewGroupsFromTheHost_ReplaceTheReadersChoice()
    {
        _ = new GridPreferenceHarness(Services);
        var grid = RenderGrid(parameters => parameters.Add(component => component.AllowGrouping, true));
        grid.Find("thead th[data-omni-col='City'] .omni-data-grid__group-toggle").Click();
        Assert.Equal([new OmniDataGridGroup("City")], grid.Instance.Grouping.ChosenGroups);

        grid.Render(parameters => parameters.Add(component => component.Groups, [new OmniDataGridGroup("Amount")]));

        Assert.Equal([new OmniDataGridGroup("Amount")], grid.Instance.Grouping.ActiveGroups);
        Assert.Null(grid.Instance.Grouping.ChosenGroups);
    }

    [Fact]
    public void SavedGroups_AreNotApplied_ToATreeGrid()
    {
        var preferences = new GridPreferenceHarness(Services);
        preferences.Seed("orders", CityGroup);

        var grid = RenderGrid(parameters => parameters
            .Add(component => component.AllowGrouping, true)
            .Add(component => component.ChildrenOf, (Func<Row, IReadOnlyList<Row>?>)(_ => null)));

        Assert.Empty(grid.Instance.Grouping.ActiveGroups);
        Assert.Null(grid.Instance.Grouping.ChosenGroups);
    }

    [Fact]
    public void SavedGroups_AreNotApplied_ToARemoteVirtualizedGrid()
    {
        var preferences = new GridPreferenceHarness(Services);
        preferences.Seed("orders", CityGroup);

        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.KeyOf, row => row.Name)
            .Add(component => component.StateKey, "orders")
            .Add(component => component.Columns, Columns)
            .Add(component => component.AllowGrouping, true)
            .Add(component => component.ScrollMode, OmniDataGridScrollMode.Virtual)
            .Add(component => component.Load, _ => Task.FromResult(new OmniDataGridResult<Row>(Rows, Rows.Count))));

        Assert.Null(grid.Instance.Grouping.ChosenGroups);
    }

    [Fact]
    public async Task GroupsWrittenOnAnotherDevice_RegroupTheRowsOnScreen()
    {
        var preferences = new GridPreferenceHarness(Services, withStore: true);
        var grid = RenderGrid(parameters => parameters.Add(component => component.AllowGrouping, true));
        Assert.Empty(grid.Instance.Grouping.ActiveGroups);

        preferences.Store!.Put("omni.grid.orders", CityGroup, DateTimeOffset.UtcNow.AddMinutes(1));
        await preferences.Preferences!.SyncAsync(Xunit.TestContext.Current.CancellationToken);
        await grid.InvokeAsync(() => grid.Instance.Persistence.WhenAppliedAsync());

        Assert.Equal([new OmniDataGridGroup("City")], grid.Instance.Grouping.ActiveGroups);
        await grid.WaitForAssertionAsync(() => Assert.NotEmpty(grid.FindAll("tbody tr.omni-data-grid__group")));
    }

    [Fact]
    public async Task AFreezeThatChangesNothing_IsRefused_AndAnnouncesNothing()
    {
        var preferences = new GridPreferenceHarness(Services);
        var grid = RenderGrid(parameters => parameters.Add(component => component.AllowColumnFreeze, true));
        var name = grid.Instance.ColumnSet.Find("Name")!;

        await grid.InvokeAsync(async () =>
        {
            Assert.False(await grid.Instance.Arrangement.SetFrozenAsync("Missing", true));
            Assert.False(await grid.Instance.Arrangement.SetFrozenAsync("Name", true));
            await grid.Instance.Chooser.FreezeAsync(name, frozen: true);
        });

        Assert.Equal(string.Empty, grid.Find(".omni-data-grid > [role=status][aria-live=polite]").TextContent.Trim());
        Assert.Null(preferences.Saved("orders"));
    }

    [Fact]
    public async Task AFreeze_IsRefused_WhileTheGridDoesNotLetTheReaderFreeze()
    {
        _ = new GridPreferenceHarness(Services);
        var grid = RenderGrid();
        var amount = grid.Instance.ColumnSet.Find("Amount")!;

        await grid.InvokeAsync(async () =>
        {
            Assert.False(await grid.Instance.Arrangement.SetFrozenAsync("Amount", true));
            await grid.Instance.Chooser.FreezeAsync(amount, frozen: true);
        });

        Assert.False(grid.Instance.Arrangement.IsFrozen(amount));
    }

    [Fact]
    public async Task ADeclaredFrozenColumn_UnfrozenThenFrozenAgain_IsSavedAsDeclared()
    {
        var preferences = new GridPreferenceHarness(Services);
        var grid = RenderGrid(parameters => parameters.Add(component => component.AllowColumnFreeze, true));

        await grid.InvokeAsync(async () =>
        {
            Assert.True(await grid.Instance.Arrangement.SetFrozenAsync("Name", false));
            Assert.True(await grid.Instance.Arrangement.SetFrozenAsync("Name", true));
        });

        Assert.True(grid.Instance.Arrangement.IsFrozen(grid.Instance.ColumnSet.Find("Name")!));
        var frozen = preferences.Saved("orders")!.Value.GetProperty("columnFrozen");
        Assert.False(frozen.ValueKind == System.Text.Json.JsonValueKind.Object && frozen.TryGetProperty("Name", out _));
    }
}
