using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniMindMap"/> in bUnit. The pointer gestures run in <c>omni-mindmap.js</c>, out of
/// bUnit's reach; what the script reports is driven here through the same
/// <see cref="MindMapInteropBridge"/> the script calls, so the .NET side of every gesture is covered.
/// </summary>
public sealed class MindMapComponentTests : OmniBunitContext
{
    private const string ModulePath = "./_content/OmniEurope.Blazor/omni-mindmap.js";

    // A centre, two children on either side and a grandchild, placed so that the arrow keys have an
    // unambiguous answer: right of the root is "right", left of it is "left", below "right" is "below".
    internal static OmniMindMapDocument Sample() => new()
    {
        RootId = "root",
        Nodes =
        [
            new OmniMindMapNode { Id = "root", Label = "Centre", X = 400, Y = 300, FontSize = 20, Bold = true },
            new OmniMindMapNode { Id = "right", Label = "Droite", Group = OmniMindMapGroups.Blue, X = 650, Y = 300 },
            new OmniMindMapNode { Id = "left", Label = "Gauche", Group = OmniMindMapGroups.Green, X = 150, Y = 300 },
            new OmniMindMapNode { Id = "below", Label = "Dessous", Group = OmniMindMapGroups.Yellow, X = 650, Y = 500 }
        ],
        Edges =
        [
            new OmniMindMapEdge { From = "root", To = "right" },
            new OmniMindMapEdge { From = "root", To = "left" },
            new OmniMindMapEdge { From = "right", To = "below" }
        ],
        Notes = [new OmniMindMapNote { AttachedTo = "right", Text = "Une note" }]
    };

    [Fact]
    public void Canvas_IsANamedApplicationWithAKeyboardHintAndALiveRegion()
    {
        var map = Render<OmniMindMap>(parameters => parameters.Add(component => component.Document, Sample()));

        var canvas = map.Find("svg.omni-mindmap__canvas");
        Assert.Equal("application", canvas.GetAttribute("role"));
        Assert.Equal("Carte mentale", canvas.GetAttribute("aria-label"));
        Assert.Equal("0", canvas.GetAttribute("tabindex"));
        var hint = map.Find($"#{canvas.GetAttribute("aria-describedby")}");
        Assert.Contains("Maj+F10", hint.TextContent, StringComparison.Ordinal);
        var live = map.Find("[role=status]");
        Assert.Equal("polite", live.GetAttribute("aria-live"));
        Assert.DoesNotMatch(MindMapComponentTests.InlineStyle, map.Markup);
    }

    [Fact]
    public void Document_IsDrawnWithGroupClassesAndSvgGeometry()
    {
        var map = Render<OmniMindMap>(parameters => parameters.Add(component => component.Document, Sample()));

        Assert.Equal(4, map.FindAll("[data-omni-node]").Count);
        Assert.Equal(3, map.FindAll("[data-omni-edge]").Count);
        var right = Node(map, "right");
        Assert.Contains("omni-mindmap__node--blue", right.ClassList);
        Assert.Equal("translate(650 300)", right.GetAttribute("transform"));
        Assert.Equal("Droite, note : Une note", right.GetAttribute("aria-label"));
        Assert.Equal("Une note", map.Find(".omni-mindmap__note-text").TextContent);
        var root = Node(map, "root");
        Assert.Equal("20", root.QuerySelector("text")!.GetAttribute("font-size"));
        Assert.Contains("omni-mindmap__node-label--bold", root.QuerySelector("text")!.ClassList);
        Assert.DoesNotContain("omni-mindmap__node-label--italic", root.QuerySelector("text")!.ClassList);
        var edge = map.Find("[data-omni-edge='0'] .omni-mindmap__edge-line");
        Assert.Equal(MindMapGeometry.EdgePath(400, 300, 650, 300), edge.GetAttribute("d"));
    }

