using Bunit;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniGitGraph{TItem}"/>: the lanes <see cref="GitGraphLayout"/> computes from the parent
/// identifiers alone, and the rows the component draws from them.
/// </summary>
public sealed class GitGraphComponentTests : OmniBunitContext
{
    // Newest first: a merge of a feature branch (f4, f2) into the main line (c3), both from c1.
    private static readonly IReadOnlyList<Commit> Branched =
    [
        new("m5", ["c3", "f4"]),
        new("f4", ["f2"]),
        new("c3", ["c1"]),
        new("f2", ["c1"]),
        new("c1", [])
    ];

    [Fact]
    public void Layout_OfALinearHistory_IsOneLaneOpenedByTheNewestAndClosedByTheRoot()
    {
        var layout = Build([new("c3", ["c2"]), new("c2", ["c1"]), new("c1", [])]);

        Assert.Equal(1, layout.LaneCount);
        Assert.All(layout.Rows, row => Assert.Equal(0, row.Lane));
        Assert.All(layout.Rows, row => Assert.False(row.IsMerge));
        Assert.Equal([new GitGraphSegment(0, 0, Upper: false, ColorLane: 0)], layout.Rows[0].Segments);
        Assert.Equal(
            [new GitGraphSegment(0, 0, Upper: true, ColorLane: 0), new GitGraphSegment(0, 0, Upper: false, ColorLane: 0)],
            layout.Rows[1].Segments);
        Assert.Equal([new GitGraphSegment(0, 0, Upper: true, ColorLane: 0)], layout.Rows[2].Segments);
    }

    [Fact]
    public void Layout_OfABranchAndItsMerge_OpensASecondLane_ThenClosesItOnTheCommonParent()
    {
        var layout = Build(Branched);

        Assert.Equal(2, layout.LaneCount);
        Assert.Equal([0, 1, 0, 1, 0], layout.Rows.Select(row => row.Lane));
        Assert.Equal([true, false, false, false, false], layout.Rows.Select(row => row.IsMerge));

        // The merge opens the second parent's lane with a curve out of its own point.
        Assert.Contains(new GitGraphSegment(0, 1, Upper: false, ColorLane: 1), layout.Rows[0].Segments);

        // While the feature commits are drawn, the main lane passes by, straight through the row.
        Assert.Contains(new GitGraphSegment(0, 0, Upper: true, ColorLane: 0), layout.Rows[1].Segments);
        Assert.Contains(new GitGraphSegment(0, 0, Upper: false, ColorLane: 0), layout.Rows[1].Segments);

        // Both lanes wait for c1; the second one curves into it and ends there.
        Assert.Contains(new GitGraphSegment(1, 0, Upper: true, ColorLane: 1), layout.Rows[4].Segments);
        Assert.DoesNotContain(layout.Rows[4].Segments, segment => !segment.Upper);
    }

    [Fact]
    public void Layout_KeepsTheLaneOfAParentOlderThanThePage_RunningToTheBottom()
    {
        var layout = Build([new("a", ["older"])]);

        Assert.Equal([new GitGraphSegment(0, 0, Upper: false, ColorLane: 0)], layout.Rows[0].Segments);
    }

    [Fact]
    public void Layout_OpensNoSecondLane_ForAParentNamedTwice()
    {
        var layout = Build([new("a", ["b", "b"]), new("b", [])]);

        Assert.Equal(1, layout.LaneCount);
        Assert.Equal([new GitGraphSegment(0, 0, Upper: false, ColorLane: 0)], layout.Rows[0].Segments);
    }

    [Fact]
    public void Paths_AreStraightWithinALane_AndCurvedFromOneLaneToAnother()
    {
        Assert.Equal("M 8 0 L 8 16", GitGraphLayout.PathOf(new GitGraphSegment(0, 0, Upper: true, ColorLane: 0)));
        Assert.Equal("M 24 16 L 24 32", GitGraphLayout.PathOf(new GitGraphSegment(1, 1, Upper: false, ColorLane: 1)));
        Assert.Equal("M 24 0 C 24 8, 8 8, 8 16", GitGraphLayout.PathOf(new GitGraphSegment(1, 0, Upper: true, ColorLane: 1)));
        Assert.Equal(40, GitGraphLayout.LaneX(2));
    }

