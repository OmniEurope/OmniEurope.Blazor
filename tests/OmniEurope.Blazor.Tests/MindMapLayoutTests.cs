using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;
using Box = OmniEurope.Blazor.Internal.MindMapLayout.Box;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The automatic layout of the mind map: two overlapping boxes pushed apart along the axis where they
/// overlap least, in the direction of the second, and the radial placement around the root.
/// </summary>
public sealed class MindMapLayoutTests
{
    [Theory]
    // Narrow and tall: 60 of overlap across against 180 up and down, so they part across.
    [InlineData(0, 0, 100, 0, 1)]
    [InlineData(100, 0, 0, 0, -1)]
    public void Boxes_OverlappingLessAcross_ArePushedAcross(double ax, double ay, double bx, double by, int direction)
    {
        var a = new Box("a", ax, ay, 140, 200);
        var b = new Box("b", bx, by, 140, 200);

        Assert.True(MindMapLayout.Separate(a, b));

        // Overlap 60: each goes half of it plus one unit away from the other, along X only.
        Assert.Equal(ax - (31 * direction), a.X);
        Assert.Equal(bx + (31 * direction), b.X);
        Assert.Equal((ay, by), (a.Y, b.Y));
    }

    [Theory]
    [InlineData(0, 0, 0, 100, 1)]
    [InlineData(0, 100, 0, 0, -1)]
    public void Boxes_OverlappingLessUpAndDown_ArePushedUpAndDown(double ax, double ay, double bx, double by, int direction)
    {
        var a = new Box("a", ax, ay, 200, 140);
        var b = new Box("b", bx, by, 200, 140);

        Assert.True(MindMapLayout.Separate(a, b));

        Assert.Equal(ay - (31 * direction), a.Y);
        Assert.Equal(by + (31 * direction), b.Y);
        Assert.Equal((ax, bx), (a.X, b.X));
    }

    [Theory]
    [InlineData(500, 0)]
    [InlineData(0, 500)]
    public void Boxes_ApartOnEitherAxis_AreLeftAlone(double bx, double by)
    {
        var a = new Box("a", 0, 0, 100, 100);
        var b = new Box("b", bx, by, 100, 100);

        Assert.False(MindMapLayout.Separate(a, b));
        Assert.Equal((bx, by), (b.X, b.Y));
    }

    [Fact]
    public void Radial_PlacesTheRootInTheMiddle_AndLinesUpUnreachableNodesAlongTheBottom()
    {
        OmniMindMapNode Node(string id) => new() { Id = id, Label = id };
        var positions = MindMapLayout.Radial(
            [Node("root"), Node("child"), Node("lost"), Node("alone")],
            [new OmniMindMapEdge { From = "root", To = "child" }, new OmniMindMapEdge { From = "root", To = "ghost" }, new OmniMindMapEdge { From = "ghost", To = "root" }],
            "root", 1000, 800, _ => (10, 10));

        Assert.Equal((500d, 400d), positions["root"]);
        Assert.Equal((50d, 720d), positions["lost"]);
        Assert.Equal((210d, 720d), positions["alone"]);
        Assert.NotEqual(positions["root"], positions["child"]);
    }

    [Fact]
    public void Radial_WithoutNodes_PlacesNothing_AndAnUnknownRootFallsBackToTheFirstNode()
    {
        Assert.Empty(MindMapLayout.Radial([], [], "root", 0, 0, _ => (10, 10)));

        var positions = MindMapLayout.Radial([new OmniMindMapNode { Id = "a" }], [], "missing", 0, 0, _ => (10, 10));
        // Below the smallest canvas, the layout keeps 800 by 600.
        Assert.Equal((400d, 300d), positions["a"]);
    }

    [Fact]
    public void BoxesTooBigToPart_StopAfterTheIterationBudget()
    {
        // Three hundred wide boxes lined up 160 apart push each other along the line for longer than
        // the budget: the layout stops there and still returns every position.
        var nodes = Enumerable.Range(0, 300).Select(index => new OmniMindMapNode { Id = $"n{index}" }).ToArray();

        var positions = MindMapLayout.Radial(nodes, [], "n0", 800, 600, _ => (340, 100));

        Assert.Equal(300, positions.Count);
    }
}
