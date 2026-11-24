using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A grid without a <c>StateKey</c> keeps its state once the host turns on
/// <see cref="OmniPreferencesOptions.PersistGridState"/>, or once the grid asks for it (<c>PersistState</c>
/// true), under a key derived from the route and its id or its columns; a grid opts out with
/// <c>PersistState</c> false, and without either switch nothing is read or written.
/// </summary>
public sealed class DataGridDerivedStateKeyTests : OmniBunitContext
{
    public sealed record Row(string Name, int Amount, string City);

    private static readonly IReadOnlyList<Row> Rows = [new("a", 3, "Lyon"), new("b", 1, "Gand"), new("c", 2, "Bari")];

    private static readonly string[] DeclaredKeys = ["Name", "Amount", "City"];

    private static readonly RenderFragment Columns = DataGridTestSetup.PropertyColumns<Row>(("Name", []), ("Amount", []), ("City", []));

    private IRenderedComponent<OmniDataGrid<Row>> RenderGrid(string? id = null, string? stateKey = null, bool? persist = null) =>
        Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.KeyOf, row => row.Name)
            .Add(component => component.Id, id)
            .Add(component => component.StateKey, stateKey)
            .Add(component => component.PersistState, persist)
            .Add(component => component.Columns, Columns));

    private void NavigateTo(string uri) => Services.GetRequiredService<NavigationManager>().NavigateTo(uri);

    private static void Sort(IRenderedComponent<OmniDataGrid<Row>> grid) =>
        grid.Find("thead th[data-omni-col='Amount'] .omni-data-grid__sort").Click();

    private static string[] Names(IRenderedComponent<OmniDataGrid<Row>> grid) =>
        [.. grid.FindAll("tbody tr[data-omni-row-index] td[data-omni-col='Name']").Select(cell => cell.TextContent.Trim())];

    [Theory]
    [InlineData("projects/12", "/projects/*")]
    [InlineData("projects/12/tasks/7?tab=2#top", "/projects/*/tasks/*")]
    [InlineData("Projects/3f2504e0-4f89-11d3-9a0c-0305e82c3301/", "/projects/*")]
    [InlineData("", "/")]
    [InlineData("orders/v2", "/orders/v2")]
    public void TheRoute_DropsQueryAndFragment_AndReplacesIntegerAndGuidSegments(string relative, string expected) =>
        Assert.Equal(expected, GridStateKey.RoutePattern(relative));

    [Fact]
    public void TheKey_TakesTheId_ElseASignatureOfTheColumnKeys()
    {
        Assert.Equal("route:/projects/*#id:main", GridStateKey.Compose("/projects/*", "main", () => throw new InvalidOperationException("not read")));

        var byColumns = GridStateKey.Compose("/projects/*", null, () => DeclaredKeys);
        Assert.Equal($"route:/projects/*#cols:{GridStateKey.Signature(DeclaredKeys)}", byColumns);
        Assert.Equal(byColumns, GridStateKey.Compose("/projects/*", null, () => ["Name", "Amount", "City"]));
        Assert.NotEqual(byColumns, GridStateKey.Compose("/projects/*", null, () => ["Amount", "Name", "City"]));
        Assert.Matches("^[0-9a-f]{16}$", GridStateKey.Signature(DeclaredKeys));
    }

    [Fact]
    public void AKeyTooLongForTheStore_IsHashed_WithinItsColumn()
    {
        var route = "/" + string.Join('/', Enumerable.Repeat("segment", 40));

        var key = GridStateKey.Compose(route, "main", () => []);

        Assert.StartsWith("route:~", key, StringComparison.Ordinal);
        Assert.True(GridStateKey.PreferencePrefix.Length + key.Length <= GridStateKey.MaxPreferenceKeyLength);
        Assert.Equal(key, GridStateKey.Compose(route, "main", () => []));
    }

    [Fact]
    public async Task WithTheHostOption_AGridWithAnId_KeepsItsStateUnderTheRouteAndItsId()
    {
        var preferences = new GridPreferenceHarness(Services, persistGridState: true);
        NavigateTo("/projects/12?tab=1");
        var grid = RenderGrid(id: "main");

        Sort(grid);

        Assert.NotNull(preferences.Saved("route:/projects/*#id:main"));

        // Another project of the same page takes the same state.
        await DisposeComponentsAsync();
        NavigateTo("/projects/34");
        Assert.Equal(["b", "c", "a"], Names(RenderGrid(id: "main")));
    }

    [Fact]
    public void WithTheHostOption_AGridWithoutAnId_IsKeyedByItsColumns_AndItsStateIsReadBack()
    {
        var preferences = new GridPreferenceHarness(Services, persistGridState: true);
        NavigateTo("/orders/5");
        var key = $"route:/orders/*#cols:{GridStateKey.Signature(DeclaredKeys)}";
        preferences.Seed(key, """{"version":3,"sorts":[{"key":"Amount","descending":false}]}""");

        var grid = RenderGrid();

        Assert.Equal(["b", "c", "a"], Names(grid));
        Sort(grid);
        Assert.Equal(["a", "c", "b"], Names(grid));
        Assert.True(preferences.Saved(key)!.Value.GetProperty("sorts")[0].GetProperty("descending").GetBoolean());
    }

    [Fact]
    public void WithoutTheHostOption_AGridWithoutStateKey_ReadsAndWritesNothing()
    {
        var preferences = new GridPreferenceHarness(Services);
        var calls = new List<string>();
        preferences.Browser.OnInvoke = calls.Add;
        NavigateTo("/orders/5");

        var grid = RenderGrid(id: "main");
        Sort(grid);

        Assert.Empty(calls);
        Assert.Equal(0, preferences.Browser.Writes);
        Assert.Empty(preferences.Browser.Storage);
    }

    [Fact]
    public void PersistStateFalse_OptsAGridOut_OfTheHostOption_AndOfItsStateKey()
    {
        var preferences = new GridPreferenceHarness(Services, persistGridState: true);
        var calls = new List<string>();
        preferences.Browser.OnInvoke = calls.Add;
        preferences.Seed("orders", """{"version":3,"sorts":[{"key":"Amount","descending":true}]}""");

        var derived = RenderGrid(id: "main", persist: false);
        var keyed = RenderGrid(stateKey: "orders", persist: false);
        Sort(derived);
        Sort(keyed);

        Assert.Empty(calls);
        Assert.Equal(0, preferences.Browser.Writes);
        Assert.Equal(["b", "c", "a"], Names(keyed));
    }

    [Fact]
    public void PersistStateTrue_KeepsTheStateOfOneGrid_WithoutTheHostOption()
    {
        var preferences = new GridPreferenceHarness(Services);
        NavigateTo("/orders");
        var grid = RenderGrid(persist: true);

        Sort(grid);

        Assert.NotNull(preferences.Saved($"route:/orders#cols:{GridStateKey.Signature(DeclaredKeys)}"));
    }

    [Fact]
    public void AStateKey_StillWins_OverTheDerivedKey()
    {
        var preferences = new GridPreferenceHarness(Services, persistGridState: true);
        NavigateTo("/orders");
        var grid = RenderGrid(id: "main", stateKey: "orders");

        Sort(grid);

        Assert.NotNull(preferences.Saved("orders"));
        Assert.Null(preferences.Saved("route:/orders#id:main"));
    }
}
