using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// PLAN-019 lot 3: a grid's <see cref="OmniDataGrid{TItem}.StateKey"/> state is a user preference
/// (<see cref="OmniPreferences"/>, key <c>omni.grid.&lt;StateKey&gt;</c>) that now holds the column order
/// and visibility too; a state of the former shape reads without loss, the former browser entry is taken
/// over once, a state changed elsewhere is applied to the grid on screen, and a layout reset goes back to
/// the declared layout while keeping the filters and sorts.
/// </summary>
public sealed class DataGridPreferenceStateTests : OmniBunitContext
{
    public sealed record Row(string Name, int Amount, string City);

    private static readonly IReadOnlyList<Row> Rows = [new("a", 1, "Lyon"), new("b", 2, "Gand"), new("c", 3, "Bari")];

    private static readonly string OldFormat =
        $$$"""{"Filters":{"Name":{"Operator":{{{(int)OmniDataGridFilterOperator.Contains}}},"Value":"b","LogicalOperator":0,"SecondOperator":0,"SecondValue":""}},"Sorts":[{"Key":"Amount","Descending":true}],"ColumnWidths":{"Name":"150px"}}""";

    private sealed record ColumnSpec(string Property, bool Frozen = false, bool Visible = true, OmniDataGridSortOrder? SortOrder = null);

    private static RenderFragment Columns(params ColumnSpec[] columns) => builder =>
    {
        for (var index = 0; index < columns.Length; index++)
        {
            builder.OpenComponent<OmniDataGridColumn<Row>>(index * 10);
            builder.AddComponentParameter(index * 10 + 1, nameof(OmniDataGridColumn<Row>.Property), columns[index].Property);
            builder.AddComponentParameter(index * 10 + 2, nameof(OmniDataGridColumn<Row>.Frozen), columns[index].Frozen);
            builder.AddComponentParameter(index * 10 + 3, nameof(OmniDataGridColumn<Row>.Visible), columns[index].Visible);
            builder.AddComponentParameter(index * 10 + 4, nameof(OmniDataGridColumn<Row>.SortOrder), columns[index].SortOrder);
            builder.CloseComponent();
        }
    };

    private static readonly ColumnSpec[] Declared = [new("Name"), new("Amount"), new("City")];

    private IRenderedComponent<OmniDataGrid<Row>> RenderGrid(
        ColumnSpec[]? columns = null,
        Func<OmniDataGridLoadRequest, Task<OmniDataGridResult<Row>>>? load = null,
        string? stateKey = "orders") =>
        Render<OmniDataGrid<Row>>(parameters =>
        {
            parameters
                .Add(component => component.KeyOf, row => row.Name)
                .Add(component => component.StateKey, stateKey)
                // The saved visibility only counts on a grid whose reader may hide columns.
                .Add(component => component.AllowColumnHide, true)
                .Add(component => component.Columns, Columns(columns ?? Declared));
            if (load is null)
            {
                parameters.Add(component => component.Items, Rows);
            }
            else
            {
                parameters.Add(component => component.Load, load);
            }
        });

    private static string[] HeaderKeys(IRenderedComponent<OmniDataGrid<Row>> grid) =>
        [.. grid.FindAll("thead tr:first-child th[data-omni-col]").Select(cell => cell.GetAttribute("data-omni-col")!)];

    private static string[] FirstRowKeys(IRenderedComponent<OmniDataGrid<Row>> grid) =>
        [.. grid.FindAll("tbody tr[data-omni-row-index]")[0].QuerySelectorAll("td[data-omni-col]").Select(cell => cell.GetAttribute("data-omni-col")!)];

