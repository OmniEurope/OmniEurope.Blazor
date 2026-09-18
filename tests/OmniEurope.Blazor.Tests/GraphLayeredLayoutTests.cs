using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniGraphLayout.Layered(IReadOnlyList{OmniGraphLayoutNode}, IReadOnlyList{OmniGraphLayoutEdge}, OmniGraphLayoutOptions?)"/>
/// and the internal <see cref="GraphLayeredLayout"/> behind it: exact positions on small graphs worked
/// out by hand, and the properties every drawing keeps on a larger one.
/// </summary>
public sealed class GraphLayeredLayoutTests
{
    private static OmniGraphLayoutNode Box(string id) => new(id, 100, 40);

    [Fact]
    public void EmptyGraph_HasNoPositionAndNoSize()
    {
        var result = OmniGraphLayout.Layered([], []);

        Assert.Empty(result.Positions);
        Assert.Equal((0d, 0d), (result.Width, result.Height));
    }

    [Fact]
    public void Chain_LeftToRight_PutsOneLayerPerNode_ALayerGapApart()
    {
        var result = OmniGraphLayout.Layered([Box("a"), Box("b"), Box("c")], [new("a", "b"), new("b", "c")]);

        Assert.Equal(new OmniGraphPoint(50, 20), result.Positions["a"]);
        Assert.Equal(new OmniGraphPoint(230, 20), result.Positions["b"]);
        Assert.Equal(new OmniGraphPoint(410, 20), result.Positions["c"]);
        Assert.Equal((460d, 40d), (result.Width, result.Height));
    }

    [Fact]
    public void Chain_TopToBottom_TurnsTheSameDrawing()
    {
        var result = OmniGraphLayout.Layered(
            [Box("a"), Box("b"), Box("c")],
            [new("a", "b"), new("b", "c")],
            new OmniGraphLayoutOptions { Direction = OmniGraphDirection.TopToBottom });

        Assert.Equal(new OmniGraphPoint(50, 20), result.Positions["a"]);
        Assert.Equal(new OmniGraphPoint(50, 140), result.Positions["b"]);
        Assert.Equal(new OmniGraphPoint(50, 260), result.Positions["c"]);
        Assert.Equal((100d, 280d), (result.Width, result.Height));
    }

    [Theory]
    [InlineData(OmniGraphDirection.LeftToRight)]
    [InlineData(OmniGraphDirection.TopToBottom)]
    public void EveryDirection_KeepsEachEdgePointingForward(OmniGraphDirection direction)
    {
        var result = OmniGraphLayout.Layered(
            [Box("a"), Box("b"), Box("c")],
            [new("a", "b"), new("a", "c")],
            new OmniGraphLayoutOptions { Direction = direction });

        double Along(string id) => direction == OmniGraphDirection.LeftToRight ? result.Positions[id].X : result.Positions[id].Y;
        double Across(string id) => direction == OmniGraphDirection.LeftToRight ? result.Positions[id].Y : result.Positions[id].X;
        Assert.True(Along("a") < Along("b"));
        Assert.Equal(Along("b"), Along("c"));
        Assert.NotEqual(Across("b"), Across("c"));
    }

    [Fact]
    public void Diamond_CentresTheSourceAndTheSinkBetweenTheTwoBranches()
    {
        var result = OmniGraphLayout.Layered(
            [Box("a"), Box("b"), Box("c"), Box("d")],
            [new("a", "b"), new("a", "c"), new("b", "d"), new("c", "d")]);

        Assert.Equal(new OmniGraphPoint(50, 60), result.Positions["a"]);
        Assert.Equal(new OmniGraphPoint(230, 20), result.Positions["b"]);
        Assert.Equal(new OmniGraphPoint(230, 100), result.Positions["c"]);
        Assert.Equal(new OmniGraphPoint(410, 60), result.Positions["d"]);
        Assert.Equal((460d, 120d), (result.Width, result.Height));
    }

    [Fact]
    public void Cycle_IsBrokenByDrawingOneEdgeBackwards()
    {
        var result = OmniGraphLayout.Layered([Box("a"), Box("b"), Box("c")], [new("a", "b"), new("b", "c"), new("c", "a")]);

        Assert.True(result.Positions["a"].X < result.Positions["b"].X);
        Assert.True(result.Positions["b"].X < result.Positions["c"].X);
    }

    [Fact]
    public void CrossingReduction_UntanglesTwoEdgesGivenCrossed()
    {
        // In input order the second layer is c, d: a->d and b->c would cross.
        var result = OmniGraphLayout.Layered([Box("a"), Box("b"), Box("c"), Box("d")], [new("a", "d"), new("b", "c")]);

        var p = result.Positions;
        Assert.Equal(p["a"].Y < p["b"].Y, p["d"].Y < p["c"].Y);
    }

    [Fact]
    public void CountCrossings_CountsTheInversionsBetweenAdjacentLayers()
    {
        List<int>[] layers = [[0, 1], [2, 3]];

        Assert.Equal(1L, GraphLayeredLayout.CountCrossings(layers, [0, 1, 0, 1], [[3], [2], [], []]));
        Assert.Equal(0L, GraphLayeredLayout.CountCrossings(layers, [0, 1, 0, 1], [[2], [3], [], []]));
        Assert.Equal(1L, GraphLayeredLayout.CountCrossings(layers, [0, 1, 0, 1], [[2, 3], [2], [], []]));
    }

