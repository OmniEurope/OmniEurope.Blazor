using System.Globalization;
using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// PLAN-019 lot 4: the column chooser of the header bar (<see cref="OmniDataGrid{TItem}.AllowColumnHide"/>,
/// <see cref="OmniDataGrid{TItem}.AllowColumnReorder"/>), the header drag that lands on the same moves, the
/// layout reset (<see cref="OmniDataGrid{TItem}.ShowResetLayout"/>, <see cref="OmniDataGrid{TItem}.ResetLayoutAsync"/>)
/// and the polite announcement of each change; everything kept through <see cref="OmniPreferences"/>.
/// </summary>
public sealed class DataGridColumnChooserTests : OmniBunitContext
{
    public sealed record Row(string Name, int Amount, string City, string Country);

    private static readonly IReadOnlyList<Row> Rows = [new("a", 1, "Lyon", "FR"), new("b", 2, "Gand", "BE"), new("c", 3, "Bari", "IT")];

    private sealed record ColumnSpec(string Property, bool Frozen = false, bool Hideable = true, string? Title = null);

    private static readonly ColumnSpec[] Declared = [new("Name"), new("Amount"), new("City")];

    private static RenderFragment Columns(params ColumnSpec[] columns) => builder =>
    {
        for (var index = 0; index < columns.Length; index++)
        {
            builder.OpenComponent<OmniDataGridColumn<Row>>(index * 10);
            builder.AddComponentParameter(index * 10 + 1, nameof(OmniDataGridColumn<Row>.Property), columns[index].Property);
            builder.AddComponentParameter(index * 10 + 2, nameof(OmniDataGridColumn<Row>.Frozen), columns[index].Frozen);
            builder.AddComponentParameter(index * 10 + 3, nameof(OmniDataGridColumn<Row>.Hideable), columns[index].Hideable);
            if (columns[index].Title is { } title)
            {
                builder.AddComponentParameter(index * 10 + 4, nameof(OmniDataGridColumn<Row>.Title), title);
            }

            builder.CloseComponent();
        }
    };

