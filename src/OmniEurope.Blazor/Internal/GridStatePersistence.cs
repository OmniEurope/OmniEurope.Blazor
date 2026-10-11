using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Keeps a grid's filters, sorts, column widths, column order and column visibility as one user
/// preference (<see cref="OmniPreferences"/>) under <see cref="PreferencePrefix"/> followed by its state
/// key, restores what is valid of a saved state, and applies again a state that changed elsewhere (the
/// host's store, another tab) while the grid is on screen.
/// </summary>
internal sealed class GridStatePersistence<TItem>(OmniDataGrid<TItem> grid) : IDisposable
{
    /// <summary>The prefix of the preference key of a grid: <c>omni.grid.</c> then its <c>StateKey</c>.</summary>
    internal const string PreferencePrefix = "omni.grid.";

    /// <summary>
    /// The version written. 1 (or none) is the former shape: filters, sorts and widths. 2 adds the column
    /// order and visibility; a state without them keeps the declared order and visibility.
    /// </summary>
    internal const int CurrentVersion = 2;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private string? _subscribedKey;
    // Counts the states applied from elsewhere (and the resets): a read still running when one arrives
    // must not bring the older state back over it.
    private int _replacements;
    private readonly Lock _applyGate = new();
    // The last state from elsewhere being applied; it never fails (its failures are logged).
    private Task _applying = Task.CompletedTask;
    private ILogger? _logger;

    internal sealed record PersistedState(
        int Version,
        Dictionary<string, GridColumnFilter>? Filters,
        List<OmniDataGridSort>? Sorts,
        Dictionary<string, string?>? ColumnWidths,
        List<string>? ColumnOrder = null,
        Dictionary<string, bool>? ColumnVisibility = null);

    internal static string PreferenceKey(string stateKey) => PreferencePrefix + stateKey;

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
            state = await grid.Preferences.GetAsync<PersistedState>(PreferenceKey(key))
                ?? await ImportLegacyAsync(key);
        }
        catch (Exception exception) when (exception is JSDisconnectedException or ObjectDisposedException)
        {
            // The circuit ended during the read: nobody is left to see the grid.
            return;
        }

        if (state is null || Volatile.Read(ref _replacements) != replacementsBefore)
        {
            return;
        }

        Restore(state);
        grid.View.InvalidateLocalProjection();
    }

    internal async Task SaveAsync()
    {
        if (grid.StateKey is not { } key)
        {
            return;
        }

        var state = new PersistedState(
            CurrentVersion,
            new Dictionary<string, GridColumnFilter>(grid.Query.Filters, StringComparer.Ordinal),
            [.. grid.Query.Sorts],
            new Dictionary<string, string?>(grid.ColumnLayout.Widths, StringComparer.Ordinal),
            grid.Arrangement.Order is { } order ? [.. order] : null,
            grid.Arrangement.Visibility.Count > 0 ? new Dictionary<string, bool>(grid.Arrangement.Visibility, StringComparer.Ordinal) : null);
        try
        {
            await grid.Preferences.SetAsync(PreferenceKey(key), state);
        }
        catch (Exception exception) when (exception is JSDisconnectedException or ObjectDisposedException)
        {
            // Best-effort: a navigation racing the save must not surface as an error.
        }
    }

    /// <summary>
    /// Back to the declared layout (order, visibility, widths), then saves the state, which keeps the
    /// filters and the sorts: the rows stay as they are, and the user's other devices take the same layout.
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
        _logger = grid.Services.GetService<ILogger<OmniDataGrid<TItem>>>();
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
            await grid.DispatchAsync(() => ApplyAsync(() => Replace(state)));
        }
        catch (Exception exception) when (exception is JSDisconnectedException or ObjectDisposedException)
        {
            // The circuit ended, or the grid went away, while the state was applied.
        }
        catch (Exception exception)
        {
            _logger?.LogError(exception,
                "The grid state {Key} changed elsewhere could not be applied.", _subscribedKey);
        }
    }

    /// <summary>A state from elsewhere in place of the grid's (null: the declared one).</summary>
    private void Replace(PersistedState? state)
    {
        if (state is null)
        {
            grid.Query.ResetToDeclared();
            grid.ColumnLayout.Restore([]);
            grid.Arrangement.Reset();
        }
        else
        {
            Restore(state);
        }
    }

    /// <summary>
    /// Applies a change of the state to a grid on screen, loading again when its query changed. A grid being
    /// removed takes no change: false, nothing applied.
    /// </summary>
    private async Task<bool> ApplyAsync(Action change)
    {
        // A change queued while the grid was being removed: no rows to load, nothing to draw.
        if (grid.DisposeRequested)
        {
            return false;
        }

        var filtersBefore = grid.Query.Filters.ToArray();
        var sortsBefore = grid.Query.Sorts.ToArray();
        change();
        grid.LayoutInterop.InvalidateColumnLayout();
        if (!SameQuery(filtersBefore, sortsBefore))
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
    /// declared one, an incomplete entry is dropped, and a missing filter text is an empty one.
    /// </summary>
    private void Restore(PersistedState state)
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
        grid.Arrangement.Restore(state.ColumnOrder, state.ColumnVisibility);
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
