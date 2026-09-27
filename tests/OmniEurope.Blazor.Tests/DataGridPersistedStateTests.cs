using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Restoring a grid's saved state (<see cref="OmniDataGrid{TItem}.StateKey"/>) must never keep the
/// grid from rendering: not while prerendering, where browser storage cannot be reached, and not on
/// a saved entry that is valid JSON but incomplete or stale.
/// </summary>
public sealed class DataGridPersistedStateTests : OmniBunitContext
{
    [Fact]
    public void DefaultStore_UnreachableWhilePrerendering_LeavesTheGridRendering()
    {
        // What the default localStorage store meets during a static prerender.
        JSInterop.Setup<string?>("localStorage.getItem", "orders")
            .SetException(new InvalidOperationException("JavaScript interop calls cannot be issued at this time. This is because the component is being statically rendered."));

        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, [1, 2])
            .Add(component => component.StateKey, "orders"));

        Assert.Equal(2, grid.FindAll("tbody tr").Count);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"Filters\":null,\"Sorts\":null,\"ColumnWidths\":null}")]
    [InlineData("{\"Filters\":{\"Value\":null,\"Other\":{}},\"Sorts\":[null,{}],\"ColumnWidths\":{}}")]
    public void IncompleteSavedState_IsIgnoredWhereInvalid_AndTheGridRenders(string saved)
    {
        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, [1, 2])
            .Add(component => component.StateKey, "orders")
            .Add(component => component.StateStore, new FixedStore(saved)));

        Assert.Equal(2, grid.FindAll("tbody tr").Count);
    }

    private sealed class FixedStore(string state) : IOmniDataGridStateStore
    {
        public Task<string?> LoadAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult<string?>(state);

        public Task SaveAsync(string key, string state, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