    [Fact]
    public async Task Changes_AreSavedThroughThePreferences_WithTheOrderAndTheVisibility()
    {
        var preferences = new GridPreferenceHarness(Services, withStore: true);
        var grid = RenderGrid();

        grid.Find("th[data-omni-col='Amount']").Click();
        Assert.True(await grid.InvokeAsync(() => grid.Instance.Arrangement.MoveAsync("City", 0)));
        Assert.True(await grid.InvokeAsync(() => grid.Instance.Arrangement.SetVisibleAsync("Amount", false)));

        var saved = preferences.Store!["omni.grid.orders"]!;
        var state = JsonDocument.Parse(saved.Value!).RootElement;
        Assert.Equal(2, state.GetProperty("version").GetInt32());
        Assert.Equal(["City", "Name", "Amount"], state.GetProperty("columnOrder").EnumerateArray().Select(key => key.GetString()));
        Assert.False(state.GetProperty("columnVisibility").GetProperty("Amount").GetBoolean());
        Assert.Equal("Amount", state.GetProperty("sorts")[0].GetProperty("key").GetString());
        Assert.Equal(["City", "Name"], HeaderKeys(grid));
    }

    [Fact]
    public void SavedOrderAndVisibility_AreRestored_AndAHiddenColumnIsAbsentFromHeaderCellsAndExport()
    {
        var preferences = new GridPreferenceHarness(Services);
        preferences.Seed("orders", """{"version":2,"columnOrder":["City","Name","Amount"],"columnVisibility":{"Amount":false,"City":true}}""");

        var grid = RenderGrid([new("Name"), new("Amount"), new("City", Visible: false)]);

        // City is declared hidden but the reader showed it; Amount is declared visible but the reader hid it.
        Assert.Equal(["City", "Name"], HeaderKeys(grid));
        Assert.Equal(["City", "Name"], FirstRowKeys(grid));
        Assert.Empty(grid.FindAll("[data-omni-col='Amount']"));
        Assert.Equal(["City", "Name"], grid.Instance.Export.Columns().Select(column => column.Key));
    }

    [Fact]
    public void FormerState_WithoutOrderNorVisibility_IsReadWithoutLoss()
    {
        var preferences = new GridPreferenceHarness(Services);
        preferences.Seed("orders", OldFormat);

        var grid = RenderGrid();

        Assert.Equal(["Name", "Amount", "City"], HeaderKeys(grid));
        var row = Assert.Single(grid.FindAll("tbody tr[data-omni-row-index]"));
        Assert.Contains("Gand", row.TextContent, StringComparison.Ordinal);
        Assert.Equal("descending", grid.Find("th[data-omni-col='Amount']").GetAttribute("aria-sort"));
        Assert.Equal("150px", grid.Instance.ColumnLayout.Widths["Name"]);
    }

    [Theory]
    // City is new to the saved order: it follows Amount, the column declared before it.
    [InlineData("""["Amount","Gone","Name"]""", "Amount,City,Name")]
    // Name is new and declared first: it opens the row.
    [InlineData("""["City","Amount"]""", "Name,City,Amount")]
    public void SavedOrder_PutsANewColumnAtItsDeclaredPlace_AndIgnoresAColumnThatIsGone(string order, string expected)
    {
        var preferences = new GridPreferenceHarness(Services);
        preferences.Seed("orders", """{"version":2,"columnOrder":""" + order + "}");

        var grid = RenderGrid();

        Assert.Equal(expected.Split(','), HeaderKeys(grid));
    }

    [Fact]
    public async Task FrozenColumns_KeepTheirFrozenPlaces_WhateverTheOrder()
    {
        var preferences = new GridPreferenceHarness(Services);
        preferences.Seed("orders", """{"version":2,"columnOrder":["Amount","City","Name"]}""");
        ColumnSpec[] columns = [new("Name", Frozen: true), new("Amount"), new("City")];

        var grid = RenderGrid(columns);
        Assert.Equal(["Name", "Amount", "City"], HeaderKeys(grid));

        // A free column moved before the frozen one stays after it: nothing changes, nothing is saved.
        var writes = preferences.Browser.Writes;
        Assert.False(await grid.InvokeAsync(() => grid.Instance.Arrangement.MoveAsync("Amount", 0)));
        Assert.Equal(writes, preferences.Browser.Writes);

        Assert.True(await grid.InvokeAsync(() => grid.Instance.Arrangement.MoveAsync("City", 1)));
        Assert.Equal(["Name", "City", "Amount"], HeaderKeys(grid));
        Assert.Equal(["Name", "City", "Amount"], preferences.Saved("orders")!.Value.GetProperty("columnOrder").EnumerateArray().Select(key => key.GetString()));
    }

