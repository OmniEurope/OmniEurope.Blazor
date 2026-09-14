using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class DataListVirtualizationTests : OmniBunitContext
{
    private const string GridModulePath = "./_content/OmniEurope.Blazor/omni-grid.js";

    [Fact]
    public void VirtualizedList_RendersOnlyAWindowAndNoStyleAttribute()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var list = Render<OmniDataList<int>>(parameters => parameters
            .Add(component => component.Items, Enumerable.Range(0, 10_000).ToArray())
            .Add(component => component.Virtualize, true)
            .Add(component => component.ItemTemplate, ItemTemplate));

        Assert.InRange(list.FindAll(".omni-data-list__item").Count, 1, 64);
        Assert.Equal(2, list.FindAll(".omni-data-list__spacer").Count);
        // The strict CSP forbids style attributes: the spacer heights travel as custom properties.
        Assert.DoesNotContain("style=", list.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VirtualizedList_MovesItsWindowWhenTheScrollAreaMoves()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var list = Render<OmniDataList<int>>(parameters => parameters
            .Add(component => component.Items, Enumerable.Range(0, 10_000).ToArray())
            .Add(component => component.Virtualize, true)
            .Add(component => component.ItemTemplate, ItemTemplate));

        Assert.Contains(">Élément 0<", list.Markup, StringComparison.Ordinal);

        // 32 000 px down with the 32 px estimate puts item 1000 at the top of the visible area.
        await list.InvokeAsync(() => list.Instance.OnViewportChangedAsync(32_000d, 320d));

        Assert.DoesNotContain(">Élément 0<", list.Markup, StringComparison.Ordinal);
        Assert.Contains(">Élément 1000<", list.Markup, StringComparison.Ordinal);
        Assert.Equal("996", list.FindAll(".omni-data-list__item")[0].GetAttribute("data-omni-row-index"));
    }

    [Fact]
    public void VirtualizedList_SendsTheSpacerHeightsToTheScript()
    {
        var module = JSInterop.SetupModule(GridModulePath);
        module.Mode = JSRuntimeMode.Loose;

        var list = Render<OmniDataList<int>>(parameters => parameters
            .Add(component => component.Items, Enumerable.Range(0, 5_000).ToArray())
            .Add(component => component.Virtualize, true)
            .Add(component => component.ItemTemplate, ItemTemplate));

        list.WaitForAssertion(() =>
        {
            Assert.Single(module.Invocations["attachList"]);
            var layout = module.Invocations["applyListLayout"][0];
            Assert.Equal(0d, Convert.ToDouble(layout.Arguments[1], CultureInfo.InvariantCulture));
            Assert.True(
                Convert.ToDouble(layout.Arguments[2], CultureInfo.InvariantCulture) > 0d,
                "The bottom spacer must stand in for the items below the window.");
        });
    }

    [Fact]
    public void PlainList_RendersEveryItemAndNeedsNoScript()
    {
        var list = Render<OmniDataList<int>>(parameters => parameters
            .Add(component => component.Items, Enumerable.Range(0, 50).ToArray())
            .Add(component => component.ItemTemplate, ItemTemplate));

        Assert.Equal(50, list.FindAll(".omni-data-list__item").Count);
        Assert.Empty(list.FindAll(".omni-data-list__spacer"));
        Assert.Empty(JSInterop.Invocations);
    }

    private static RenderFragment<int> ItemTemplate => item => builder => builder.AddContent(0, $"Élément {item}");
}
