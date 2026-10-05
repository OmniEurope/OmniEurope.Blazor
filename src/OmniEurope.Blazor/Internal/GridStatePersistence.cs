using System.Text.Json;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Saves a grid's filters, sorts and column widths under its state key, and restores what is valid
/// of a saved entry.
/// </summary>
internal sealed class GridStatePersistence<TItem>(OmniDataGrid<TItem> grid)
{
    private sealed record PersistedState(
        Dictionary<string, GridColumnFilter> Filters,
        List<OmniDataGridSort> Sorts,
        Dictionary<string, string?> ColumnWidths);

    private IOmniDataGridStateStore EffectiveStateStore => grid.StateStore ?? grid.InjectedStateStore;

    internal async Task LoadAsync(string key)
    {
        string? json;
        var store = EffectiveStateStore;
        try
        {
            json = await store.LoadAsync(key);
        }
        catch (JSDisconnectedException)
        {
            return;
        }
        catch (InvalidOperationException) when (store is OmniLocalStorageDataGridStateStore)
        {
            // Prerendering: browser storage cannot be reached yet. The interactive render creates the
            // grid again and restores its state then.
            return;
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        PersistedState? state;
        try
        {
            state = JsonSerializer.Deserialize<PersistedState>(json);
        }
        catch (JsonException)
        {
            // A stale or hand-edited entry from a previous grid shape must not block the page.
            return;
        }

        if (state is null)
        {
            return;
        }

        // Valid JSON can still miss a part or hold entries of an older shape: a missing part restores
        // nothing, an incomplete entry is dropped, and a missing filter text is an empty one.
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
        grid.View.InvalidateLocalProjection();
    }

    internal async Task SaveAsync()
    {
        if (grid.StateKey is not { } key)
        {
            return;
        }

        var state = new PersistedState(
            new Dictionary<string, GridColumnFilter>(grid.Query.Filters, StringComparer.Ordinal),
            [.. grid.Query.Sorts],
            new Dictionary<string, string?>(grid.ColumnLayout.Widths, StringComparer.Ordinal));
        var json = JsonSerializer.Serialize(state);
        try
        {
            await EffectiveStateStore.SaveAsync(key, json);
        }
        catch (JSDisconnectedException)
        {
            // Best-effort: a navigation racing the save must not surface as an error.
        }
    }
}
