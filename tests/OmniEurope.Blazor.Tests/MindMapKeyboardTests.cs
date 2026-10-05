using Bunit;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The keyboard of the mind map canvas, key by key: history, modifiers that cancel a command, zoom and
/// fit, the context menu, Enter while a link is drawn, Escape in each state, and the letter commands.
/// </summary>
public sealed class MindMapKeyboardTests : OmniBunitContext
{
    private readonly List<OmniMindMapDocument> _changes = [];
    private readonly List<OmniMindMapViewState> _views = [];

    private IRenderedComponent<OmniMindMap> RenderMap(OmniMindMapDocument? document = null) =>
        Render<OmniMindMap>(parameters => parameters
            .Add(map => map.Document, document ?? MindMapComponentTests.Sample())
            .Add(map => map.DocumentChanged, value => _changes.Add(value))
            .Add(map => map.ViewStateChanged, value => _views.Add(value)));

    private static void Press(IRenderedComponent<OmniMindMap> map, string key, bool shift = false, bool ctrl = false, bool alt = false, bool meta = false) =>
        map.Find("svg.omni-mindmap__canvas").KeyDown(new KeyboardEventArgs { Key = key, ShiftKey = shift, CtrlKey = ctrl, AltKey = alt, MetaKey = meta });

    [Fact]
    public void AltWithAnyKey_DoesNothing()
    {
        var map = RenderMap();

        Press(map, "n", alt: true);
        Press(map, "z", ctrl: true, alt: true);

        Assert.Empty(_changes);
    }

    [Fact]
    public void History_UndoesWithCtrlOrCmdZ_AndRedoesWithY_OrShiftZ()
    {
        var map = RenderMap();
        Press(map, "Home");
        Press(map, "n");
        Assert.Single(_changes);

        Press(map, "z", meta: true);
        Assert.Equal(2, _changes.Count);
        Press(map, "Z", ctrl: true, shift: true);
        Assert.Equal(3, _changes.Count);
        Press(map, "z", ctrl: true);
        Press(map, "y", ctrl: true);
        Assert.Equal(5, _changes.Count);

        // Another letter with Ctrl is no command of the canvas.
        Press(map, "a", ctrl: true);
        Assert.Equal(5, _changes.Count);
    }

    [Fact]
    public void ZoomKeys_ChangeTheView_AndZeroFitsIt()
    {
        var map = RenderMap();

        Press(map, "+");
        Press(map, "=");
        Assert.True(_views[^1].Zoom > _views[0].Zoom);
        Press(map, "-");
        Press(map, "_");
        var zoomed = _views.Count;

        Press(map, "0");

        Assert.True(_views.Count > zoomed);
    }

    [Theory]
    [InlineData("ContextMenu", false)]
    [InlineData("F10", true)]
    public void MenuKeys_OpenTheContextMenu(string key, bool shift)
    {
        var map = RenderMap();
        Press(map, "Home");

        Press(map, key, shift: shift);

        Assert.Single(map.FindAll("[role=menu]"));
    }

    [Fact]
    public void F10WithoutShift_OpensNothing()
    {
        var map = RenderMap();
        Press(map, "Home");

        Press(map, "F10");

        Assert.Empty(map.FindAll("[role=menu]"));
    }

    [Fact]
    public async Task Escape_ClosesTheMenu_ThenCancelsTheLink_ThenClearsTheSelection_ThenDoesNothing()
    {
        var map = RenderMap();
        Press(map, "Home");
        Press(map, "ContextMenu");

        Press(map, "Escape");
        Assert.Empty(map.FindAll("[role=menu]"));
        Assert.Equal("root", map.Instance.SelectedNode?.Id);

        await map.InvokeAsync(() => map.Instance.StartLinkModeAsync());
        Press(map, "Escape");
        Assert.Equal("root", map.Instance.SelectedNode?.Id);

        Press(map, "Escape");
        Assert.Null(map.Instance.SelectedNode);

        Press(map, "Escape");
        Assert.Null(map.Instance.SelectedNode);
        Assert.Empty(_changes);
    }

    [Fact]
    public async Task EnterWhileLinking_EndsTheLinkOnTheSelectedNode()
    {
        var map = RenderMap();
        await map.InvokeAsync(() => map.Instance.StartLinkModeAsync("left"));
        Press(map, "ArrowRight");
        Press(map, "ArrowRight");
        Assert.Equal("right", map.Instance.SelectedNode?.Id);

        Press(map, "Enter");

        var linked = Assert.Single(_changes);
        Assert.Contains(linked.Edges, edge => edge is { From: "left", To: "right" });
    }

    [Fact]
    public async Task EnterWhileLinking_WithNothingSelected_DoesNothing()
    {
        var renamed = new List<string>();
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(map => map.Document, MindMapComponentTests.Sample())
            .Add(map => map.DocumentChanged, value => _changes.Add(value))
            .Add(map => map.OnNodeRename, node => renamed.Add(node.Id)));
        await map.InvokeAsync(() => map.Instance.StartLinkModeAsync());

        Press(map, "Enter");

        Assert.Empty(_changes);
        Assert.Empty(renamed);
    }

    [Fact]
    public void EnterOrF2_WithoutALink_RenamesTheSelectedNode()
    {
        var renamed = new List<string>();
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(map => map.Document, MindMapComponentTests.Sample())
            .Add(map => map.OnNodeRename, node => renamed.Add(node.Id)));
        Press(map, "Home");

        Press(map, "F2");
        Press(map, "Enter");

        Assert.Equal(["root", "root"], renamed);
    }

    [Fact]
    public void Letters_AddDuplicateAndCentre_AndOtherKeysDoNothing()
    {
        var map = RenderMap();
        Press(map, "Home");

        Press(map, "N");
        Assert.Equal(5, _changes[^1].Nodes.Count);
        Press(map, "d");
        Assert.Equal(6, _changes[^1].Nodes.Count);
        var views = _views.Count;
        Press(map, "c");
        Assert.True(_views.Count > views);

        var changes = _changes.Count;
        Press(map, "q");
        Press(map, "Tab");
        Assert.Equal(changes, _changes.Count);
    }

    [Fact]
    public void ArrowWithoutSelection_TakesTheRoot_OrTheFirstNodeWithoutRoot_OrNothingInAnEmptyMap()
    {
        // Without a root, the first drawn node stands for it.
        var rootless = MindMapComponentTests.Sample() with { RootId = null, Nodes = [.. MindMapComponentTests.Sample().Nodes.Reverse()] };
        var map = RenderMap(rootless);

        Press(map, "ArrowUp");
        Assert.Equal("below", map.Instance.SelectedNode?.Id);

        var empty = RenderMap(new OmniMindMapDocument());
        Press(empty, "ArrowDown");
        Press(empty, "Home");
        Assert.Null(empty.Instance.SelectedNode);
    }
}
