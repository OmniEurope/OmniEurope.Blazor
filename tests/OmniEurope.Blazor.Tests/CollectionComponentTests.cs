using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class CollectionComponentTests : BunitContext
{
    [Fact]
    public void DataList_RendersItemsAndEmptyState()
    {
        var list = Render<OmniDataList<int>>(parameters => parameters
            .Add(component => component.Items, [1, 2, 3])
            .Add(component => component.ItemTemplate, ItemTemplate));

        Assert.Equal(3, list.FindAll(".omni-data-list__item").Count);
        Assert.Contains("Élément 2", list.Markup, StringComparison.Ordinal);

        var empty = Render<OmniDataList<int>>(parameters => parameters
            .Add(component => component.ItemTemplate, ItemTemplate));
        Assert.Contains("Aucun élément.", empty.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void DataList_CanRetryAFailedRemoteLoad()
    {
        var attempts = 0;
        var list = Render<OmniDataList<int>>(parameters => parameters
            .Add(component => component.Load, _ =>
            {
                attempts++;
                return attempts == 1
                    ? Task.FromException<IReadOnlyList<int>>(new InvalidOperationException("offline"))
                    : Task.FromResult<IReadOnlyList<int>>([7]);
            })
            .Add(component => component.ItemTemplate, ItemTemplate));

        Assert.Contains("Le chargement a échoué.", list.Markup, StringComparison.Ordinal);
        list.Find("button").Click();

        Assert.Equal(2, attempts);
        Assert.Contains("Élément 7", list.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void PagerAndTree_AreControlledAndKeyboardOperable()
    {
        var page = 2;
        var pager = Render<OmniPager>(parameters => parameters
            .Add(component => component.Page, page)
            .Add(component => component.PageCount, 4)
            .Add(component => component.PageChanged, value => page = value));
        pager.FindAll("button")[1].Click();
        Assert.Equal(3, page);

        var tree = Render<TreeTestHost>();
        tree.FindAll(".omni-tree__select")[0].Click();
        Assert.Equal(["root"], tree.Instance.Selected);
        tree.FindAll("[role=treeitem]")[1].KeyDown("Enter");

        Assert.Equal(["root", "child"], tree.Instance.Selected);
        Assert.Equal("true", tree.Find("[role=tree]").GetAttribute("aria-multiselectable"));
        Assert.DoesNotContain("style=", tree.Markup, StringComparison.OrdinalIgnoreCase);
    }

    private static RenderFragment<int> ItemTemplate => item => builder => builder.AddContent(0, $"Élément {item}");
}
