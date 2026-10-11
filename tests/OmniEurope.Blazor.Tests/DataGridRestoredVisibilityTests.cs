using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A saved grid state comes from elsewhere (the host's store, another tab, an older layout): its column
/// visibility is constrained to what the reader could undo from the chooser, and the former browser entry
/// is imported only where it cannot belong to another user, without failing on a refused storage.
/// </summary>
public sealed class DataGridRestoredVisibilityTests : OmniBunitContext
{
    public sealed record Row(string Name, int Amount, string City);

    private static readonly IReadOnlyList<Row> Rows = [new("a", 1, "Lyon"), new("b", 2, "Gand"), new("c", 3, "Bari")];

    private static readonly string OldFormat =
        $$$"""{"Filters":{"Name":{"Operator":{{{(int)OmniDataGridFilterOperator.Contains}}},"Value":"b","LogicalOperator":0,"SecondOperator":0,"SecondValue":""}},"Sorts":[],"ColumnWidths":{}}""";

    private static RenderFragment Columns(params (string Property, bool Hideable)[] columns) => builder =>
    {
        for (var index = 0; index < columns.Length; index++)
        {
            builder.OpenComponent<OmniDataGridColumn<Row>>(index * 10);
            builder.AddComponentParameter(index * 10 + 1, nameof(OmniDataGridColumn<Row>.Property), columns[index].Property);
            builder.AddComponentParameter(index * 10 + 2, nameof(OmniDataGridColumn<Row>.Hideable), columns[index].Hideable);
            builder.CloseComponent();
        }
    };

    private IRenderedComponent<OmniDataGrid<Row>> RenderGrid(bool hide, bool amountHideable = true) =>
        Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.KeyOf, row => row.Name)
            .Add(component => component.StateKey, "orders")
            .Add(component => component.AllowColumnHide, hide)
            .Add(component => component.Columns, Columns(("Name", true), ("Amount", amountHideable), ("City", true))));

    private static string[] HeaderKeys(IRenderedComponent<OmniDataGrid<Row>> grid) =>
        [.. grid.FindAll("thead tr:first-child th[data-omni-col]").Select(cell => cell.GetAttribute("data-omni-col")!)];

    [Fact]
    public async Task SavedHiddenColumns_AreShown_WhileTheGridHasNoColumnHide_AndHiddenAgainOnceItIsAllowed()
    {
        var preferences = new GridPreferenceHarness(Services);
        preferences.Seed("orders", """{"version":2,"columnVisibility":{"Amount":false,"City":false}}""");

        var grid = RenderGrid(hide: false);

        // Without the chooser nothing could bring them back: the declaration wins, the choice is kept.
        Assert.Equal(["Name", "Amount", "City"], HeaderKeys(grid));
        Assert.False(await grid.InvokeAsync(() => grid.Instance.Arrangement.SetVisibleAsync("Name", false)));
        Assert.False(grid.Instance.Arrangement.Visibility["Amount"]);

        grid.Render(parameters => parameters.Add(component => component.AllowColumnHide, true));

        Assert.Equal(["Name"], HeaderKeys(grid));
    }

    [Fact]
    public async Task ASavedFalse_ForANonHideableColumn_IsIgnored()
    {
        var preferences = new GridPreferenceHarness(Services);
        preferences.Seed("orders", """{"version":2,"columnVisibility":{"Amount":false,"City":false}}""");

        var grid = RenderGrid(hide: true, amountHideable: false);

        Assert.Equal(["Name", "Amount"], HeaderKeys(grid));
        Assert.False(await grid.InvokeAsync(() => grid.Instance.Arrangement.SetVisibleAsync("Amount", false)));
        Assert.Equal(["Name", "Amount"], HeaderKeys(grid));
    }

    [Fact]
    public void ASavedStateHidingEveryColumn_KeepsTheFirstOne_AsTheChooserWould()
    {
        var preferences = new GridPreferenceHarness(Services);
        preferences.Seed("orders", """{"version":2,"columnOrder":["City","Name","Amount"],"columnVisibility":{"Name":false,"Amount":false,"City":false}}""");

        var grid = RenderGrid(hide: true);

        Assert.Equal(["City"], HeaderKeys(grid));
        Assert.Equal(3, grid.FindAll("tbody tr[data-omni-row-index]").Count);
        // Its box tells it shows, and cannot be unticked: it is the last column.
        var city = grid.Instance.ColumnSet.Find("City")!;
        Assert.True(grid.Instance.Chooser.IsVisible(city));
        Assert.False(grid.Instance.Chooser.CanToggle(city));
    }

    [Fact]
    public async Task AStateFromAnotherTab_HidingEveryColumn_KeepsOne()
    {
        var preferences = new GridPreferenceHarness(Services);
        var grid = RenderGrid(hide: true);

        preferences.Seed("orders", """{"version":2,"columnVisibility":{"Name":false,"Amount":false,"City":false}}""");
        await preferences.Browser.Bridge!.OnStored(preferences.StorageKey("orders"));

        grid.WaitForAssertion(() => Assert.Equal(["Name"], HeaderKeys(grid)));
    }

    [Fact]
    public void ARefusedBrowserStorage_LeavesNothingToImport_AndTheGridRenders()
    {
        _ = new GridPreferenceHarness(Services);
        JSInterop.Setup<string?>("localStorage.getItem", "orders").SetException(new JSException("SecurityError: access denied"));

        var grid = RenderGrid(hide: false);

        Assert.Equal(3, grid.FindAll("tbody tr[data-omni-row-index]").Count);
        Assert.Single(JSInterop.Invocations["localStorage.getItem"]);
    }

    [Fact]
    public void TheFormerBrowserEntry_IsLeftAlone_WhenTheHostHasAStore()
    {
        // The bare key is shared by every user of the browser: with a store it may be another user's grid.
        var preferences = new GridPreferenceHarness(Services, withStore: true);
        JSInterop.Setup<string?>("localStorage.getItem", "orders").SetResult(OldFormat);

        var grid = RenderGrid(hide: false);

        Assert.Equal(3, grid.FindAll("tbody tr[data-omni-row-index]").Count);
        Assert.Empty(JSInterop.Invocations["localStorage.getItem"]);
        Assert.Empty(JSInterop.Invocations["localStorage.removeItem"]);
        Assert.Null(preferences.Store!["omni.grid.orders"]);
    }

    [Fact]
    public void TheFormerBrowserEntry_IsImported_EvenWhenItsRemovalIsRefused()
    {
        var preferences = new GridPreferenceHarness(Services);
        JSInterop.Setup<string?>("localStorage.getItem", "orders").SetResult(OldFormat);
        JSInterop.SetupVoid("localStorage.removeItem", "orders").SetException(new JSException("SecurityError: access denied"));

        var grid = RenderGrid(hide: false);

        Assert.Single(grid.FindAll("tbody tr[data-omni-row-index]"));
        Assert.Equal("b", preferences.Saved("orders")!.Value.GetProperty("filters").GetProperty("Name").GetProperty("value").GetString());
    }
}
