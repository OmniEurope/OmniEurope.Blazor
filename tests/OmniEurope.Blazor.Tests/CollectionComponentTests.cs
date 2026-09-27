using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class CollectionComponentTests : OmniBunitContext
{
    [Fact]
    public void TreeItem_SynchronizesControlledExpansionAndReportsLoadFailures()
    {
        var item = Render<TreeControlledTestHost>();

        item.InvokeAsync(item.Instance.Expand);
        Assert.Equal("true", item.Find("[role=treeitem]").GetAttribute("aria-expanded"));

        item.Find(".omni-tree__toggle").Click();
        item.Find(".omni-tree__toggle").Click();
        item.WaitForAssertion(() => Assert.IsType<InvalidOperationException>(item.Instance.ObservedException));
        Assert.Equal("alert", item.Find(".omni-tree__state").GetAttribute("role"));
    }

    [Fact]
    public void TreeItem_ExpandedInitially_LoadsItsChildrenOnce()
    {
        var loads = 0;
        var item = Render<OmniTreeItem<string>>(parameters => parameters
            .Add(component => component.Value, "root")
            .Add(component => component.Text, "Racine")
            .Add(component => component.Expanded, true)
            .Add(component => component.LoadChildren, _ => { loads++; return Task.CompletedTask; }));

        Assert.Equal(1, loads);
        item.Render();
        Assert.Equal(1, loads);
    }

    [Fact]
    public void TreeItem_ExpandedByTheParent_LoadsItsChildren()
    {
        var loads = 0;
        Func<CancellationToken, Task> loader = _ => { loads++; return Task.CompletedTask; };
        var item = Render<OmniTreeItem<string>>(parameters => parameters
            .Add(component => component.Value, "root")
            .Add(component => component.Text, "Racine")
            .Add(component => component.LoadChildren, loader));
        Assert.Equal(0, loads);

        item.Render(parameters => parameters.Add(component => component.Expanded, true));

        Assert.Equal(1, loads);
        Assert.Equal("true", item.Find("[role=treeitem]").GetAttribute("aria-expanded"));
    }

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
    public async Task DataList_NewLoaderWhileTheFirstIsStillLoading_ReplacesIt()
    {
        var pending = new TaskCompletionSource<IReadOnlyList<int>>();
        var list = Render<OmniDataList<int>>(parameters => parameters
            .Add(component => component.Load, _ => pending.Task)
            .Add(component => component.ItemTemplate, ItemTemplate));

        list.Render(parameters => parameters
            .Add(component => component.Load, _ => Task.FromResult<IReadOnlyList<int>>([8])));
        Assert.Contains("Élément 8", list.Markup, StringComparison.Ordinal);

        await list.InvokeAsync(() => pending.SetResult([1]));
        Assert.Contains("Élément 8", list.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Élément 1", list.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void DataList_NewLoaderAfterAFailedLoad_Loads()
    {
        var list = Render<OmniDataList<int>>(parameters => parameters
            .Add(component => component.Load, _ => Task.FromException<IReadOnlyList<int>>(new InvalidOperationException("offline")))
            .Add(component => component.ItemTemplate, ItemTemplate));

        list.Render(parameters => parameters
            .Add(component => component.Load, _ => Task.FromResult<IReadOnlyList<int>>([8])));

        Assert.Contains("Élément 8", list.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void DataList_TreatsAnEmptyResultAsLoadedAndReloadsWhenTheDelegateChanges()
    {
        var firstCalls = 0;
        var secondCalls = 0;
        Func<CancellationToken, Task<IReadOnlyList<int>>> first = _ =>
        {
            firstCalls++;
            return Task.FromResult<IReadOnlyList<int>>([]);
        };
        Func<CancellationToken, Task<IReadOnlyList<int>>> second = _ =>
        {
            secondCalls++;
            return Task.FromResult<IReadOnlyList<int>>([]);
        };

        var list = Render<OmniDataList<int>>(parameters => parameters
            .Add(component => component.Load, first)
            .Add(component => component.ItemTemplate, ItemTemplate));
        list.Render(parameters => parameters
            .Add(component => component.Load, first)
            .Add(component => component.ItemTemplate, ItemTemplate));
        Assert.Equal(1, firstCalls);

        list.Render(parameters => parameters
            .Add(component => component.Load, second)
            .Add(component => component.ItemTemplate, ItemTemplate));
        Assert.Equal(1, secondCalls);
        Assert.Contains("Aucun élément.", list.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Pager_ChangesTheControlledPage()
    {
        var page = 2;
        var pager = Render<OmniPager>(parameters => parameters
            .Add(component => component.Page, page)
            .Add(component => component.PageCount, 4)
            .Add(component => component.PageChanged, value => page = value));
        pager.FindAll("button")[1].Click();
        Assert.Equal(3, page);
    }

    [Fact]
    public void Tree_MouseSelectionUsesStableValues()
    {
        var tree = Render<TreeTestHost>();
        tree.FindAll(".omni-tree__select")[0].Click();
        Assert.Equal(["root"], tree.Instance.Selected);
    }

    [Fact]
    public void Tree_KeyboardSelectionAndAriaAreCoherent()
    {
        var tree = Render<TreeTestHost>();
        tree.FindAll("[role=treeitem]")[1].KeyDown("Enter");

        Assert.Equal(["child"], tree.Instance.Selected);
        Assert.Equal("true", tree.Find("[role=tree]").GetAttribute("aria-multiselectable"));
        Assert.DoesNotContain("style=", tree.Markup, StringComparison.OrdinalIgnoreCase);
    }

    private static RenderFragment<int> ItemTemplate => item => builder => builder.AddContent(0, $"Élément {item}");
}