    [Fact]
    public void NodesTheScriptCannotAddress_AreKeptInTheDocumentButNotDrawn()
    {
        var document = OmniMindMapDocument.FromJson("""{"rootId":"a","nodes":[{"id":"a","label":"A","x":1,"y":1},{"id":"bad id","label":"B","x":2,"y":2}],"edges":[{"from":"a","to":"bad id"}],"notes":[]}""");
        OmniMindMapDocument? changed = null;
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, document)
            .Add(component => component.DocumentChanged, value => changed = value));

        Assert.Single(map.FindAll("[data-omni-node]"));
        Assert.Empty(map.FindAll("[data-omni-edge]"));

        map.Find("svg").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        map.Find("svg").KeyDown(new KeyboardEventArgs { Key = "ArrowRight", ShiftKey = true });

        Assert.NotNull(changed);
        Assert.Equal(2, changed!.Nodes.Count);
        Assert.Equal(11, changed.Nodes[0].X);
        Assert.Equal(2, changed.Nodes[1].X);
        Assert.Single(changed.Edges);
    }

    [Fact]
    public async Task Module_IsAttachedWithTheBridgeAndDetachedOnDispose()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<MindMapCanvasSize?>("attach", _ => true).SetResult(new MindMapCanvasSize(1000, 700));
        module.SetupVoid("detach", _ => true);

        var map = Render<OmniMindMap>(parameters => parameters.Add(component => component.Document, Sample()));

        var attach = Assert.Single(module.Invocations["attach"]);
        Assert.IsType<DotNetObjectReference<MindMapInteropBridge>>(attach.Arguments[1]);
        Assert.NotEmpty(module.Invocations["measure"]);
        await DisposeComponentsAsync();
        Assert.Single(module.Invocations["detach"]);
    }

    [Fact]
    public void FirstRender_FitsTheMapInTheCanvasAndReportsTheView()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<MindMapCanvasSize?>("attach", _ => true).SetResult(new MindMapCanvasSize(1000, 700));
        OmniMindMapViewState? view = null;

        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, Sample())
            .Add(component => component.ViewStateChanged, value => view = value));

        map.WaitForAssertion(() => Assert.NotNull(view));
        Assert.InRange(view!.Zoom, OmniMindMap.MinZoom, 2);
        Assert.Contains($"scale({MindMapGeometry.Format(view.Zoom)})", map.Find(".omni-mindmap__viewport").GetAttribute("transform"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AutomaticFit_FollowsTheCanvasUntilTheReaderMovesTheView()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<MindMapCanvasSize?>("attach", _ => true).SetResult(new MindMapCanvasSize(300, 150));
        var views = new List<OmniMindMapViewState>();
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, Sample())
            .Add(component => component.ViewStateChanged, value => views.Add(value)));
        var bridge = new MindMapInteropBridge(map.Instance);
        map.WaitForAssertion(() => Assert.NotEmpty(views));
        var cramped = views[^1].Zoom;

        await map.InvokeAsync(() => bridge.Resized(1000, 700));

        Assert.True(views[^1].Zoom > cramped, $"The view kept the zoom {views[^1].Zoom} fitted to a 300 by 150 canvas.");

        await map.InvokeAsync(() => bridge.ViewChanged(5, 5, 1));
        var count = views.Count;
        await map.InvokeAsync(() => bridge.Resized(1200, 800));

        Assert.Equal(count, views.Count);
        Assert.Equal(new OmniMindMapViewState(5, 5, 1), map.Instance.CurrentView);
    }

    [Fact]
    public void GivenViewState_IsAppliedAndNotRefitted()
    {
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, Sample())
            .Add(component => component.ViewState, new OmniMindMapViewState(12, -8, 1.5)));

        var viewport = map.Find(".omni-mindmap__viewport");
        Assert.Equal("translate(12 -8) scale(1.5)", viewport.GetAttribute("transform"));
        Assert.Equal("1.5", viewport.GetAttribute("data-zoom"));
    }

    [Fact]
    public async Task PressingANode_SelectsItAndRaisesNodeSelected()
    {
        OmniMindMapNode? selected = null;
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, Sample())
            .Add(component => component.NodeSelected, value => selected = value));
        var bridge = new MindMapInteropBridge(map.Instance);

        await map.InvokeAsync(() => bridge.NodePressed("right"));

        Assert.Equal("right", selected?.Id);
        Assert.Equal("true", Node(map, "right").GetAttribute("data-omni-selected"));
        Assert.Contains("omni-mindmap__node--selected", Node(map, "right").ClassList);
        Assert.Equal(Node(map, "right").Id, map.Find("svg").GetAttribute("aria-activedescendant"));
        Assert.Equal("Droite, liens : 2.", map.Instance.Announcement);

        await map.InvokeAsync(() => bridge.BackgroundPressed());

        Assert.Null(selected);
        Assert.Null(map.Find("svg").GetAttribute("aria-activedescendant"));
    }

    [Fact]
    public async Task DroppedNodes_AreCommittedRoundedAndUndoable()
    {
        var changes = new List<OmniMindMapDocument>();
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, Sample())
            .Add(component => component.DocumentChanged, value => changes.Add(value)));
        var bridge = new MindMapInteropBridge(map.Instance);

        await map.InvokeAsync(() => bridge.NodesMoved([new MindMapNodeMove("left", 120.4, 280.6), new MindMapNodeMove("ghost", 1, 1)]));

        var moved = Assert.Single(changes);
        Assert.Contains(moved.Nodes, node => node is { Id: "left", X: 120, Y: 281 });
        Assert.Equal("translate(120 281)", Node(map, "left").GetAttribute("transform"));

        map.Find("svg").KeyDown(new KeyboardEventArgs { Key = "z", CtrlKey = true });

        Assert.Equal(2, changes.Count);
        Assert.Contains(changes[^1].Nodes, node => node is { Id: "left", X: 150, Y: 300 });
        Assert.Equal("Modification annulée.", map.Instance.Announcement);

        map.Find("svg").KeyDown(new KeyboardEventArgs { Key = "y", CtrlKey = true });

        Assert.Equal(3, changes.Count);
        Assert.Contains(changes[^1].Nodes, node => node is { Id: "left", X: 120, Y: 281 });
    }

    [Fact]
    public async Task History_IsBoundedToThirtyStates()
    {
        var map = Render<OmniMindMap>(parameters => parameters.Add(component => component.Document, Sample()));
        var bridge = new MindMapInteropBridge(map.Instance);

        for (var step = 1; step <= 40; step++)
        {
            await map.InvokeAsync(() => bridge.NodesMoved([new MindMapNodeMove("left", 150 + step, 300)]));
        }

        var undone = 0;
        while (map.Instance.CanUndo)
        {
            map.Find("svg").KeyDown(new KeyboardEventArgs { Key = "z", CtrlKey = true });
            undone++;
        }

        Assert.Equal(OmniMindMap.MaxHistory - 1, undone);
        Assert.Equal("translate(161 300)", Node(map, "left").GetAttribute("transform"));
    }

    [Fact]
    public void ArrowKeys_WalkToTheNearestNodeInThatDirection_AndHomeReturnsToTheCentre()
    {
        var map = Render<OmniMindMap>(parameters => parameters.Add(component => component.Document, Sample()));
        var canvas = map.Find("svg");

        canvas.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.Equal("root", map.Instance.SelectedNode?.Id);

        canvas.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.Equal("right", map.Instance.SelectedNode?.Id);

        canvas.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Equal("below", map.Instance.SelectedNode?.Id);

        canvas.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Equal("below", map.Instance.SelectedNode?.Id);

        canvas.KeyDown(new KeyboardEventArgs { Key = "Home" });
        Assert.Equal("root", map.Instance.SelectedNode?.Id);

        canvas.KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.Equal("left", map.Instance.SelectedNode?.Id);

        canvas.KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(map.Instance.SelectedNode);
        Assert.Equal("Aucune sélection.", map.Instance.Announcement);
    }

    [Fact]
    public void KeyboardCommands_AddDuplicateMoveAndDelete()
    {
        OmniMindMapDocument current = Sample();
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, current)
            .Add(component => component.DocumentChanged, value => current = value));
        var canvas = map.Find("svg");

        canvas.KeyDown(new KeyboardEventArgs { Key = "n" });
        var added = Assert.Single(current.Nodes, node => node.Id == "node_1");
        Assert.Equal("Nouveau", added.Label);
        Assert.Equal(OmniMindMapGroups.Green, added.Group);
        Assert.Equal("node_1", map.Instance.SelectedNode?.Id);
        Assert.Equal("Nœud Nouveau ajouté.", map.Instance.Announcement);

        canvas.KeyDown(new KeyboardEventArgs { Key = "Home" });
        canvas.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        canvas.KeyDown(new KeyboardEventArgs { Key = "d" });
        var copy = Assert.Single(current.Nodes, node => node.Id == "node_2");
        Assert.Equal(("Droite", 690d, 330d), (copy.Label, copy.X, copy.Y));
        Assert.Contains(current.Edges, edge => edge is { From: "root", To: "node_2" });
        Assert.DoesNotContain(current.Edges, edge => edge is { From: "node_2", To: "below" });

        canvas.KeyDown(new KeyboardEventArgs { Key = "ArrowUp", ShiftKey = true });
        Assert.Equal(320d, Assert.Single(current.Nodes, node => node.Id == "node_2").Y);

        canvas.KeyDown(new KeyboardEventArgs { Key = "Home" });
        canvas.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.Equal("right", map.Instance.SelectedNode?.Id);
        canvas.KeyDown(new KeyboardEventArgs { Key = "Delete" });

        Assert.DoesNotContain(current.Nodes, node => node.Id == "right");
        Assert.DoesNotContain(current.Edges, edge => edge.From == "right" || edge.To == "right");
        Assert.Empty(current.Notes);
        Assert.Equal("Nœuds supprimés : 1.", map.Instance.Announcement);
    }

    [Fact]
    public void ZoomKeys_ZoomAboutTheCanvasCentreWithinBounds()
    {
        var views = new List<OmniMindMapViewState>();
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, Sample())
            .Add(component => component.ViewState, new OmniMindMapViewState(0, 0, 1))
            .Add(component => component.ViewStateChanged, value => views.Add(value)));
        var canvas = map.Find("svg");

        canvas.KeyDown(new KeyboardEventArgs { Key = "+" });

        Assert.Equal(1.2, views[^1].Zoom, 3);
        Assert.Equal(400 - (400 * 1.2), views[^1].PanX, 3);
        Assert.Equal("Zoom 120 %.", map.Instance.Announcement);

        for (var step = 0; step < 40; step++)
        {
            canvas.KeyDown(new KeyboardEventArgs { Key = "-" });
        }

        Assert.Equal(OmniMindMap.MinZoom, views[^1].Zoom, 6);
    }

    [Fact]
    public async Task ViewChangedByTheScript_IsReportedAndClamped()
    {
        OmniMindMapViewState? view = null;
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, Sample())
            .Add(component => component.ViewState, new OmniMindMapViewState(0, 0, 1))
            .Add(component => component.ViewStateChanged, value => view = value));
        var bridge = new MindMapInteropBridge(map.Instance);

        await map.InvokeAsync(() => bridge.ViewChanged(30, 40, 9));

        Assert.Equal(new OmniMindMapViewState(30, 40, OmniMindMap.MaxZoom), view);
        Assert.Equal("translate(30 40) scale(5)", map.Find(".omni-mindmap__viewport").GetAttribute("transform"));
    }

    [Fact]
    public async Task LassoSelectsSeveralNodes_ThenDeleteRemovesThemAll()
    {
        OmniMindMapDocument current = Sample();
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, current)
            .Add(component => component.DocumentChanged, value => current = value));
        var bridge = new MindMapInteropBridge(map.Instance);

        await map.InvokeAsync(() => bridge.LassoSelected(600, 250, 700, 550));

        Assert.Null(map.Instance.SelectedNode);
        Assert.Equal(2, map.FindAll("[data-omni-selected='true']").Count);
        Assert.Equal("Nœuds sélectionnés : 2.", map.Instance.Announcement);

        map.Find("svg").KeyDown(new KeyboardEventArgs { Key = "Delete" });

        Assert.Equal(["root", "left"], current.Nodes.Select(node => node.Id));
        Assert.Equal(["root->left"], current.Edges.Select(edge => $"{edge.From}->{edge.To}"));
    }

    [Fact]
    public async Task SelectedLink_IsDeletedAlone()
    {
        OmniMindMapDocument current = Sample();
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, current)
            .Add(component => component.DocumentChanged, value => current = value));
        var bridge = new MindMapInteropBridge(map.Instance);

        await map.InvokeAsync(() => bridge.EdgePressed(2));

        Assert.Contains("omni-mindmap__edge--selected", map.Find("[data-omni-edge='2']").ClassList);
        map.Find("svg").KeyDown(new KeyboardEventArgs { Key = "Backspace" });

        Assert.Equal(4, current.Nodes.Count);
        Assert.Equal(2, current.Edges.Count);
        Assert.Equal("Lien supprimé.", map.Instance.Announcement);
    }

    [Fact]
    public async Task LinkMode_JoinsTwoPressedNodesOnce()
    {
        OmniMindMapDocument current = Sample();
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, current)
            .Add(component => component.DocumentChanged, value => current = value));
        var bridge = new MindMapInteropBridge(map.Instance);

        await map.InvokeAsync(() => map.Instance.DispatchAsync(() => map.Instance.StartLinkModeAsync()));
        Assert.Equal("true", map.Find("svg").GetAttribute("data-omni-linking"));
        Assert.Contains("Cliquez sur le nœud source", map.Find(".omni-mindmap__banner").TextContent, StringComparison.Ordinal);

        await map.InvokeAsync(() => bridge.NodePressed("left"));
        Assert.Contains("Cliquez sur le nœud cible", map.Find(".omni-mindmap__banner").TextContent, StringComparison.Ordinal);
        Assert.Contains("omni-mindmap__node--link-source", Node(map, "left").ClassList);

        await map.InvokeAsync(() => bridge.NodePressed("below"));

        Assert.Contains(current.Edges, edge => edge is { From: "left", To: "below" });
        Assert.Equal("false", map.Find("svg").GetAttribute("data-omni-linking"));
        Assert.Equal("Lien créé de Gauche vers Dessous.", map.Instance.Announcement);

        var edges = current.Edges.Count;
        await map.InvokeAsync(() => map.Instance.DispatchAsync(() => map.Instance.StartLinkModeAsync("below")));
        await map.InvokeAsync(() => bridge.NodePressed("left"));
        Assert.Equal(edges, current.Edges.Count);
    }

    [Fact]
    public async Task LinkMode_IsCancelledWithEscape()
    {
        var map = Render<OmniMindMap>(parameters => parameters.Add(component => component.Document, Sample()));

        await map.InvokeAsync(() => map.Instance.DispatchAsync(() => map.Instance.StartLinkModeAsync()));
        map.Find("svg").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.False(map.Instance.IsLinking);
        Assert.Empty(map.FindAll(".omni-mindmap__banner"));
        Assert.Equal("Création du lien annulée.", map.Instance.Announcement);
    }

    [Fact]
    public async Task DoubleClicks_RenameANodeOrAddOneWhereTheCanvasWasClicked()
    {
        OmniMindMapNode? renamed = null;
        OmniMindMapDocument current = Sample();
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, current)
            .Add(component => component.DocumentChanged, value => current = value)
            .Add(component => component.NodeRenameRequested, value => renamed = value));
        var bridge = new MindMapInteropBridge(map.Instance);

        await map.InvokeAsync(() => bridge.NodeDoubleClicked("left"));
        Assert.Equal("left", renamed?.Id);

        await map.InvokeAsync(() => bridge.CanvasDoubleClicked(10.4, 20.6));
        var added = Assert.Single(current.Nodes, node => node.Id == "node_1");
        Assert.Equal((10d, 21d), (added.X, added.Y));
        Assert.Equal(OmniMindMapGroups.Rotation[1], added.Group);

        renamed = null;
        map.Find("svg").KeyDown(new KeyboardEventArgs { Key = "F2" });
        Assert.Equal("node_1", renamed?.Id);
    }

    [Fact]
    public async Task NodeContextMenu_OffersTheOriginalActionsAndRecolours()
    {
        OmniMindMapDocument current = Sample();
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, current)
            .Add(component => component.DocumentChanged, value => current = value));
        var bridge = new MindMapInteropBridge(map.Instance);

        await map.InvokeAsync(() => bridge.ContextMenuRequested("left", 100, 100, 150, 300));

        var menu = map.Find("[role=menu]");
        Assert.Equal(
            ["Renommer le nœud", "Dupliquer", "Lien", "Centrer", "Supprimer"],
            menu.QuerySelectorAll("[role=menuitem]").Select(item => item.TextContent));
        Assert.Equal(OmniMindMapGroups.Palette.Count, menu.QuerySelectorAll("[role=menuitemradio]").Length);
        Assert.Equal("true", menu.QuerySelector(".omni-mindmap__swatch--green")!.GetAttribute("aria-checked"));
        Assert.Equal("left", map.Instance.SelectedNode?.Id);

        map.Find(".omni-mindmap__menu .omni-mindmap__swatch--purple").Click();

        Assert.Equal(OmniMindMapGroups.Purple, current.Nodes.Single(node => node.Id == "left").Group);
        Assert.Contains("omni-mindmap__node--purple", Node(map, "left").ClassList);
        Assert.Empty(map.FindAll("[role=menu]"));
    }

    [Fact]
    public async Task BackgroundContextMenu_AddsANodeWhereItWasOpened()
    {
        OmniMindMapDocument current = Sample();
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, current)
            .Add(component => component.DocumentChanged, value => current = value));
        var bridge = new MindMapInteropBridge(map.Instance);

        await map.InvokeAsync(() => bridge.ContextMenuRequested(null, 20, 30, 222, 333));

        Assert.Equal(["Nœud", "Réorganiser", "Ajuster"], map.FindAll("[role=menuitem]").Select(item => item.TextContent));
        map.FindAll("[role=menuitem]")[0].Click();

        Assert.Contains(current.Nodes, node => node is { X: 222, Y: 333, Label: "Nouveau" });
    }

    [Fact]
    public async Task ContextMenu_ClosesWithEscapeOrWhenDismissed()
    {
        var map = Render<OmniMindMap>(parameters => parameters.Add(component => component.Document, Sample()));
        var bridge = new MindMapInteropBridge(map.Instance);

        await map.InvokeAsync(() => bridge.ContextMenuRequested("left", 10, 10, 150, 300));
        map.Find("[role=menu]").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(map.FindAll("[role=menu]"));

        map.Find("svg").KeyDown(new KeyboardEventArgs { Key = "F10", ShiftKey = true });
        Assert.Single(map.FindAll("[role=menu]"));

        await map.InvokeAsync(() => bridge.MenuDismissed());
        Assert.Empty(map.FindAll("[role=menu]"));
    }

    [Fact]
    public async Task ReadOnly_SelectsAndNavigatesButChangesNothing()
    {
        var changes = 0;
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, Sample())
            .Add(component => component.ReadOnly, true)
            .Add(component => component.DocumentChanged, _ => changes++));
        var bridge = new MindMapInteropBridge(map.Instance);
        var canvas = map.Find("svg");

        Assert.Equal("true", canvas.GetAttribute("data-omni-readonly"));
        Assert.Contains("C : centrer", map.Find($"#{canvas.GetAttribute("aria-describedby")}").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("N : nouveau", map.Find($"#{canvas.GetAttribute("aria-describedby")}").TextContent, StringComparison.Ordinal);

        await map.InvokeAsync(() => bridge.NodePressed("left"));
        Assert.Equal("left", map.Instance.SelectedNode?.Id);

        await map.InvokeAsync(() => bridge.NodesMoved([new MindMapNodeMove("left", 1, 1)]));
        await map.InvokeAsync(() => bridge.CanvasDoubleClicked(5, 5));
        canvas.KeyDown(new KeyboardEventArgs { Key = "n" });
        canvas.KeyDown(new KeyboardEventArgs { Key = "d" });
        canvas.KeyDown(new KeyboardEventArgs { Key = "Delete" });
        canvas.KeyDown(new KeyboardEventArgs { Key = "ArrowUp", ShiftKey = true });
        await map.InvokeAsync(() => map.Instance.DispatchAsync(() => map.Instance.StartLinkModeAsync()));

        Assert.Equal(0, changes);
        Assert.False(map.Instance.IsLinking);
        Assert.Equal(4, map.FindAll("[data-omni-node]").Count);

        await map.InvokeAsync(() => bridge.ContextMenuRequested("left", 10, 10, 150, 300));
        Assert.Equal(["Centrer"], map.FindAll("[role=menuitem]").Select(item => item.TextContent));
        Assert.Empty(map.FindAll("[role=menuitemradio]"));
    }

    [Fact]
    public async Task DocumentReplacedFromOutside_KeepsTheSurvivingSelectionAndTheHistory()
    {
        var map = Render<OmniMindMap>(parameters => parameters.Add(component => component.Document, Sample()));
        var bridge = new MindMapInteropBridge(map.Instance);
        await map.InvokeAsync(() => bridge.NodePressed("left"));
        await map.InvokeAsync(() => bridge.NodesMoved([new MindMapNodeMove("left", 100, 100)]));

        var remote = Sample() with { Nodes = [.. Sample().Nodes.Where(node => node.Id != "below")] };
        map.Render(parameters => parameters.Add(component => component.Document, remote));

        Assert.Equal("left", map.Instance.SelectedNode?.Id);
        Assert.Equal(3, map.FindAll("[data-omni-node]").Count);
        Assert.True(map.Instance.CanUndo);
    }

    [Fact]
    public void GraphWithoutPositions_IsLaidOutRadiallyWithoutReportingAChange()
    {
        var changes = 0;
        var document = new OmniMindMapDocument
        {
            RootId = "a",
            Nodes = [new OmniMindMapNode { Id = "a", Label = "A" }, new OmniMindMapNode { Id = "b", Label = "B" }, new OmniMindMapNode { Id = "c", Label = "C" }],
            Edges = [new OmniMindMapEdge { From = "a", To = "b" }, new OmniMindMapEdge { From = "a", To = "c" }]
        };

        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, document)
            .Add(component => component.DocumentChanged, _ => changes++));

        Assert.Equal(0, changes);
        Assert.Equal("translate(400 300)", Node(map, "a").GetAttribute("transform"));
        Assert.NotEqual(Node(map, "b").GetAttribute("transform"), Node(map, "c").GetAttribute("transform"));
    }

    [Fact]
    public void ContextLabels_RenameTheActionsEverywhereTheyAppear()
    {
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, Sample())
            .Add(component => component.AriaLabel, "Plan du projet")
            .Add(component => component.ContextLabels, new OmniMindMapLabels { NewNodeLabel = "Idée", AddNode = "Ajouter une idée" }));

        Assert.Equal("Plan du projet", map.Find("svg").GetAttribute("aria-label"));
        Assert.Equal("Ajouter une idée", map.Instance.AddNodeLabel);
        map.Find("svg").KeyDown(new KeyboardEventArgs { Key = "n" });
        Assert.Equal("Idée", map.Instance.SelectedNode?.Label);
    }

    [Fact]
    public void EnglishCulture_UsesTheEnglishResources()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var map = Render<OmniMindMap>(parameters => parameters.Add(component => component.Document, Sample()));

            Assert.Equal("Mind map", map.Find("svg").GetAttribute("aria-label"));
            map.Find("svg").KeyDown(new KeyboardEventArgs { Key = "n" });
            Assert.Equal("New", map.Instance.SelectedNode?.Label);
            Assert.Equal("Node New added.", map.Instance.Announcement);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Theory]
    [InlineData("fr-FR")]
    [InlineData("en-US")]
    public void EveryMindMapResource_ResolvesInTheSupportedCultures(string cultureName)
    {
        var localizer = Services.GetRequiredService<IStringLocalizer<Resources.AppStrings>>();
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            var keys = localizer.GetAllStrings(includeParentCultures: true)
                .Where(entry => entry.Name.StartsWith("MindMap", StringComparison.Ordinal))
                .ToArray();

            Assert.True(keys.Length >= 50, $"{keys.Length} mind map resources found for {cultureName}.");
            Assert.All(keys, entry => Assert.False(localizer[entry.Name].ResourceNotFound, entry.Name));
            Assert.All(keys, entry => Assert.DoesNotContain('\u2014', entry.Value));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    /// <summary>An inline style attribute, without mistaking an SVG attribute such as font-style for one.</summary>
    internal static readonly System.Text.RegularExpressions.Regex InlineStyle = new(@"(?<![\w-])style\s*=",System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    internal static AngleSharp.Dom.IElement Node(IRenderedComponent<OmniMindMap> map, string id) =>
        map.Find($"[data-omni-node='{id}']");
}
