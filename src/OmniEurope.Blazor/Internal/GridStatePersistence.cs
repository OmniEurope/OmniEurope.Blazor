using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Keeps a grid's filters, sorts, page size, groups, column widths, column order, column visibility and frozen
/// columns as one user preference (<see cref="OmniPreferences"/>) under <see cref="PreferencePrefix"/> followed
/// by its state key (its <c>StateKey</c>, else the key <see cref="GridStateKey"/> derives), restores what is
/// valid of a saved state, and applies again a state that changed elsewhere (the host's store, another tab)
/// while the grid is on screen.
/// </summary>
internal sealed class GridStatePersistence<TItem>(IGridHost<TItem> grid) : IGridStatePersistence, IDisposable
{
    /// <summary>The prefix of the preference key of a grid: <c>omni.grid.</c> then its <c>StateKey</c>.</summary>
    internal const string PreferencePrefix = GridStateKey.PreferencePrefix;

    /// <summary>
    /// The version written. 1 (or none) is the former shape: filters, sorts and widths. 2 adds the column
    /// order and visibility; a state without them keeps the declared order and visibility. 3 adds the page
    /// size, the groups and the frozen columns the reader chose; a state without them keeps the current ones.
    /// </summary>
    internal const int CurrentVersion = 3;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private string? _subscribedKey;
    // Counts the states applied from elsewhere (and the resets): a read still running when one arrives
    // must not bring the older state back over it.
    private int _replacements;
    private readonly Lock _applyGate = new();
    // The last state from elsewhere being applied; it never fails (its failures are logged).
    private Task _applying = Task.CompletedTask;
    private ILogger _logger = NullLogger.Instance;
    private bool _storageRefusalReported;
    // The key derived for a grid without a StateKey (see GridStateKey), once resolved.
    private string? _derivedKey;
    // A derived key that needs the declared columns: resolved after the first render.
    private bool _derivedKeyPending;

    internal sealed record PersistedState(
        int Version,
        Dictionary<string, GridColumnFilter>? Filters,
        List<OmniDataGridSort>? Sorts,
        Dictionary<string, string?>? ColumnWidths,
        List<string>? ColumnOrder = null,
        Dictionary<string, bool>? ColumnVisibility = null,
        int? PageSize = null,
        List<OmniDataGridGroup>? Groups = null,
        Dictionary<string, bool>? ColumnFrozen = null);

    internal static string PreferenceKey(string stateKey) => PreferencePrefix + stateKey;

    /// <summary>
    /// The key the grid keeps its state under: its <c>StateKey</c>, else the derived one, none when the grid
    /// opts out (<c>PersistState</c> false) or keeps nothing.
    /// </summary>
    internal string? CurrentKey => grid.PersistState == false ? null : grid.StateKey ?? _derivedKey;

    /// <summary>
    /// The key to read before the first render: the <c>StateKey</c>, or the derived key of a grid with an id
    /// when the host keeps every grid's state (<c>PersistGridState</c>) or the grid asks for it
    /// (<c>PersistState</c> true). Without an id the derived key needs the declared columns: it is left to
    /// <see cref="ResolveDeferredKey"/>. Null when nothing is kept, or not yet.
    /// </summary>
    internal string? ResolveInitialKey()
    {
        if (grid.PersistState == false)
        {
            return null;
        }

        if (grid.StateKey is { } stateKey)
        {
            return stateKey;
        }

        if (grid.PersistState != true && !grid.Preferences.PersistGridState)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(grid.Id))
        {
            _derivedKeyPending = true;
            return null;
        }