    [Fact]
    public void Render_DrawsAnOrderedListNamedByDefault_OneRowPerCommit_WithItsOwnDrawing()
    {
        var graph = RenderGraph(Branched);

        var list = graph.Find("ol.omni-git-graph");
        Assert.Equal("Historique des commits", list.GetAttribute("aria-label"));
        var rows = graph.FindAll("li.omni-git-graph__row");
        Assert.Equal(5, rows.Count);
        Assert.Equal(["0", "1", "0", "1", "0"], rows.Select(row => row.GetAttribute("data-omni-lane")));

        var drawing = rows[1].QuerySelector("svg")!;
        Assert.Equal("true", drawing.GetAttribute("aria-hidden"));
        Assert.Equal("false", drawing.GetAttribute("focusable"));
        Assert.Equal("0 0 32 32", drawing.GetAttribute("viewBox"));
        var node = drawing.QuerySelector("circle")!;
        Assert.Equal(("24", "16", "5"), (node.GetAttribute("cx"), node.GetAttribute("cy"), node.GetAttribute("r")));
        Assert.Contains("omni-git-graph__lane--1", node.ClassList);
        Assert.Equal("f4", rows[1].QuerySelector(".omni-git-graph__content code")!.TextContent);
        Assert.DoesNotContain("style=", graph.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Render_MarksAMerge_ForTheEyeAndForAssistiveTechnology()
    {
        var graph = RenderGraph(Branched);

        var rows = graph.FindAll("li.omni-git-graph__row");
        Assert.Contains("omni-git-graph__node--merge", rows[0].QuerySelector("circle")!.ClassList);
        Assert.Equal("Commit de fusion.", rows[0].QuerySelector(".omni-git-graph__content .omni-visually-hidden")!.TextContent);
        Assert.All(rows.Skip(1), row =>
        {
            Assert.DoesNotContain("omni-git-graph__node--merge", row.QuerySelector("circle")!.ClassList);
            Assert.Null(row.QuerySelector(".omni-visually-hidden"));
        });
        var edges = rows[0].QuerySelectorAll("path");
        Assert.Contains(edges, edge => edge.GetAttribute("d") == "M 8 16 C 8 24, 24 24, 24 32" && edge.ClassList.Contains("omni-git-graph__lane--1"));
    }

    [Fact]
    public void Render_TakesTheEightChartColoursInTurn()
    {
        // An octopus merge of nine parents opens nine lanes; the ninth takes the first colour again.
        IReadOnlyList<string> parents = [.. Enumerable.Range(0, 9).Select(index => $"p{index}")];
        var graph = RenderGraph([new("octopus", parents)]);

        var edges = graph.FindAll("path.omni-git-graph__edge");
        Assert.Equal(9, edges.Count);
        Assert.Equal(
            ["0", "1", "2", "3", "4", "5", "6", "7", "0"],
            edges.Select(edge => edge.ClassList.Single(name => name.StartsWith("omni-git-graph__lane--", StringComparison.Ordinal))["omni-git-graph__lane--".Length..]));
        Assert.Equal("0 0 144 32", graph.Find("svg").GetAttribute("viewBox"));
    }

    [Fact]
    public void Render_WithNoCommit_WritesTheEmptyText_AndTheParametersReplaceTheDefaults()
    {
        var empty = RenderGraph([]);
        Assert.Equal("Aucun commit", empty.Find("p.omni-git-graph__empty").TextContent);
        Assert.Empty(empty.FindAll("ol"));

        var custom = Render<OmniGitGraph<Commit>>(parameters => parameters
            .Add(component => component.Items, [])
            .Add(component => component.IdOf, commit => commit.Id)
            .Add(component => component.ParentsOf, commit => commit.Parents)
            .Add(component => component.EmptyText, "Rien à montrer")
            .Add(component => component.Id, "history"));
        Assert.Equal("Rien à montrer", custom.Find("#history").TextContent);

        var named = Render<OmniGitGraph<Commit>>(parameters => parameters
            .Add(component => component.Items, Branched)
            .Add(component => component.IdOf, commit => commit.Id)
            .Add(component => component.ParentsOf, commit => commit.Parents)
            .Add(component => component.Label, "Branche main"));
        Assert.Equal("Branche main", named.Find("ol").GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_ANullParentList_CountsAsNoParent()
    {
        var graph = Render<OmniGitGraph<Commit>>(parameters => parameters
            .Add(component => component.Items, [new Commit("root", [])])
            .Add(component => component.IdOf, commit => commit.Id)
            .Add(component => component.ParentsOf, _ => null!));

        Assert.Single(graph.FindAll("li"));
        Assert.Empty(graph.FindAll("path"));
    }

    [Fact]
    public void RequiredDelegates_AreChecked()
    {
        Assert.Throws<ArgumentNullException>(() => Render<OmniGitGraph<Commit>>(parameters => parameters
            .Add(component => component.Items, Branched)
            .Add(component => component.ParentsOf, commit => commit.Parents)));
        Assert.Throws<ArgumentNullException>(() => Render<OmniGitGraph<Commit>>(parameters => parameters
            .Add(component => component.Items, Branched)
            .Add(component => component.IdOf, commit => commit.Id)));
    }

    private static GitGraphLayout Build(IReadOnlyList<Commit> commits) =>
        GitGraphLayout.Build([.. commits.Select(commit => commit.Id)], [.. commits.Select(commit => commit.Parents)]);

    private IRenderedComponent<OmniGitGraph<Commit>> RenderGraph(IReadOnlyList<Commit> commits) =>
        Render<OmniGitGraph<Commit>>(parameters => parameters
            .Add(component => component.Items, commits)
            .Add(component => component.IdOf, commit => commit.Id)
            .Add(component => component.ParentsOf, commit => commit.Parents)
            .Add(component => component.RowTemplate, commit => builder =>
            {
                builder.OpenElement(0, "code");
                builder.AddContent(1, commit.Id);
                builder.CloseElement();
            }));

    public sealed record Commit(string Id, IReadOnlyList<string> Parents);
}
