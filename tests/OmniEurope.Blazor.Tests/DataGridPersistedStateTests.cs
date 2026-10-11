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
    public void BrowserUnreachableWhilePrerendering_LeavesTheGridRendering()
    {
        // What the preferences and the legacy import meet during a static prerender without a store.
        const string prerendering = "JavaScript interop calls cannot be issued at this time. This is because the component is being statically rendered.";
        var preferences = new GridPreferenceHarness(Services);
        preferences.Browser.ImportFailure = new InvalidOperationException(prerendering);
        JSInterop.Setup<string?>("localStorage.getItem", "orders").SetException(new InvalidOperationException(prerendering));

        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, [1, 2])
            .Add(component => component.StateKey, "orders"));

        Assert.Equal(2, grid.FindAll("tbody tr").Count);
        Assert.Single(JSInterop.Invocations["localStorage.getItem"]);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"Filters\":null,\"Sorts\":null,\"ColumnWidths\":null}")]
    [InlineData("{\"Filters\":{\"Value\":null,\"Other\":{}},\"Sorts\":[null,{}],\"ColumnWidths\":{}}")]
    [InlineData("{\"version\":2,\"columnOrder\":[]}")]
    [InlineData("{\"version\":2,\"columnOrder\":[null,\"\",\"missing\"],\"columnVisibility\":{\"missing\":false}}")]
    public void IncompleteSavedState_IsIgnoredWhereInvalid_AndTheGridRenders(string saved)
    {
        var preferences = new GridPreferenceHarness(Services);
        preferences.Seed("orders", saved);

        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, [1, 2])
            .Add(component => component.StateKey, "orders"));

        Assert.Equal(2, grid.FindAll("tbody tr").Count);
    }
}