    [Fact]
    public async Task TheLastVisibleColumn_CannotBeHidden()
    {
        _ = new GridPreferenceHarness(Services);
        var grid = RenderGrid([new("Name"), new("Amount", Visible: false)]);

        Assert.False(await grid.InvokeAsync(() => grid.Instance.Arrangement.SetVisibleAsync("Name", false)));
        Assert.Equal(["Name"], HeaderKeys(grid));
    }

    [Fact]
    public void FormerBrowserEntry_IsTakenOverOnce_ThenRemoved_WithoutAHostStore()
    {
        var preferences = new GridPreferenceHarness(Services);
        JSInterop.Setup<string?>("localStorage.getItem", "orders").SetResult(OldFormat);
        JSInterop.SetupVoid("localStorage.removeItem", "orders").SetVoidResult();

        var grid = RenderGrid();

        Assert.Single(grid.FindAll("tbody tr[data-omni-row-index]"));
        Assert.Single(JSInterop.Invocations["localStorage.removeItem"]);
        // Kept as the preference, in the current shape.
        var stored = preferences.Saved("orders")!.Value;
        Assert.Equal(2, stored.GetProperty("version").GetInt32());
        Assert.Equal("b", stored.GetProperty("filters").GetProperty("Name").GetProperty("value").GetString());
    }

    [Fact]
    public void AnotherValueUnderTheBareKey_IsLeftToTheHost()
    {
        var preferences = new GridPreferenceHarness(Services);
        JSInterop.Setup<string?>("localStorage.getItem", "orders").SetResult("""{"theme":"dark"}""");

        var grid = RenderGrid();

        Assert.Equal(3, grid.FindAll("tbody tr[data-omni-row-index]").Count);
        Assert.Empty(JSInterop.Invocations["localStorage.removeItem"]);
        Assert.Null(preferences.Saved("orders"));
    }

    [Fact]
    public async Task StateFromTheStore_IsAppliedLive_AndReloadsOnlyWhenTheQueryChanged()
    {
        var preferences = new GridPreferenceHarness(Services, withStore: true);
        var requests = new List<OmniDataGridLoadRequest>();
        var grid = RenderGrid(load: request =>
        {
            requests.Add(request);
            var rows = Rows.Where(row => request.Filters.All(filter => row.Name.Contains(filter.Value ?? string.Empty, StringComparison.Ordinal))).ToArray();
            return Task.FromResult(new OmniDataGridResult<Row>(rows, rows.Length));
        });
        grid.WaitForAssertion(() => Assert.Equal(3, grid.FindAll("tbody tr[data-omni-row-index]").Count));
        Assert.Single(requests);

        // Written on another device: order, visibility and widths only, no reload.
        preferences.Store!.Put("omni.grid.orders", """{"version":2,"columnOrder":["City","Amount","Name"],"columnVisibility":{"Amount":false},"columnWidths":{"City":"90px"}}""", DateTimeOffset.UtcNow.AddMinutes(1));
        await preferences.Preferences!.SyncAsync(Xunit.TestContext.Current.CancellationToken);

        grid.WaitForAssertion(() => Assert.Equal(["City", "Name"], HeaderKeys(grid)));
        Assert.Equal("90px", grid.Instance.ColumnLayout.Widths["City"]);
        Assert.Single(requests);

        // Then a filter: the rows are loaded again with it.
        preferences.Store.Put("omni.grid.orders", OldFormat, DateTimeOffset.UtcNow.AddMinutes(2));
        await preferences.Preferences.SyncAsync(Xunit.TestContext.Current.CancellationToken);

        grid.WaitForAssertion(() => Assert.Single(grid.FindAll("tbody tr[data-omni-row-index]")));
        Assert.Equal(2, requests.Count);
        Assert.Equal("b", Assert.Single(requests[1].Filters).Value);
        Assert.Equal(["Name", "Amount", "City"], HeaderKeys(grid));
    }

