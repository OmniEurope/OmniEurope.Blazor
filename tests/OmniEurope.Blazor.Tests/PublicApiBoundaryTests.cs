using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class PublicApiBoundaryTests
{
    [Fact]
    public void CascadingCoordinationTypes_AreNotPublicApi()
    {
        var assembly = typeof(OmniDataGrid<>).Assembly;
        var names = new[]
        {
            "OmniEurope.Blazor.Components.OmniDataGridColumnDefinition`1",
            "OmniEurope.Blazor.Components.OmniDataGridContext`1",
            "OmniEurope.Blazor.Components.OmniTabsContext",
            "OmniEurope.Blazor.Components.OmniStepsContext",
            "OmniEurope.Blazor.Components.OmniTreeContext`1"
        };

        foreach (var name in names)
        {
            var type = assembly.GetType(name);
            Assert.NotNull(type);
            Assert.False(type!.IsPublic, name);
        }
    }
}