    private IRenderedComponent<OmniDataGrid<Row>> RenderGrid(
        bool hide = true,
        bool reorder = true,
        bool reset = false,
        ColumnSpec[]? columns = null,
        string? stateKey = "orders",
        Action<OmniDataGridColumnOrderChange>? reordered = null) =>
        Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.KeyOf, row => row.Name)
            .Add(component => component.StateKey, stateKey)
            .Add(component => component.AllowColumnHide, hide)
            .Add(component => component.AllowColumnReorder, reorder)
            .Add(component => component.ShowResetLayout, reset)
            .Add(component => component.OnColumnReorder, change => reordered?.Invoke(change))
            .Add(component => component.Columns, Columns(columns ?? Declared)));

    private static string[] HeaderKeys(IRenderedComponent<OmniDataGrid<Row>> grid) =>
        [.. grid.FindAll("thead tr:first-child th[data-omni-col]").Select(cell => cell.GetAttribute("data-omni-col")!)];

    private static void Open(IRenderedComponent<OmniDataGrid<Row>> grid) => grid.Find(".omni-data-grid__chooser-toggle").Click();

    private static IElement Box(IRenderedComponent<OmniDataGrid<Row>> grid, string name) =>
        grid.FindAll(".omni-data-grid__chooser-item").Single(item => item.TextContent.Trim() == name).QuerySelector("input[type=checkbox]")!;

    private static IElement Arrow(IRenderedComponent<OmniDataGrid<Row>> grid, string key, int step) =>
        grid.Find($".omni-data-grid__chooser-list button[data-omni-col='{key}'][data-omni-move='{step}']");

    private static string[] ListedNames(IRenderedComponent<OmniDataGrid<Row>> grid) =>
        [.. grid.FindAll(".omni-data-grid__chooser-item").Select(item => item.TextContent.Trim())];

    private static string Announcement(IRenderedComponent<OmniDataGrid<Row>> grid) =>
        grid.Find(".omni-data-grid > [role=status][aria-live=polite]").TextContent.TrimEnd(' ');

    [Fact]
    public void Defaults_RenderNoChooser_NoHeaderBar_NoDragHook()
    {
        var preferences = new GridPreferenceHarness(Services);
        var grid = RenderGrid(hide: false, reorder: false);

        Assert.Empty(grid.FindAll(".omni-data-grid__chooser"));
        Assert.Empty(grid.FindAll(".omni-data-grid__bar--header"));
        Assert.Empty(grid.FindAll("[data-omni-reorder]"));
        Assert.Empty(grid.FindAll(".omni-data-grid > [role=status]"));
        Assert.DoesNotContain(JSInterop.Invocations, invocation => invocation.Identifier == "attachReorder");
        Assert.Equal(0, preferences.Browser.Writes);
    }

    [Fact]
    public void Chooser_IsAnIconButtonOfTheHeaderBar_AndEveryControlIsNamed()
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr");
        try
        {
            _ = new GridPreferenceHarness(Services);
            var grid = RenderGrid(reset: true);

            var toggle = grid.Find(".omni-data-grid__bar--header .omni-data-grid__chooser-toggle");
            Assert.Equal("Choisir les colonnes", toggle.GetAttribute("aria-label"));
            Assert.Equal("Choisir les colonnes", toggle.GetAttribute("title"));
            Assert.Equal("button", toggle.GetAttribute("type"));
            Assert.NotNull(toggle.QuerySelector("svg"));
            Assert.Equal(string.Empty, toggle.TextContent.Trim());

            Open(grid);
            Assert.Equal("Choisir les colonnes", grid.Find(".omni-data-grid__chooser-list").GetAttribute("aria-label"));
            Assert.Equal(["Name", "Amount", "City"], ListedNames(grid));
            Assert.Equal("Déplacer Amount à gauche", Arrow(grid, "Amount", -1).GetAttribute("aria-label"));
            Assert.Equal("Déplacer Amount à droite", Arrow(grid, "Amount", 1).GetAttribute("title"));
            // Each box is named by the column it shows: the label wraps it.
            Assert.Equal("Amount", Box(grid, "Amount").ParentElement!.TextContent.Trim());
            Assert.Contains("Réinitialiser la disposition", grid.Find(".omni-data-grid__chooser-reset").TextContent, StringComparison.Ordinal);
            Assert.Equal("polite", grid.Find(".omni-data-grid > [role=status]").GetAttribute("aria-live"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void ABoxHidesThenShowsAColumn_AnnouncesIt_AndKeepsItInThePreferences()
    {
        var preferences = new GridPreferenceHarness(Services, withStore: true);
        var grid = RenderGrid(reorder: false);
        Open(grid);

        Box(grid, "Amount").Change(false);

        Assert.Equal(["Name", "City"], HeaderKeys(grid));
        Assert.Equal(grid.Instance.Text("GridColumnHidden", "Amount"), Announcement(grid));
        var state = JsonDocument.Parse(preferences.Store!["omni.grid.orders"]!.Value!).RootElement;
        Assert.False(state.GetProperty("columnVisibility").GetProperty("Amount").GetBoolean());
        // Still listed, unticked, so it can come back.
        Assert.False(Box(grid, "Amount").HasAttribute("checked"));

        Box(grid, "Amount").Change(true);

        Assert.Equal(["Name", "Amount", "City"], HeaderKeys(grid));
        Assert.Equal(grid.Instance.Text("GridColumnShown", "Amount"), Announcement(grid));
        state = JsonDocument.Parse(preferences.Store["omni.grid.orders"]!.Value!).RootElement;
        Assert.True(state.GetProperty("columnVisibility").GetProperty("Amount").GetBoolean());
    }

    [Fact]
    public async Task TheLastVisibleColumn_HasItsBoxDisabled_AndCannotBeHidden()
    {
        _ = new GridPreferenceHarness(Services);
        var grid = RenderGrid(reorder: false);
        Open(grid);
        Box(grid, "Amount").Change(false);
        Box(grid, "City").Change(false);

        var last = Box(grid, "Name");
        Assert.True(last.HasAttribute("disabled"));
        Assert.False(Box(grid, "Amount").HasAttribute("disabled"));

        await grid.InvokeAsync(() => grid.Instance.Chooser.ToggleAsync(grid.Instance.ColumnSet.Find("Name")!, false));
        Assert.Equal(["Name"], HeaderKeys(grid));
    }

    [Fact]
    public async Task AColumnThatIsNotHideable_IsLeftOutOfAHideOnlyChooser_AndHasADisabledBoxWhenColumnsAlsoMove()
    {
        _ = new GridPreferenceHarness(Services);
        ColumnSpec[] columns = [new("Name", Hideable: false), new("Amount"), new("City")];

        var hideOnly = RenderGrid(reorder: false, columns: columns);
        Open(hideOnly);
        Assert.Equal(["Amount", "City"], ListedNames(hideOnly));
        Assert.Empty(hideOnly.FindAll(".omni-data-grid__chooser-move"));

        var both = RenderGrid(columns: columns, stateKey: "other");
        Open(both);
        Assert.Equal(["Name", "Amount", "City"], ListedNames(both));
        Assert.True(Box(both, "Name").HasAttribute("disabled"));
        await both.InvokeAsync(() => both.Instance.Chooser.ToggleAsync(both.Instance.ColumnSet.Find("Name")!, false));
        Assert.Equal(["Name", "Amount", "City"], HeaderKeys(both));
    }

    [Fact]
    public void AHideableChangedByTheHost_ReachesTheChooser()
    {
        _ = new GridPreferenceHarness(Services);
        var grid = RenderGrid(reorder: false);
        Open(grid);
        Assert.Equal(["Name", "Amount", "City"], ListedNames(grid));

        grid.Render(parameters => parameters.Add(component => component.Columns, Columns(new("Name"), new("Amount", Hideable: false), new("City"))));

        Assert.Equal(["Name", "City"], ListedNames(grid));
    }

    [Fact]
    public async Task ReorderOnly_ListsTheNamesWithTheirArrows_WithoutBoxes()
    {
        _ = new GridPreferenceHarness(Services);
        var grid = RenderGrid(hide: false);
        Open(grid);

        Assert.Empty(grid.FindAll(".omni-data-grid__chooser-list input[type=checkbox]"));
        Assert.Equal(["Name", "Amount", "City"], grid.FindAll(".omni-data-grid__chooser-name").Select(name => name.TextContent));
        Assert.Equal(6, grid.FindAll(".omni-data-grid__chooser-move").Count);

        // Without AllowColumnHide, the chooser hides nothing even when asked.
        await grid.InvokeAsync(() => grid.Instance.Chooser.ToggleAsync(grid.Instance.ColumnSet.Find("Amount")!, false));
        Assert.Equal(["Name", "Amount", "City"], HeaderKeys(grid));
    }

    [Fact]
    public void TheArrows_MoveAColumnLeftAndRight_FromTheKeyboard_SaveTheOrder_AnnounceIt_AndKeepTheFocusOnIt()
    {
        var preferences = new GridPreferenceHarness(Services, withStore: true);
        var reorders = new List<OmniDataGridColumnOrderChange>();
        var grid = RenderGrid(reordered: reorders.Add);
        Open(grid);
        Assert.True(Arrow(grid, "Name", -1).HasAttribute("disabled"));
        Assert.True(Arrow(grid, "City", 1).HasAttribute("disabled"));

        Arrow(grid, "City", -1).Click();

        Assert.Equal(["Name", "City", "Amount"], HeaderKeys(grid));
        Assert.Equal(["Name", "City", "Amount"], ListedNames(grid));
        Assert.Equal(grid.Instance.Text("GridColumnMoved", "City", 2, 3), Announcement(grid));
        var state = JsonDocument.Parse(preferences.Store!["omni.grid.orders"]!.Value!).RootElement;
        Assert.Equal(["Name", "City", "Amount"], state.GetProperty("columnOrder").EnumerateArray().Select(key => key.GetString()));
        var change = Assert.Single(reorders);
        Assert.Equal("City", change.Key);
        Assert.Equal(["Name", "City", "Amount"], change.Order);
        var focus = Assert.Single(JSInterop.Invocations, invocation => invocation.Identifier == "focusMoveControl");
        Assert.Equal("City", focus.Arguments[1]);
        Assert.Equal(-1, focus.Arguments[2]);

        Arrow(grid, "Name", 1).Click();

        Assert.Equal(["City", "Name", "Amount"], HeaderKeys(grid));
        Assert.Equal(2, reorders.Count);
    }

    [Fact]
    public async Task FrozenColumns_OnlyMoveAmongThemselves_FromTheArrowsAndFromADrop()
    {
        _ = new GridPreferenceHarness(Services);
        var reorders = new List<OmniDataGridColumnOrderChange>();
        var grid = RenderGrid(columns: [new("Name", Frozen: true), new("Country", Frozen: true), new("Amount"), new("City")], reordered: reorders.Add);
        Assert.Equal("frozen", grid.Find("th[data-omni-col='Name']").GetAttribute("data-omni-reorder"));
        Assert.Equal("free", grid.Find("th[data-omni-col='Amount']").GetAttribute("data-omni-reorder"));
        Open(grid);

        // The last frozen column cannot step into the free ones, nor the first free one into the frozen.
        Assert.True(Arrow(grid, "Country", 1).HasAttribute("disabled"));
        Assert.True(Arrow(grid, "Amount", -1).HasAttribute("disabled"));
        Assert.False(Arrow(grid, "Country", -1).HasAttribute("disabled"));

        await grid.InvokeAsync(() => grid.Instance.OnColumnDroppedAsync("Amount", "Name", after: false));
        Assert.Equal(["Name", "Country", "Amount", "City"], HeaderKeys(grid));
        Assert.Empty(reorders);

        await grid.InvokeAsync(() => grid.Instance.OnColumnDroppedAsync("City", "Amount", after: false));
        Assert.Equal(["Name", "Country", "City", "Amount"], HeaderKeys(grid));
        await grid.InvokeAsync(() => grid.Instance.OnColumnDroppedAsync("Name", "Country", after: true));
        Assert.Equal(["Country", "Name", "City", "Amount"], HeaderKeys(grid));
        Assert.Equal(2, reorders.Count);
        Assert.Equal(grid.Instance.Text("GridColumnMoved", "Name", 2, 4), Announcement(grid));
    }

    [Fact]
    public async Task ADrop_IsIgnored_WhenReorderingIsOff_OrForAColumnTheGridDoesNotHave()
    {
        var preferences = new GridPreferenceHarness(Services);
        var off = RenderGrid(reorder: false);

        await off.InvokeAsync(() => off.Instance.OnColumnDroppedAsync("City", "Name", after: false));
        await off.InvokeAsync(() => off.Instance.OnColumnDroppedAsync("Gone", "Name", after: false));
        Assert.Equal(["Name", "Amount", "City"], HeaderKeys(off));

        var on = RenderGrid(stateKey: "other");
        await on.InvokeAsync(() => on.Instance.OnColumnDroppedAsync("City", "Gone", after: false));
        await on.InvokeAsync(() => on.Instance.OnColumnDroppedAsync("City", "City", after: true));
        Assert.Equal(["Name", "Amount", "City"], HeaderKeys(on));
        Assert.Equal(0, preferences.Browser.Writes);
        Assert.Single(JSInterop.Invocations, invocation => invocation.Identifier == "attachReorder");
    }

    [Fact]
    public void AHiddenColumnMovedInTheChooser_IsAnnouncedAtItsPlaceInTheList()
    {
        _ = new GridPreferenceHarness(Services);
        var grid = RenderGrid();
        Open(grid);
        Box(grid, "City").Change(false);

        Arrow(grid, "City", -1).Click();

        Assert.Equal(["Name", "City", "Amount"], ListedNames(grid));
        Assert.Equal(["Name", "Amount"], HeaderKeys(grid));
        Assert.Equal(grid.Instance.Text("GridColumnMoved", "City", 2, 3), Announcement(grid));
    }

    [Fact]
    public void TheResetAction_IsHiddenByDefault_AndRestoresTheDeclaredLayout_KeepingTheSort()
    {
        var preferences = new GridPreferenceHarness(Services, withStore: true);
        var hidden = RenderGrid(stateKey: "other");
        Open(hidden);
        Assert.Empty(hidden.FindAll(".omni-data-grid__chooser-reset"));

        var grid = RenderGrid(reset: true);
        grid.Find("th[data-omni-col='Amount']").Click();
        Open(grid);
        Box(grid, "City").Change(false);
        Arrow(grid, "Amount", -1).Click();
        Assert.Equal(["Amount", "Name"], HeaderKeys(grid));

        grid.Find(".omni-data-grid__chooser-reset").Click();

        Assert.Equal(["Name", "Amount", "City"], HeaderKeys(grid));
        Assert.Equal("ascending", grid.Find("th[data-omni-col='Amount']").GetAttribute("aria-sort"));
        Assert.Equal(grid.Instance.Text("GridLayoutReset"), Announcement(grid));
        var state = JsonDocument.Parse(preferences.Store!["omni.grid.orders"]!.Value!).RootElement;
        Assert.Equal(JsonValueKind.Null, state.GetProperty("columnOrder").ValueKind);
        Assert.Equal("Amount", state.GetProperty("sorts")[0].GetProperty("key").GetString());
    }

    [Fact]
    public async Task ResetLayout_OnAGridBeingRemoved_DoesNothing()
    {
        var preferences = new GridPreferenceHarness(Services);
        var grid = RenderGrid();
        await grid.InvokeAsync(() => grid.Instance.Arrangement.SetVisibleAsync("City", false));
        var writes = preferences.Browser.Writes;
        var instance = grid.Instance;
        await grid.InvokeAsync(async () => await instance.DisposeAsync());

        await grid.InvokeAsync(() => instance.ResetLayoutAsync());
        await grid.InvokeAsync(() => instance.OnColumnDroppedAsync("Amount", "Name", after: false));

        Assert.Equal(["Name", "Amount"], instance.ColumnSet.VisibleColumns.Select(column => column.Key));
        Assert.Equal(writes, preferences.Browser.Writes);
    }

    [Fact]
    public void AColumnIsNamedByItsTitle_OrByItsKeyWithoutOne()
    {
        _ = new GridPreferenceHarness(Services);
        var grid = RenderGrid(columns: [new("Name", Title: "Nom"), new("Amount", Title: " ")]);
        Open(grid);

        Assert.Equal(["Nom", "Amount"], ListedNames(grid));
        Assert.Equal(grid.Instance.Text("GridColumnMoveRight", "Nom"), Arrow(grid, "Name", 1).GetAttribute("aria-label"));
    }

    [Fact]
    public async Task TheChooser_IgnoresAMoveItDoesNotOffer()
    {
        var preferences = new GridPreferenceHarness(Services);
        var hideOnly = RenderGrid(reorder: false);
        var amount = hideOnly.Instance.ColumnSet.Find("Amount")!;
        await hideOnly.InvokeAsync(() => hideOnly.Instance.Chooser.MoveAsync(amount, -1));
        Assert.Equal(["Name", "Amount", "City"], HeaderKeys(hideOnly));

        var grid = RenderGrid(stateKey: "other");
        var city = grid.Instance.ColumnSet.Find("City")!;
        var gone = new OmniDataGridColumnDefinition<Row> { Key = "Gone", Title = "Gone", Value = _ => null };
        // Past the end of the list, and a column that left the grid since its arrow was drawn.
        await grid.InvokeAsync(() => grid.Instance.Chooser.MoveAsync(city, 1));
        await grid.InvokeAsync(() => grid.Instance.Chooser.MoveAsync(gone, 1));
        Assert.Equal(["Name", "Amount", "City"], HeaderKeys(grid));
        Assert.Equal(0, preferences.Browser.Writes);
        Assert.DoesNotContain(JSInterop.Invocations, invocation => invocation.Identifier == "focusMoveControl");
    }
}
