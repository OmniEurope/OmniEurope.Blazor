using System.Globalization;
using Bunit;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// What the lot 9 added to <see cref="OmniMindMap"/>: directed links ending on the border of their
/// target under an arrowhead (<see cref="OmniMindMap.Directed"/>), host content inside the boxes
/// (<see cref="OmniMindMap.NodeTemplate"/>) and the layered automatic layout
/// (<see cref="OmniMindMap.LayeredLayout"/>).
/// </summary>
public sealed class MindMapLayeredTests : OmniBunitContext
{
    [Theory]
    [InlineData(300, 0, "M 50 0 C 150 0, 150 0, 250 0")]
    [InlineData(-300, 0, "M -50 0 C -150 0, -150 0, -250 0")]
    [InlineData(0, 200, "M 0 20 C 0 100, 0 100, 0 180")]
    [InlineData(0, -200, "M 0 -20 C 0 -100, 0 -100, 0 -180")]
    public void DirectedPath_LeavesOneBorder_AndStopsOnTheOther(double toX, double toY, string expected) =>
        Assert.Equal(expected, MindMapGeometry.DirectedEdgePath(0, 0, 100, 40, toX, toY, 100, 40));

    [Fact]
    public void BoxOf_TakesAFixedSize_OrTheEstimatedLabelWithPadding()
    {
        Assert.Equal((200d, 60d), MindMapGeometry.BoxOf(new OmniMindMapNode { Id = "a", Label = "A", Width = 200, Height = 60 }));
        var (width, height) = MindMapGeometry.BoxOf(new OmniMindMapNode { Id = "b", Label = "Un libellé assez long pour dépasser" });
        var text = MindMapGeometry.EstimateText("Un libellé assez long pour dépasser", OmniMindMapNode.DefaultFontSize, bold: false);
        Assert.Equal(text.Width + (MindMapGeometry.PaddingX * 2), width);
        Assert.Equal(text.Height + (MindMapGeometry.PaddingY * 2), height);
        Assert.Equal(MindMapGeometry.MinimumAutoWidth, MindMapGeometry.BoxOf(new OmniMindMapNode { Id = "c", Label = "C" }).Width);
    }