    [Fact]
    public void InvalidInput_IsIgnored_RepeatedNodesAndEdges_LoopsUnknownNodesAndBadSizes()
    {
        var result = OmniGraphLayout.Layered(
            [new("a", double.NaN, -5), new("a", 100, 100), new("b", 10, 10), null!],
            [new("a", "a"), new("a", "zzz"), new("a", "b"), new("a", "b"), null!]);

        Assert.Equal(["a", "b"], result.Positions.Keys.Order());
        Assert.Equal(new OmniGraphPoint(0, 5), result.Positions["a"]);
        Assert.Equal(new OmniGraphPoint(85, 5), result.Positions["b"]);
        Assert.Equal((90d, 10d), (result.Width, result.Height));
    }

    [Fact]
    public void BadSpacing_CountsAsNone()
    {
        var result = OmniGraphLayout.Layered(
            [new("a", 10, 10), new("b", 10, 10)],
            [new("a", "b")],
            new OmniGraphLayoutOptions { NodeSpacing = -10, LayerSpacing = double.NaN });

        Assert.Equal(new OmniGraphPoint(5, 5), result.Positions["a"]);
        Assert.Equal(new OmniGraphPoint(15, 5), result.Positions["b"]);
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => OmniGraphLayout.Layered(null!, []));
        Assert.Throws<ArgumentNullException>(() => OmniGraphLayout.Layered([], null!));
        Assert.Throws<ArgumentNullException>(() => OmniGraphLayout.Layered((OmniMindMapDocument)null!));
    }

    [Fact]
    public void LargerGraph_KeepsEveryEdgeForward_NoOverlapInALayer_AndTheSameDrawingEveryTime()
    {
        var nodes = Enumerable.Range(0, 30).Select(index => new OmniGraphLayoutNode($"n{index}", 60 + (index % 4 * 10), 30 + (index % 3 * 8))).ToList();
        var edges = new List<OmniGraphLayoutEdge>();
        for (var from = 0; from < 30; from++)
        {
            for (var to = from + 1; to < 30; to++)
            {
                if (((from * 7) + (to * 3)) % 11 == 0)
                {
                    edges.Add(new($"n{from}", $"n{to}"));
                }
            }
        }

        var options = new OmniGraphLayoutOptions();
        var result = OmniGraphLayout.Layered(nodes, edges, options);
        var again = OmniGraphLayout.Layered(nodes, edges, options);

        Assert.Equal(30, result.Positions.Count);
        Assert.Equal(result.Positions.OrderBy(entry => entry.Key), again.Positions.OrderBy(entry => entry.Key));
        Assert.All(edges, edge => Assert.True(result.Positions[edge.From].X < result.Positions[edge.To].X, $"{edge.From} -> {edge.To}"));

        foreach (var layer in nodes.GroupBy(node => result.Positions[node.Id].X))
        {
            var ordered = layer.OrderBy(node => result.Positions[node.Id].Y).ToList();
            for (var index = 1; index < ordered.Count; index++)
            {
                var gap = result.Positions[ordered[index].Id].Y - result.Positions[ordered[index - 1].Id].Y;
                var needed = ((ordered[index].Height + ordered[index - 1].Height) / 2) + options.NodeSpacing;
                Assert.True(gap >= needed - 1e-9, $"{ordered[index - 1].Id} and {ordered[index].Id} are {gap} apart, {needed} needed.");
            }
        }

        Assert.All(result.Positions, entry =>
        {
            var node = nodes.Single(candidate => candidate.Id == entry.Key);
            Assert.InRange(entry.Value.Y - (node.Height / 2), -1e-9, result.Height + 1e-9);
            Assert.InRange(entry.Value.X - (node.Width / 2), -1e-9, result.Width + 1e-9);
        });
    }

    [Fact]
    public void MindMapDocument_IsLaidOutFromItsBoxes_PositionsRoundedAndNothingElseChanged()
    {
        var document = new OmniMindMapDocument
        {
            RootId = "root",
            Nodes =
            [
                new OmniMindMapNode { Id = "root", Label = "Racine", X = 7, Y = 9, Width = 200, Height = 60 },
                new OmniMindMapNode { Id = "child", Label = "Enfant" }
            ],
            Edges = [new OmniMindMapEdge { From = "root", To = "child" }],
            Notes = [new OmniMindMapNote { AttachedTo = "child", Text = "Note" }]
        };

        var laidOut = OmniGraphLayout.Layered(document, new OmniGraphLayoutOptions { Direction = OmniGraphDirection.TopToBottom });

        var childBox = MindMapGeometry.BoxOf(document.Nodes[1]);
        var expected = OmniGraphLayout.Layered(
            [new("root", 200, 60), new("child", childBox.Width, childBox.Height)],
            [new("root", "child")],
            new OmniGraphLayoutOptions { Direction = OmniGraphDirection.TopToBottom });
        Assert.Equal((Math.Round(expected.Positions["root"].X), Math.Round(expected.Positions["root"].Y)), (laidOut.Nodes[0].X, laidOut.Nodes[0].Y));
        Assert.Equal((Math.Round(expected.Positions["child"].X), Math.Round(expected.Positions["child"].Y)), (laidOut.Nodes[1].X, laidOut.Nodes[1].Y));
        Assert.Equal(30, laidOut.Nodes[0].Y);
        Assert.Equal((200d, 60d), (laidOut.Nodes[0].Width, laidOut.Nodes[0].Height));
        Assert.Equal(document.Edges, laidOut.Edges);
        Assert.Equal(document.Notes, laidOut.Notes);
        Assert.Equal("root", laidOut.RootId);
    }

    [Fact]
    public void Options_HaveTheDocumentedDefaults()
    {
        var options = new OmniGraphLayoutOptions();

        Assert.Equal(OmniGraphDirection.LeftToRight, options.Direction);
        Assert.Equal(40, options.NodeSpacing);
        Assert.Equal(80, options.LayerSpacing);
    }
}
