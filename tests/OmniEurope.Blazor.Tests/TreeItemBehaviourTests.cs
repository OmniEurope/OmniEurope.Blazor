using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// OmniTreeItem: the keys of an item, an item rendered without its tree, the drag gestures that land
/// nowhere, and children loads that fail or are replaced.
/// </summary>
public sealed class TreeItemBehaviourTests : OmniBunitContext
{
    private static IElement Item(IRenderedComponent<TreeDragDropTestHost> host, string text) =>
        host.FindAll(".omni-tree__item").First(item => item.QuerySelector(".omni-tree__row")!.TextContent.Contains(text, StringComparison.Ordinal));

    private static IElement Row(IRenderedComponent<TreeDragDropTestHost> host, string text) =>
        host.FindAll(".omni-tree__row").Single(row => row.TextContent.Contains(text, StringComparison.Ordinal));

    [Fact]
    public void Arrows_OpenAClosedBranch_AndCloseAnOpenOne_OnlyOnce()
    {
        var host = Render<TreeDragDropTestHost>();

        Item(host, "60 - Achats").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.True(host.Instance.AchatsOpen);
        Item(host, "60 - Achats").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.True(host.Instance.AchatsOpen);

        Item(host, "60 - Achats").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.False(host.Instance.AchatsOpen);
        Item(host, "60 - Achats").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.False(host.Instance.AchatsOpen);
    }

    [Fact]
    public void Arrows_OnALeaf_AndOtherKeys_DoNothing()
    {
        var host = Render<TreeDragDropTestHost>();
        var markup = host.Markup;

        Item(host, "7 - Produits").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Item(host, "7 - Produits").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        Item(host, "7 - Produits").KeyDown(new KeyboardEventArgs { Key = "Tab" });

        Assert.Equal(markup, host.Markup);
    }

    [Theory]
    [InlineData("Enter")]
    [InlineData(" ")]
    public void EnterOrSpace_SelectsTheItem_UnlessItIsDisabled(string key)
    {
        IReadOnlyList<string> selected = [];
        RenderFragment items = builder =>
        {
            builder.OpenComponent<OmniTreeItem<string>>(0);
            builder.AddComponentParameter(1, nameof(OmniTreeItem<string>.Value), "a");
            builder.AddComponentParameter(2, nameof(OmniTreeItem<string>.Text), "A");
            builder.CloseComponent();
            builder.OpenComponent<OmniTreeItem<string>>(3);
            builder.AddComponentParameter(4, nameof(OmniTreeItem<string>.Value), "b");
            builder.AddComponentParameter(5, nameof(OmniTreeItem<string>.Text), "B");
            builder.AddComponentParameter(6, nameof(OmniTreeItem<string>.Disabled), true);
            builder.CloseComponent();
        };
        var tree = Render<OmniTree<string>>(parameters => parameters
            .Add(component => component.ChildContent, items)
            .Add(component => component.ValueChanged, values => selected = values));

        tree.FindAll(".omni-tree__item")[1].KeyDown(new KeyboardEventArgs { Key = key });
        Assert.Empty(selected);

        tree.FindAll(".omni-tree__item")[0].KeyDown(new KeyboardEventArgs { Key = key });
        Assert.Equal(["a"], selected);
    }

    [Fact]
    public void ItemWithoutATree_SelectsNothing_AndIgnoresTheDragGestures()
    {
        var item = Render<OmniTreeItem<string>>(parameters => parameters
            .Add(component => component.Value, "seul")
            .Add(component => component.Text, "Seul"));
        var row = item.Find(".omni-tree__row");

        item.Find(".omni-tree__item").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        row.DragStart();
        row.DragEnter();
        row.DragLeave();
        row.Drop();
        row.DragEnd();

        Assert.Null(row.GetAttribute("draggable"));
        Assert.Equal("false", item.Find(".omni-tree__item").GetAttribute("aria-selected"));
    }

    [Fact]
    public void LeafOpenedByItsHost_DrawsNoGroup()
    {
        var item = Render<OmniTreeItem<string>>(parameters => parameters
            .Add(component => component.Value, "feuille")
            .Add(component => component.Text, "Feuille")
            .Add(component => component.Expanded, true));

        Assert.Empty(item.FindAll("[role=group]"));
        Assert.Null(item.Find(".omni-tree__item").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void DragGesturesWithNothingDragged_ChangeNothing()
    {
        var host = Render<TreeDragDropTestHost>();

        Row(host, "7 - Produits").DragEnd();
        Row(host, "7 - Produits").DragLeave();
        Row(host, "7 - Produits").Drop();

        Assert.Empty(host.Instance.Drops);
        Assert.Empty(host.FindAll(".omni-tree__row--drop-target"));
    }

    [Fact]
    public void DragEnteringTheSameTargetTwice_KeepsItTheTarget_AndLeavingAnotherRowKeepsIt()
    {
        var host = Render<TreeDragDropTestHost>();
        Row(host, "60 - Achats").DragStart();

        Row(host, "7 - Produits").DragEnter();
        Row(host, "7 - Produits").DragEnter();
        Row(host, "6 - Charges").DragLeave();
        Assert.Contains("omni-tree__row--drop-target", Row(host, "7 - Produits").ClassList);

        Row(host, "7 - Produits").DragLeave();
        Assert.Empty(host.FindAll(".omni-tree__row--drop-target"));
        Row(host, "60 - Achats").DragEnd();
        Assert.DoesNotContain("omni-tree--dragging", host.Find(".omni-tree").ClassList);
    }

    [Fact]
    public void FailingHandlerOfALoadOpenedByItsParameters_ReachesTheRenderer()
    {
        var item = Render<OmniTreeItem<string>>(parameters => parameters
            .Add(component => component.Value, "a")
            .Add(component => component.Text, "A")
            .Add(component => component.LoadChildren, _ => throw new InvalidOperationException("chargement"))
            .Add(component => component.OnLoadError, (Action<Exception>)(_ => throw new ArgumentException("gestionnaire"))));

        var error = Assert.Throws<ArgumentException>(() => item.Render(parameters => parameters.Add(component => component.Expanded, true)));
        Assert.Equal("gestionnaire", error.Message);
    }

    [Fact]
    public async Task ReplacedLoad_FailingAfterwards_IsNoLongerShown()
    {
        var first = new TaskCompletionSource();
        var errors = new List<Exception>();
        var item = Render<OmniTreeItem<string>>(parameters => parameters
            .Add(component => component.Value, "a")
            .Add(component => component.Text, "A")
            .Add(component => component.Expanded, true)
            .Add(component => component.LoadChildren, _ => first.Task)
            .Add(component => component.OnLoadError, exception => errors.Add(exception)));

        // A new loader replaces the running load, which then fails: its failure is not the item's.
        item.Render(parameters => parameters.Add(component => component.LoadChildren, _ => Task.CompletedTask));
        await item.InvokeAsync(() => first.SetException(new InvalidOperationException("trop tard")));

        Assert.Empty(errors);
        Assert.Empty(item.FindAll("[role=alert]"));
    }
}