    [Fact]
    public void Directed_DrawsEveryLinkToTheBorderOfItsTarget_UnderAnArrowhead()
    {
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Id, "map")
            .Add(component => component.Document, MindMapComponentTests.Sample())
            .Add(component => component.Directed, true));

        var marker = map.Find("defs marker#map-arrow");
        Assert.Equal("auto", marker.GetAttribute("orient"));
        Assert.NotNull(marker.QuerySelector("path.omni-mindmap__arrow-head"));
        var edge = map.Find("[data-omni-edge='0']");
        Assert.Equal("true", edge.GetAttribute("data-omni-directed"));
        var line = edge.QuerySelector(".omni-mindmap__edge-line")!;
        Assert.Equal("url(#map-arrow)", line.GetAttribute("marker-end"));

        var document = MindMapComponentTests.Sample();
        var root = document.Nodes[0];
        var right = document.Nodes[1];
        var (rootWidth, rootHeight) = map.Instance.SizeOf(root);
        var (rightWidth, rightHeight) = map.Instance.SizeOf(right);
        var expected = MindMapGeometry.DirectedEdgePath(root.X, root.Y, rootWidth, rootHeight, right.X, right.Y, rightWidth, rightHeight);
        Assert.Equal(expected, line.GetAttribute("d"));
        Assert.Equal(expected, edge.QuerySelector(".omni-mindmap__edge-hit")!.GetAttribute("d"));
        Assert.DoesNotMatch(MindMapComponentTests.InlineStyle, map.Markup);
    }

    [Fact]
    public void Undirected_KeepsTheCentreToCentreLinks_WithNoArrow()
    {
        var map = Render<OmniMindMap>(parameters => parameters.Add(component => component.Document, MindMapComponentTests.Sample()));

        Assert.Empty(map.FindAll("marker"));
        var edge = map.Find("[data-omni-edge='0']");
        Assert.Null(edge.GetAttribute("data-omni-directed"));
        Assert.Null(edge.QuerySelector(".omni-mindmap__edge-line")!.GetAttribute("marker-end"));
    }

    [Fact]
    public void NodeTemplate_DrawsTheHostContentInsideTheBox_AndTheNodeKeepsItsName()
    {
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, MindMapComponentTests.Sample())
            .Add(component => component.NodeTemplate, node => builder =>
            {
                builder.OpenElement(0, "strong");
                builder.AddContent(1, node.Label.ToUpperInvariant());
                builder.CloseElement();
            }));

        var right = MindMapComponentTests.Node(map, "right");
        var content = right.QuerySelector("foreignObject.omni-mindmap__node-content")!;
        var (width, height) = map.Instance.SizeOf(MindMapComponentTests.Sample().Nodes[1]);
        Assert.Equal(MindMapGeometry.Format(width), content.GetAttribute("width"));
        Assert.Equal(MindMapGeometry.Format(height), content.GetAttribute("height"));
        Assert.Equal(MindMapGeometry.Format(-width / 2), content.GetAttribute("x"));
        Assert.Equal("DROITE", content.QuerySelector(".omni-mindmap__node-template strong")!.TextContent);
        Assert.Null(right.QuerySelector("text.omni-mindmap__node-label"));
        Assert.Equal("Droite, note : Une note", right.GetAttribute("aria-label"));
        Assert.DoesNotMatch(MindMapComponentTests.InlineStyle, map.Markup);
    }

    [Fact]
    public void LayeredLayout_PlacesADocumentWithoutPositions_InLayers()
    {
        var document = new OmniMindMapDocument
        {
            RootId = "root",
            Nodes =
            [
                new OmniMindMapNode { Id = "root", Label = "Racine" },
                new OmniMindMapNode { Id = "a", Label = "A" },
                new OmniMindMapNode { Id = "b", Label = "B" }
            ],
            Edges = [new OmniMindMapEdge { From = "root", To = "a" }, new OmniMindMapEdge { From = "root", To = "b" }]
        };

        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, document)
            .Add(component => component.LayeredLayout, new OmniGraphLayoutOptions()));

        var root = PositionOf(map, "root");
        var a = PositionOf(map, "a");
        var b = PositionOf(map, "b");
        Assert.True(root.X < a.X);
        Assert.Equal(a.X, b.X);
        Assert.NotEqual(a.Y, b.Y);
        Assert.InRange(root.Y, ((a.Y + b.Y) / 2) - 1, ((a.Y + b.Y) / 2) + 1);
    }

    [Fact]
    public async Task LayeredLayout_IsWhatTheAutoLayoutActionApplies_InTheDirectionAsked()
    {
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, MindMapComponentTests.Sample())
            .Add(component => component.LayeredLayout, new OmniGraphLayoutOptions { Direction = OmniGraphDirection.TopToBottom }));
        Assert.Equal((400d, 300d), PositionOf(map, "root"));

        await map.Instance.DispatchAsync(map.Instance.AutoLayoutAsync);

        var root = PositionOf(map, "root");
        var right = PositionOf(map, "right");
        var left = PositionOf(map, "left");
        var below = PositionOf(map, "below");
        Assert.True(root.Y < right.Y, $"root {root}, right {right}, left {left}, below {below}");
        Assert.Equal(right.Y, left.Y);
        Assert.True(right.Y < below.Y);
        Assert.Equal(right.X, below.X);
    }

    private static (double X, double Y) PositionOf(IRenderedComponent<OmniMindMap> map, string id)
    {
        var transform = MindMapComponentTests.Node(map, id).GetAttribute("transform")!;
        var parts = transform["translate(".Length..^1].Split(' ');
        return (double.Parse(parts[0], CultureInfo.InvariantCulture), double.Parse(parts[1], CultureInfo.InvariantCulture));
    }
}
