using AngleSharp.Dom;
using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// OmniTree drag and drop (a node dropped onto another becomes its child) and the whole-tree expand and
/// collapse: the drop is reported, never into the dragged node's own branch, and only where the host agrees.
/// </summary>
public sealed class TreeDragDropTests : OmniBunitContext
{
    private static IElement Row(IRenderedComponent<TreeDragDropTestHost> host, string text) =>
        host.FindAll(".omni-tree__row").Single(row => row.TextContent.Contains(text, StringComparison.Ordinal));

    [Fact]
    public void ADropOntoAnotherItem_ReportsBothValues_WithFeedbackDuringTheDrag()
    {
        var host = Render<TreeDragDropTestHost>();

        Assert.Equal("true", Row(host, "60 - Achats").GetAttribute("draggable"));
        Row(host, "60 - Achats").DragStart();
        Assert.Contains("omni-tree--dragging", host.Find(".omni-tree").ClassList);
        Assert.Contains("omni-tree__row--dragging", Row(host, "60 - Achats").ClassList);

        Row(host, "7 - Produits").DragEnter();
        Assert.Contains("omni-tree__row--drop-target", Row(host, "7 - Produits").ClassList);
        Row(host, "7 - Produits").Drop();

        var drop = Assert.Single(host.Instance.Drops);
        Assert.Equal(new OmniTreeDropEventArgs<string>("60", "7"), drop);
        Assert.DoesNotContain("omni-tree--dragging", host.Find(".omni-tree").ClassList);
        Assert.Empty(host.FindAll(".omni-tree__row--drop-target"));
        Assert.Empty(host.FindAll("[style]"));
    }

    [Fact]
    public void AnItem_CannotLandOnItself_NorInItsOwnBranch()
    {
        var host = Render<TreeDragDropTestHost>();

        Row(host, "6 - Charges").DragStart();
        Row(host, "60 - Achats").DragEnter();
        Assert.Empty(host.FindAll(".omni-tree__row--drop-target"));
        Row(host, "60 - Achats").Drop();
        Row(host, "6 - Charges").DragStart();
        Row(host, "6 - Charges").Drop();

        Assert.Empty(host.Instance.Drops);
    }

    [Fact]
    public async Task TheHost_DecidesWhatMovesAndWhereItLands()
    {
        var host = Render<TreeDragDropTestHost>(parameters => parameters
            .Add(component => component.CanDrag, value => value != "7")
            .Add(component => component.CanDrop, (dragged, target) => target.Length < 2));
        await host.InvokeAsync(() => host.Instance.Tree!.ExpandAllAsync());
        host.WaitForAssertion(() => Assert.Equal(4, host.FindAll(".omni-tree__row").Count));

        Assert.Null(Row(host, "7 - Produits").GetAttribute("draggable"));
        Row(host, "607").DragStart();
        Row(host, "60 - Achats").Drop();
        Assert.Empty(host.Instance.Drops);

        Row(host, "607").DragStart();
        Row(host, "7 - Produits").Drop();
        Assert.Equal(new OmniTreeDropEventArgs<string>("607", "7"), Assert.Single(host.Instance.Drops));
    }

    [Fact]
    public void WithoutAllowDragDrop_NothingIsDraggable_AndNoScriptIsLoaded()
    {
        var host = Render<TreeDragDropTestHost>(parameters => parameters.Add(component => component.AllowDragDrop, false));

        Assert.Empty(host.FindAll("[draggable]"));
        Assert.DoesNotContain(JSInterop.Invocations, invocation => invocation.Identifier == "attachTreeDrag");
    }

    [Fact]
    public void WithAllowDragDrop_TheDragScriptIsAttachedOnce()
    {
        var host = Render<TreeDragDropTestHost>();
        host.Render();

        Assert.Single(JSInterop.Invocations, invocation => invocation.Identifier == "attachTreeDrag");
    }

    [Fact]
    public async Task ExpandAll_OpensEveryBranchAndTheOnesItReveals_CollapseAll_ClosesThem()
    {
        var host = Render<TreeDragDropTestHost>();
        Assert.DoesNotContain(host.FindAll(".omni-tree__row"), row => row.TextContent.Contains("607", StringComparison.Ordinal));

        await host.InvokeAsync(() => host.Instance.Tree!.CollapseAllAsync());
        Assert.False(host.Instance.ChargesOpen);
        Assert.Single(host.FindAll(".omni-tree__row"), row => row.TextContent.Contains("6 - Charges", StringComparison.Ordinal));
        Assert.Equal(2, host.FindAll(".omni-tree__row").Count);

        await host.InvokeAsync(() => host.Instance.Tree!.ExpandAllAsync());
        host.WaitForAssertion(() => Assert.Equal(4, host.FindAll(".omni-tree__row").Count));
        Assert.True(host.Instance.ChargesOpen);
        host.WaitForAssertion(() => Assert.True(host.Instance.AchatsOpen));
    }
}