    [Fact]
    public async Task StateFromTheStore_ThatReloadsASlowGrid_DoesNotHoldTheSynchronization()
    {
        var preferences = new GridPreferenceHarness(Services, withStore: true);
        var slow = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requests = 0;
        var grid = RenderGrid(load: async request =>
        {
            if (Interlocked.Increment(ref requests) > 1)
            {
                await slow.Task;
            }

            var rows = Rows.Where(row => request.Filters.All(filter => row.Name.Contains(filter.Value ?? string.Empty, StringComparison.Ordinal))).ToArray();
            return new OmniDataGridResult<Row>(rows, rows.Length);
        });
        grid.WaitForAssertion(() => Assert.Equal(3, grid.FindAll("tbody tr[data-omni-row-index]").Count));

        try
        {
            // A filter written on another device: the grid loads again, and the synchronization does not wait for it.
            preferences.Store!.Put("omni.grid.orders", OldFormat, DateTimeOffset.UtcNow.AddMinutes(1));
            await preferences.Preferences!.SyncAsync(Xunit.TestContext.Current.CancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
            grid.WaitForAssertion(() => Assert.Equal(2, Volatile.Read(ref requests)));
        }
        finally
        {
            slow.TrySetResult();
        }

        grid.WaitForAssertion(() => Assert.Single(grid.FindAll("tbody tr[data-omni-row-index]")));
        await grid.InvokeAsync(() => grid.Instance.Persistence.WhenAppliedAsync());
    }

    [Theory]
    // A failure while the state from elsewhere is applied is logged, and the next state still applies.
    [InlineData(false)]
    // The grid went away (or the circuit ended) meanwhile: nobody is left to tell, nothing is logged.
    [InlineData(true)]
    public async Task StateFromTheStore_ThatFailsWhileApplied_IsLoggedUnlessTheGridIsGone(bool gone)
    {
        var preferences = new GridPreferenceHarness(Services, withStore: true);
        var logger = new GridLogger();
        Services.AddSingleton<Microsoft.Extensions.Logging.ILogger<OmniDataGrid<Row>>>(logger);
        var requests = 0;
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.KeyOf, row => row.Name)
            .Add(component => component.StateKey, "orders")
            .Add(component => component.Columns, Columns(Declared))
            .Add(component => component.Load, _ => Interlocked.Increment(ref requests) > 1
                ? Task.FromException<OmniDataGridResult<Row>>(new TimeoutException("service down"))
                : Task.FromResult(new OmniDataGridResult<Row>(Rows, Rows.Count)))
            .Add(component => component.OnLoadError, (Exception _) => throw (gone ? new ObjectDisposedException("grid") : new InvalidOperationException("handler failed"))));
        grid.WaitForAssertion(() => Assert.Equal(3, grid.FindAll("tbody tr[data-omni-row-index]").Count));

        // A filter written on another device: the reload fails, and so does the host's error handler.
        preferences.Store!.Put("omni.grid.orders", OldFormat, DateTimeOffset.UtcNow.AddMinutes(1));
        await preferences.Preferences!.SyncAsync(Xunit.TestContext.Current.CancellationToken);
        await grid.InvokeAsync(() => grid.Instance.Persistence.WhenAppliedAsync());

