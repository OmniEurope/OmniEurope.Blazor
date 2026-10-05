using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// OmniMindMap driven the way its script drives it, through the bridge: every entry of the context menu
/// on a node and on the background, a menu locked or opened among several selected nodes, its keys,
/// gestures on unknown nodes and edges or a view that is not a number, links refused, history and
/// layout locked, and the JSON a host writes by hand.
/// </summary>
public sealed class MindMapGesturesTests : OmniBunitContext
{
    private readonly List<OmniMindMapDocument> _changes = [];
    private readonly List<OmniMindMapNode?> _selections = [];

    private (IRenderedComponent<OmniMindMap> Map, MindMapInteropBridge Bridge) RenderMap(OmniMindMapDocument? document = null, bool readOnly = false)
    {
        var module = JSInterop.SetupModule(OmniModules.MindMap);
        module.Mode = JSRuntimeMode.Loose;
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, document ?? MindMapComponentTests.Sample())
            .Add(component => component.ReadOnly, readOnly)
            .Add(component => component.DocumentChanged, value => _changes.Add(value))
            .Add(component => component.OnNodeSelect, node => _selections.Add(node)));
        var bridge = ((DotNetObjectReference<MindMapInteropBridge>)module.Invocations["attach"][0].Arguments[1]!).Value;
        return (map, bridge);
    }

    private static IReadOnlyList<AngleSharp.Dom.IElement> MenuItems(IRenderedComponent<OmniMindMap> map) =>
        map.FindAll(".omni-mindmap__menu-item");

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task NodeMenuEntries_EachCloseTheMenu(int entry)
    {
        var (map, bridge) = RenderMap();
        await map.InvokeAsync(() => bridge.ContextMenuRequested("right", 10, 10, 0, 0));
        Assert.Equal(5, MenuItems(map).Count);

        MenuItems(map)[entry].Click();

        Assert.Empty(map.FindAll(".omni-mindmap__menu"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task BackgroundMenuEntries_AddArrangeOrFit(int entry)
    {
        var (map, bridge) = RenderMap();
        await map.InvokeAsync(() => bridge.ContextMenuRequested(null, 10, 10, 30, 40));

        MenuItems(map)[entry].Click();

        Assert.Empty(map.FindAll(".omni-mindmap__menu"));
        if (entry == 0)
        {
            Assert.Equal(5, Assert.Single(_changes).Nodes.Count);
        }
    }

    [Fact]
    public async Task LockedBackgroundMenu_OffersOnlyTheFit_AndTheMenuKeys()
    {
        var (map, bridge) = RenderMap(readOnly: true);
        await map.InvokeAsync(() => bridge.ContextMenuRequested(null, 10, 10, 0, 0));
        Assert.Single(MenuItems(map));

        map.Find(".omni-mindmap__menu").KeyDown(new KeyboardEventArgs { Key = "a" });
        Assert.NotEmpty(map.FindAll(".omni-mindmap__menu"));
        map.Find(".omni-mindmap__menu").KeyDown(new KeyboardEventArgs { Key = "Tab" });
        Assert.Empty(map.FindAll(".omni-mindmap__menu"));

        await map.InvokeAsync(() => bridge.ContextMenuRequested("right", 10, 10, 0, 0));
        map.Find(".omni-mindmap__menu").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(map.FindAll(".omni-mindmap__menu"));
    }

    [Fact]
    public async Task MenuFromTheKeyboardWithoutSelection_OpensAtTheCentre_AndOnOneOfSeveralNodesKeepsThemAll()
    {
        var (map, bridge) = RenderMap();
        map.Find("svg.omni-mindmap__canvas").KeyDown(new KeyboardEventArgs { Key = "ContextMenu" });
        Assert.NotEmpty(map.FindAll(".omni-mindmap__menu"));
        await map.InvokeAsync(bridge.MenuDismissed);

        await map.InvokeAsync(() => bridge.LassoSelected(-10_000, -10_000, 10_000, 10_000));
        await map.InvokeAsync(() => bridge.ContextMenuRequested("left", 10, 10, 0, 0));
        Assert.Equal(4, map.FindAll(".omni-mindmap__node--selected").Count);

        map.Find(".omni-mindmap__swatch--red").Click();
        Assert.Equal(OmniMindMapGroups.Red, _changes[^1].Nodes.Single(node => node.Id == "left").Group);
    }

    [Fact]
    public async Task Gestures_OnUnknownNodesOrEdges_OrAViewThatIsNotANumber_ChangeNothing()
    {
        var (map, bridge) = RenderMap();
        var markup = map.Markup;

        await map.InvokeAsync(() => bridge.NodePressed("inconnu"));
        await map.InvokeAsync(() => bridge.NodeDoubleClicked("inconnu"));
        await map.InvokeAsync(() => bridge.EdgePressed(99));
        await map.InvokeAsync(() => bridge.ViewChanged(double.NaN, 0, 1));
        await map.InvokeAsync(() => bridge.ViewChanged(0, double.PositiveInfinity, 1));
        await map.InvokeAsync(() => bridge.ViewChanged(0, 0, double.NaN));

        Assert.Empty(_changes);
        Assert.Equal(markup, map.Markup);
    }

    [Fact]
    public async Task Lasso_OfOneNode_SelectsIt_AndTheBackgroundDropsTheSelection()
    {
        var (map, bridge) = RenderMap();
        await map.InvokeAsync(() => bridge.LassoSelected(600, 250, 700, 350));
        Assert.Equal("right", _selections[^1]?.Id);

        await map.InvokeAsync(bridge.BackgroundPressed);
        Assert.Null(_selections[^1]);
        await map.InvokeAsync(() => bridge.LassoSelected(600, 250, 700, 350));
        await map.InvokeAsync(() => bridge.LassoSelected(-5, -5, -4, -4));
        Assert.Null(_selections[^1]);
    }

    [Fact]
    public async Task Links_ToItselfOrWithoutLinkMode_AreRefused_AndAnUnknownSourceStartsNone()
    {
        var (map, bridge) = RenderMap();
        await map.InvokeAsync(() => map.Instance.StartLinkModeAsync("inconnu"));
        await map.InvokeAsync(() => bridge.NodePressed("right"));
        await map.InvokeAsync(() => map.Instance.StartLinkModeAsync("right"));
        await map.InvokeAsync(() => bridge.NodePressed("right"));
        await map.InvokeAsync(() => bridge.EdgePressed(0));

        Assert.Empty(_changes);
    }

    [Fact]
    public async Task LockedMap_RefusesLayoutUndoAndRedo_AndAnEmptyMapHasNothingToArrange()
    {
        var (locked, _) = RenderMap(readOnly: true);
        await locked.InvokeAsync(locked.Instance.AutoLayoutAsync);
        locked.Find("svg.omni-mindmap__canvas").KeyDown(new KeyboardEventArgs { Key = "z", CtrlKey = true });
        locked.Find("svg.omni-mindmap__canvas").KeyDown(new KeyboardEventArgs { Key = "y", CtrlKey = true });

        var (empty, _) = RenderMap(OmniMindMapDocument.Empty);
        await empty.InvokeAsync(empty.Instance.AutoLayoutAsync);
        await empty.InvokeAsync(empty.Instance.DeleteSelectionAsync);
        Assert.False(await empty.InvokeAsync(empty.Instance.FitViewAsync));

        Assert.Empty(_changes);
    }

    [Fact]
    public async Task MapAtItsNodeLimit_RefusesANewNode()
    {
        var nodes = Enumerable.Range(0, OmniMindMap.MaxNodes).Select(index => new OmniMindMapNode { Id = $"n{index}", Label = "n", X = index, Y = 0 }).ToArray();
        var (map, _) = RenderMap(new OmniMindMapDocument { RootId = "n0", Nodes = nodes });

        await map.InvokeAsync(map.Instance.AddNodeAsync);

        Assert.Empty(_changes);
    }

    [Fact]
    public void Json_WrittenByHand_IsReadWithItsDefaults()
    {
        var document = OmniMindMapDocument.FromJson("""
            {"rootId":"missing","nodes":[{"id":"a","fontSize":12.6,"group":""},{"label":"sans id"},3,{"id":"b","fontSize":"grand"}],
             "edges":[{"from":"a","to":"zz"},{"from":"a"}],"notes":[{"attachedTo":"zz","text":"perdue"},{"text":"sans cible"}]}
            """);

        Assert.Equal(["a", "b"], document.Nodes.Select(node => node.Id));
        Assert.Equal(13, document.Nodes[0].FontSize);
        Assert.Equal(OmniMindMapGroups.Root, document.Nodes[0].Group);
        Assert.Equal(OmniMindMapNode.DefaultFontSize, document.Nodes[1].FontSize);
        Assert.Single(document.Edges);
        Assert.Empty(OmniMindMapDocument.FromJson("""{"nodes":"pas une liste"}""").Nodes);

        var (map, _) = RenderMap(document);
        Assert.Equal(2, map.FindAll(".omni-mindmap__node").Count);
    }
}