        return _derivedKey = GridStateKey.Derive(grid.Services.GetService<NavigationManager>(), grid.Id, () => []);
    }

    /// <summary>The derived key of a grid without an id, once its columns registered (after its first render); null otherwise.</summary>
    internal string? ResolveDeferredKey()
    {
        if (!_derivedKeyPending)
        {
            return null;
        }

        _derivedKeyPending = false;
        return _derivedKey = GridStateKey.Derive(
            grid.Services.GetService<NavigationManager>(), null, () => grid.ColumnSet.Declared.Select(column => column.Key));
    }

    /// <summary>
    /// Reads the saved state and follows its later changes. Without a preference, an entry the former
    /// browser store left under the bare state key is imported once, then removed, when the host registered
    /// no store (see <see cref="ImportLegacyAsync"/>).
    /// </summary>
    internal async Task LoadAsync(string key)
    {
        Follow(key);
        var replacementsBefore = Volatile.Read(ref _replacements);
        PersistedState? state;
        try
        {
            // A derived key never had an entry of the former browser store.
            state = await grid.Preferences.GetAsync<PersistedState>(PreferenceKey(key))
                ?? (key == _derivedKey ? null : await ImportLegacyAsync(key));
        }
        catch (Exception exception) when (exception is JSDisconnectedException or ObjectDisposedException)
        {
            // The circuit ended during the read: nobody is left to see the grid.
            return;
        }
        catch (JSException exception)
        {
            // The browser refused its storage: the grid starts from its declaration.
            ReportStorageRefused(key, exception);
            return;
        }

        if (state is null || Volatile.Read(ref _replacements) != replacementsBefore)
        {
            return;
        }

        await RestoreAsync(state);
        grid.View.InvalidateLocalProjection();
    }

    public async Task SaveAsync()
    {
        if (CurrentKey is not { } key)
        {
            return;
        }

        var state = new PersistedState(
            CurrentVersion,
            new Dictionary<string, GridColumnFilter>(grid.Query.Filters, StringComparer.Ordinal),
            [.. grid.Query.Sorts],
            new Dictionary<string, string?>(grid.ColumnLayout.Widths, StringComparer.Ordinal),
            grid.Arrangement.Order is { } order ? [.. order] : null,
            grid.Arrangement.Visibility.Count > 0 ? new Dictionary<string, bool>(grid.Arrangement.Visibility, StringComparer.Ordinal) : null,
            grid.Paging.ChosenPageSize,
            grid.Grouping.ChosenGroups is { } groups ? [.. groups] : null,
            grid.Arrangement.Frozen.Count > 0 ? new Dictionary<string, bool>(grid.Arrangement.Frozen, StringComparer.Ordinal) : null);
        try
        {
            await grid.Preferences.SetAsync(PreferenceKey(key), state);
        }
        catch (Exception exception) when (exception is JSDisconnectedException or ObjectDisposedException)
        {
            // Best-effort: a navigation racing the save must not surface as an error.
        }
        catch (JSException exception)
        {
            // The browser refused its storage: the change stays on screen for this visit, and what was
            // saved before is left as it was.
            ReportStorageRefused(key, exception);
        }
    }

    /// <summary>
    /// The browser refused its storage (private mode, blocked site data, full quota): persistence is
    /// unavailable and the grid works in memory. Logged once per grid, with its state key, never its state.
    /// A failure of the host's store does not come here: <see cref="OmniPreferences"/> applies its own policy.
    /// </summary>
    private void ReportStorageRefused(string key, JSException exception)
    {
        if (_storageRefusalReported)
        {
            return;
        }

        _storageRefusalReported = true;
        _logger.LogWarning(exception,
            "The browser storage refused the state of the grid {Key}; the grid keeps its filters, sorts and layout for this visit only.", key);
    }

    /// <summary>
    /// Back to the declared layout (order, visibility, frozen columns, widths), then saves the state, which
    /// keeps the filters, the sorts, the page size and the groups: the rows stay as they are, and the user's other devices take the same layout.
    /// The preference is not removed, it would take the filters and sorts with it (owner decision, PLAN-019
    /// lot 4). A state removed elsewhere still brings everything back to the declaration
    /// (<see cref="OnPreferenceChangedAsync"/>).
    /// </summary>
    internal async Task ResetLayoutAsync()
    {
        // A read still running must not bring the former layout back over this one.
        Interlocked.Increment(ref _replacements);
        var applied = await ApplyAsync(() =>
        {
            grid.ColumnLayout.Restore([]);
            grid.Arrangement.Reset();
            return Task.CompletedTask;
        });
        if (applied)
        {
            await SaveAsync();
        }
    }

    /// <summary>Stops following the preference.</summary>
    public void Dispose()
    {
        if (_subscribedKey is not null)
        {
            grid.Preferences.Changed -= OnPreferenceChangedAsync;
            _subscribedKey = null;
        }
    }

    // Called once, from the grid's OnInitializedAsync.
    private void Follow(string key)
    {
        // Every Blazor host registers logging; a container built by hand may not, and the grid then logs nothing.
        _logger = grid.Logger;
        grid.Preferences.Changed += OnPreferenceChangedAsync;
        _subscribedKey = PreferenceKey(key);
    }

    /// <summary>A state written elsewhere: by the host's store (another device) or by another tab.</summary>
    private Task OnPreferenceChangedAsync(OmniPreferenceChangedEventArgs change)
    {
        // A local write is this grid's own (or a grid of the same key in this circuit), already shown.
        if (change.Origin == OmniPreferenceChangeOrigin.Local || change.Key != _subscribedKey)
        {
            return Task.CompletedTask;
        }

        PersistedState? state = null;
        if (change.Value is not null && !TryParse(change.Value, out state))
        {
            // An unreadable state from elsewhere keeps the grid as it is.
            return Task.CompletedTask;
        }

        Interlocked.Increment(ref _replacements);
        // Not awaited: a reload of a slow loader must not hold the preferences (their synchronization,
        // the other handlers). The states from elsewhere are applied one after the other, in order.
        lock (_applyGate)
        {
            _applying = ApplyAfterAsync(_applying, state);
        }

        return Task.CompletedTask;
    }

    /// <summary>Completes once the states from elsewhere received so far are applied.</summary>
    internal Task WhenAppliedAsync()
    {
        lock (_applyGate)
        {
            return _applying;
        }
    }

    private async Task ApplyAfterAsync(Task previous, PersistedState? state)
    {
        await previous;
        try
        {
            await grid.DispatchAsync(() => ApplyAsync(() => ReplaceAsync(state)));
        }
        catch (Exception exception) when (exception is JSDisconnectedException or ObjectDisposedException)
        {
            // The circuit ended, or the grid went away, while the state was applied.
        }
        catch (Exception exception)
        {
            _logger.LogError(exception,
                "The grid state {Key} changed elsewhere could not be applied.", _subscribedKey);
        }
    }

    /// <summary>
    /// A state from elsewhere in place of the grid's (null: the declared layout and query; the page size and
    /// the groups stay as they are).
    /// </summary>
    private Task ReplaceAsync(PersistedState? state)
    {
        if (state is not null)
        {
            return RestoreAsync(state);
        }

        grid.Query.ResetToDeclared();
        grid.ColumnLayout.Restore([]);
        grid.Arrangement.Reset();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Applies a change of the state to a grid on screen, loading again when its query changed. A grid being
    /// removed takes no change: false, nothing applied.
    /// </summary>
    private async Task<bool> ApplyAsync(Func<Task> change)
    {
        // A change queued while the grid was being removed: no rows to load, nothing to draw.
        if (grid.DisposeRequested)
        {
            return false;
        }

        var filtersBefore = grid.Query.Filters.ToArray();
        var sortsBefore = grid.Query.Sorts.ToArray();
        var pageSizeBefore = grid.Paging.PageSize;
        var groupsBefore = grid.Grouping.ActiveGroups;
        await change();
        grid.LayoutInterop.InvalidateColumnLayout();
        if (!groupsBefore.SequenceEqual(grid.Grouping.ActiveGroups))
        {
            grid.View.InvalidateLocalProjection();
        }

        // Another page size moves the rows of every page: back to the first one, like a new query.
        if (!SameQuery(filtersBefore, sortsBefore) || pageSizeBefore != grid.Paging.PageSize)
        {
            await grid.Paging.ResetToFirstPageAsync();
            grid.View.InvalidateLocalProjection();
            await grid.View.RefreshAfterQueryChangeAsync();
        }

        grid.RebuildRenderSnapshot();
        grid.Render();
        return true;
    }

    private bool SameQuery(KeyValuePair<string, GridColumnFilter>[] filters, OmniDataGridSort[] sorts) =>
        filters.Length == grid.Query.Filters.Count
        && filters.All(pair => grid.Query.Filters.TryGetValue(pair.Key, out var filter) && filter == pair.Value)
        && sorts.SequenceEqual(grid.Query.Sorts);

    /// <summary>
    /// Valid JSON can still miss a part or hold entries of an older shape: a missing part restores the
    /// declared one (the page size and the groups keep the current ones), an incomplete entry is dropped, and
    /// a missing filter text is an empty one. A restored page size or groups are reported to the host
    /// (<c>PageSizeChanged</c>, <c>GroupsChanged</c>), as the reader's own change would be.
    /// </summary>
    private async Task RestoreAsync(PersistedState state)
    {
        var filters = new List<KeyValuePair<string, GridColumnFilter>>();
        foreach (var (columnKey, filter) in state.Filters ?? [])
        {
            if (filter is not null
                && Enum.IsDefined(filter.Operator)
                && Enum.IsDefined(filter.LogicalOperator)
                && Enum.IsDefined(filter.SecondOperator))
            {
                filters.Add(new(columnKey, filter with { Value = filter.Value ?? string.Empty, SecondValue = filter.SecondValue ?? string.Empty }));
            }
        }

        grid.Query.Restore(filters, (state.Sorts ?? []).Where(sort => sort is { Key: not null }));
        grid.ColumnLayout.Restore(state.ColumnWidths ?? []);
        grid.Arrangement.Restore(state.ColumnOrder, state.ColumnVisibility, state.ColumnFrozen);
        if (grid.Paging.Restore(state.PageSize))
        {
            await grid.PageSizeChanged.InvokeAsync(grid.Paging.PageSize);
        }

        if (grid.Grouping.Restore(state.Groups))
        {
            await grid.GroupsChanged.InvokeAsync(grid.Grouping.ActiveGroups);
        }
    }

    /// <summary>
    /// The former <c>OmniLocalStorageDataGridStateStore</c> wrote the state under the bare state key. Such
    /// an entry becomes the preference, then is removed. Only an entry of that store is taken: it always
    /// held its three parts; any other value under that key belongs to the host and is left alone.
    /// </summary>
    /// <remarks>
    /// The bare key is shared by every user of the browser, so it is only imported while the preferences
    /// have no user scope either (no host store, scope <c>local</c>). With a host store, the entry may be
    /// another user's grid: it is left alone rather than written into this user's store.
    /// </remarks>
    private async Task<PersistedState?> ImportLegacyAsync(string key)
    {
        if (grid.Preferences.HasStore)
        {
            return null;
        }

        string? json;
        try
        {
            json = await grid.JavaScript.InvokeAsync<string?>("localStorage.getItem", key);
        }
        catch (InvalidOperationException)
        {
            // Prerendering: the browser storage cannot be reached yet; the interactive render imports it.
            return null;
        }
        catch (JSException)
        {
            // The browser refused its storage (disabled, private mode): nothing to import, the grid goes on.
            return null;
        }

        if (string.IsNullOrWhiteSpace(json)
            || !TryParse(json, out var legacy)
            || legacy is not { Filters: not null, Sorts: not null, ColumnWidths: not null })
        {
            return null;
        }

        var state = legacy with { Version = CurrentVersion };
        await grid.Preferences.SetAsync(PreferenceKey(key), state);
        try
        {
            await grid.JavaScript.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch (JSException)
        {
            // Left in place: the preference now exists, so the entry is never read again.
        }

        return state;
    }

    private static bool TryParse(string json, out PersistedState? state)
    {
        try
        {
            state = JsonSerializer.Deserialize<PersistedState>(json, JsonOptions);
            return state is not null;
        }
        catch (JsonException)
        {
            state = null;
            return false;
        }
    }
}