        Assert.Equal(2, requests);
        if (gone)
        {
            Assert.Empty(logger.Entries);
        }
        else
        {
            var entry = Assert.Single(logger.Entries);
            Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Error, entry.Level);
            Assert.Contains("omni.grid.orders", entry.Message, StringComparison.Ordinal);
        }

        // The grid still follows the preference: a layout written next, on the same query, is applied.
        preferences.Store.Put("omni.grid.orders", OldFormat[..^1] + ""","version":2,"columnOrder":["City","Name","Amount"]}""",
            DateTimeOffset.UtcNow.AddMinutes(2));
        await preferences.Preferences.SyncAsync(Xunit.TestContext.Current.CancellationToken);
        await grid.InvokeAsync(() => grid.Instance.Persistence.WhenAppliedAsync());
        // The failed load shows its error instead of the table: the layout is read from the grid itself.
        Assert.Equal(["City", "Name", "Amount"], grid.Instance.ColumnSet.VisibleColumns.Select(column => column.Key));
    }

    [Fact]
    public async Task StateFromAnotherTab_IsAppliedLive()
    {
        var preferences = new GridPreferenceHarness(Services);
        var grid = RenderGrid();
        Assert.Equal(["Name", "Amount", "City"], HeaderKeys(grid));

        preferences.Seed("orders", """{"version":2,"columnOrder":["City","Name","Amount"]}""");
        await preferences.Browser.Bridge!.OnStored(preferences.StorageKey("orders"));

        grid.WaitForAssertion(() => Assert.Equal(["City", "Name", "Amount"], HeaderKeys(grid)));
    }

    [Fact]
    public async Task ResetLayout_ReturnsToTheDeclaredLayout_KeepsTheFiltersAndSorts_AndSavesTheState()
    {
        var preferences = new GridPreferenceHarness(Services, withStore: true);
        preferences.Seed("orders", $$$"""{"version":2,"filters":{"City":{"operator":{{{(int)OmniDataGridFilterOperator.DoesNotContain}}},"value":"Bari","logicalOperator":0,"secondOperator":0,"secondValue":""}},"sorts":[{"key":"Name","descending":false}],"columnWidths":{"Name":"200px"},"columnOrder":["City","Name","Amount"],"columnVisibility":{"Amount":false}}""");
        var grid = RenderGrid([new("Name"), new("Amount", SortOrder: OmniDataGridSortOrder.Descending), new("City")]);
        Assert.Equal(["City", "Name"], HeaderKeys(grid));
        Assert.Equal("ascending", grid.Find("th[data-omni-col='Name']").GetAttribute("aria-sort"));
        var sortsBefore = grid.Instance.Query.Sorts.ToArray();

        await grid.InvokeAsync(() => grid.Instance.ResetLayoutAsync());

        // The layout is the declared one again...
        Assert.Equal(["Name", "Amount", "City"], HeaderKeys(grid));
        Assert.Empty(grid.Instance.ColumnLayout.Widths);
        // ...while the reader's sort and filter stay, and so do the rows they select.
        Assert.Equal("ascending", grid.Find("th[data-omni-col='Name']").GetAttribute("aria-sort"));
        Assert.Equal(sortsBefore, grid.Instance.Query.Sorts);
        Assert.Equal(["a", "b"], grid.FindAll("tbody tr[data-omni-row-index] td[data-omni-col='Name']").Select(cell => cell.TextContent.Trim()));
        // The state is saved without the layout, not removed: the other devices keep the filters and sorts.
        var state = JsonDocument.Parse(preferences.Store!["omni.grid.orders"]!.Value!).RootElement;
        Assert.Equal(JsonValueKind.Null, state.GetProperty("columnOrder").ValueKind);
        Assert.Equal(JsonValueKind.Null, state.GetProperty("columnVisibility").ValueKind);
        Assert.Empty(state.GetProperty("columnWidths").EnumerateObject());
        Assert.Equal("Name", state.GetProperty("sorts")[0].GetProperty("key").GetString());
        Assert.Equal("Bari", state.GetProperty("filters").GetProperty("City").GetProperty("value").GetString());
        Assert.NotNull(preferences.Saved("orders"));
    }

    [Fact]
    public async Task MovingAColumnTheGridDoesNotHave_ChangesNothing()
    {
        var preferences = new GridPreferenceHarness(Services);
        var grid = RenderGrid();

        Assert.False(await grid.InvokeAsync(() => grid.Instance.Arrangement.MoveAsync("Gone", 0)));
        Assert.Equal(["Name", "Amount", "City"], HeaderKeys(grid));
        Assert.Equal(0, preferences.Browser.Writes);
    }

    [Fact]
    public async Task StateFromTheStore_ThatCannotBeRead_OrOfAnotherGrid_LeavesTheGridAsItIs_AndARemovalResetsIt()
    {
        var preferences = new GridPreferenceHarness(Services, withStore: true);
        preferences.Seed("orders", """{"version":2,"columnOrder":["City","Name","Amount"]}""");
        var grid = RenderGrid();
        Assert.Equal(["City", "Name", "Amount"], HeaderKeys(grid));
        var token = Xunit.TestContext.Current.CancellationToken;

        preferences.Store!.Put("omni.grid.orders", "pas du json", DateTimeOffset.UtcNow.AddMinutes(1));
        preferences.Store.Put("omni.grid.other", """{"version":2,"columnOrder":["Amount","City","Name"]}""", DateTimeOffset.UtcNow.AddMinutes(1));
        await preferences.Preferences!.SyncAsync(token);
        Assert.Equal(["City", "Name", "Amount"], HeaderKeys(grid));

        // Reset on another device: the tombstone brings the declaration back.
        preferences.Store.Put("omni.grid.orders", null, DateTimeOffset.UtcNow.AddMinutes(2));
        await preferences.Preferences.SyncAsync(token);
        grid.WaitForAssertion(() => Assert.Equal(["Name", "Amount", "City"], HeaderKeys(grid)));
    }

    [Fact]
    public void FormerBrowserEntryThatIsNotJson_IsLeftAlone()
    {
        var preferences = new GridPreferenceHarness(Services);
        JSInterop.Setup<string?>("localStorage.getItem", "orders").SetResult("pas du json");

        var grid = RenderGrid();

        Assert.Equal(["Name", "Amount", "City"], HeaderKeys(grid));
        Assert.Empty(JSInterop.Invocations["localStorage.removeItem"]);
        Assert.Null(preferences.Saved("orders"));
    }

    [Fact]
    public async Task ResetLayout_WithoutAStateKey_OnlyResetsTheGrid()
    {
        var preferences = new GridPreferenceHarness(Services);
        var grid = RenderGrid(stateKey: null);
        await grid.InvokeAsync(() => grid.Instance.Arrangement.SetVisibleAsync("Amount", false));
        Assert.Equal(["Name", "City"], HeaderKeys(grid));

        await grid.InvokeAsync(() => grid.Instance.Persistence.ResetLayoutAsync());

        Assert.Equal(["Name", "Amount", "City"], HeaderKeys(grid));
        Assert.Equal(0, preferences.Browser.Writes);
    }

    [Fact]
    public async Task ResetLayout_OnALostCircuit_IsQuiet()
    {
        var preferences = new GridPreferenceHarness(Services);
        preferences.Seed("orders", """{"version":2,"columnVisibility":{"Amount":false}}""");
        var grid = RenderGrid();
        preferences.Browser.Failures["set"] = new Microsoft.JSInterop.JSDisconnectedException("perdu");

        await grid.InvokeAsync(() => grid.Instance.Persistence.ResetLayoutAsync());

        Assert.Equal(["Name", "Amount", "City"], HeaderKeys(grid));
    }

    [Fact]
    public void BrowserStorageRefused_ForReadAndWrite_TheGridRenders()
    {
        // ROB-003, as the audit wrote it: the default preferences over a browser whose storage refuses
        // its reads (private mode, blocked site data) and its writes (quota).
        JSInterop.Setup<string?>("localStorage.getItem", _ => true).SetException(new JSException("SecurityError: storage denied"));
        JSInterop.SetupVoid("localStorage.setItem", _ => true).SetException(new JSException("QuotaExceededError: storage full"));

        var grid = Render<OmniDataGrid<string>>(parameters => parameters
            .Add(component => component.Items, ["b", "a"])
            .Add(component => component.StateKey, "audit-grid"));

        Assert.Equal(2, grid.FindAll("tbody tr").Count);
        Assert.Single(JSInterop.Invocations["localStorage.getItem"]);
    }

    [Fact]
    public void PreferenceScriptThatCannotLoad_LeavesTheGridRenderingItsDeclaration()
    {
        var preferences = new GridPreferenceHarness(Services);
        preferences.Browser.Failures["import"] = new JSException("SecurityError: The operation is insecure.");

        var grid = RenderGrid();

        Assert.Equal(["Name", "Amount", "City"], HeaderKeys(grid));
        Assert.Equal(3, grid.FindAll("tbody tr[data-omni-row-index]").Count);
    }

    [Fact]
    public void PreferenceScriptRefused_ForReadAndWrite_TheGridRenders_KeepsItsStateInMemory_AndLogsOnceWithoutTheState()
    {
        var preferences = new GridPreferenceHarness(Services);
        var logger = new GridLogger();
        Services.AddSingleton<Microsoft.Extensions.Logging.ILogger<OmniDataGrid<Row>>>(logger);
        preferences.Seed("orders", """{"version":2,"columnOrder":["City","Name","Amount"]}""");
        var seeded = preferences.Browser.Storage[preferences.StorageKey("orders")];
        preferences.Browser.Failures["get"] = new JSException("SecurityError: storage denied");
        preferences.Browser.Failures["set"] = new JSException("QuotaExceededError: storage full");

        var grid = RenderGrid();
        Assert.Equal(["Name", "Amount", "City"], HeaderKeys(grid));

        grid.Find("th[data-omni-col='Amount']").Click();
        grid.Find("th[data-omni-col='Amount']").Click();

        // The reader's sort is applied and kept for this visit; the state saved before is left as it was.
        Assert.Equal("descending", grid.Find("th[data-omni-col='Amount']").GetAttribute("aria-sort"));
        Assert.Equal(["c", "b", "a"], grid.FindAll("tbody tr[data-omni-row-index] td[data-omni-col='Name']").Select(cell => cell.TextContent.Trim()));
        Assert.Equal(seeded, preferences.Browser.Storage[preferences.StorageKey("orders")]);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Warning, entry.Level);
        Assert.Contains("orders", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Amount", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("City", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PreferenceStoreFailure_KeepsItsOwnPolicy_TheGridRendersFromTheBrowserCache()
    {
        // A host store that fails is the preferences' business (logged there, value kept in the browser),
        // never a browser-storage refusal of the grid.
        var preferences = new GridPreferenceHarness(Services, withStore: true);
        var logger = new GridLogger();
        Services.AddSingleton<Microsoft.Extensions.Logging.ILogger<OmniDataGrid<Row>>>(logger);
        preferences.Store!.FailReads = true;
        preferences.Store.FailWrites = true;

        var grid = RenderGrid();
        grid.Find("th[data-omni-col='Amount']").Click();

        Assert.Equal("ascending", grid.Find("th[data-omni-col='Amount']").GetAttribute("aria-sort"));
        Assert.Equal("Amount", preferences.Saved("orders")!.Value.GetProperty("sorts")[0].GetProperty("key").GetString());
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void PreferenceScriptRefused_InAContainerWithoutLogging_TheGridRendersAndSorts()
    {
        // A container built by hand may hold no logger: the refusal is then reported to nobody.
        var preferences = new GridPreferenceHarness(Services);
        Services.AddSingleton<Microsoft.Extensions.Logging.ILogger<OmniDataGrid<Row>>>(_ => null!);
        preferences.Browser.Failures["get"] = new JSException("SecurityError: storage denied");

        var grid = RenderGrid();
        grid.Find("th[data-omni-col='Amount']").Click();

        Assert.Equal("ascending", grid.Find("th[data-omni-col='Amount']").GetAttribute("aria-sort"));
    }

    private sealed class GridLogger : Microsoft.Extensions.Logging.ILogger<OmniDataGrid<Row>>
    {
        public System.Collections.Concurrent.ConcurrentQueue<(Microsoft.Extensions.Logging.LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter) => Entries.Enqueue((logLevel, formatter(state, exception)));
    }

    [Fact]
    public async Task ResetLayout_OfAGridBeingRemoved_ChangesNothingOnScreen()
    {
        var preferences = new GridPreferenceHarness(Services);
        preferences.Seed("orders", """{"version":2,"columnVisibility":{"Amount":false}}""");
        var grid = RenderGrid();
        var instance = grid.Instance;
        await grid.InvokeAsync(async () => await instance.DisposeAsync());

        await grid.InvokeAsync(() => instance.Persistence.ResetLayoutAsync());

        Assert.Equal(["Name", "City"], instance.ColumnSet.VisibleColumns.Select(column => column.Key));
    }
}
