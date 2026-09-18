namespace OmniEurope.Blazor.Internal;

/// <summary>
/// A piece of line drawn inside one row of an <see cref="Components.OmniGitGraph{TItem}"/>: either in
/// the upper half, from the top of the row to the commit or straight down, or in the lower half, from
/// the commit or straight down to the bottom of the row.
/// </summary>
/// <param name="FromLane">Lane the line starts in.</param>
/// <param name="ToLane">Lane the line ends in.</param>
/// <param name="Upper">Whether it is drawn in the upper half of the row.</param>
/// <param name="ColorLane">Lane whose colour the line takes.</param>
internal sealed record GitGraphSegment(int FromLane, int ToLane, bool Upper, int ColorLane);

/// <summary>One row: the lane of its commit, whether it merges, and the lines crossing the row.</summary>
internal sealed record GitGraphRow(int Lane, bool IsMerge, IReadOnlyList<GitGraphSegment> Segments);

/// <summary>
/// The lanes of a commit graph, computed from parent identifiers alone, one row per commit, newest
/// first. Each commit continues the lane of the first child waiting for it; its first parent takes its
/// lane on, every other parent opens (or joins) another lane, and the lanes of the other children
/// waiting for it close on it. Each row is drawn on its own, so rows stay aligned with the host's text
/// whatever the height of a row.
/// </summary>
internal sealed record GitGraphLayout(IReadOnlyList<GitGraphRow> Rows, int LaneCount)
{
    /// <summary>Width of a lane, in the units of a row's drawing.</summary>
    internal const double LaneWidth = 16;

    /// <summary>Height of a row, in the units of its drawing.</summary>
    internal const double RowHeight = 32;

    /// <summary>Radius of the point of a commit.</summary>
    internal const double NodeRadius = 5;

    internal static GitGraphLayout Build(IReadOnlyList<string> ids, IReadOnlyList<IReadOnlyList<string>> parents)
    {
        var lanes = new List<string?>();
        var rows = new List<GitGraphRow>(ids.Count);
        var laneCount = 0;
        for (var row = 0; row < ids.Count; row++)
        {
            var id = ids[row];
            var segments = new List<GitGraphSegment>();

            var mine = new List<int>();
            for (var lane = 0; lane < lanes.Count; lane++)
            {
                if (string.Equals(lanes[lane], id, StringComparison.Ordinal))
                {
                    mine.Add(lane);
                }
            }

            var column = mine.Count > 0 ? mine[0] : FreeOrAppend(lanes);
            for (var lane = 0; lane < lanes.Count; lane++)
            {
                if (lanes[lane] is null)
                {
                    continue;
                }

                segments.Add(mine.Contains(lane)
                    ? new GitGraphSegment(lane, column, Upper: true, ColorLane: lane)
                    : new GitGraphSegment(lane, lane, Upper: true, ColorLane: lane));
            }

            // The other children converging on this commit free their lanes.
            foreach (var lane in mine.Skip(1))
            {
                lanes[lane] = null;
            }

            var opened = new HashSet<int>();
            var commitParents = parents[row];
            if (commitParents.Count > 0)
            {
                lanes[column] = commitParents[0];
                opened.Add(column);
                for (var index = 1; index < commitParents.Count; index++)
                {
                    var parent = commitParents[index];
                    if (string.Equals(parent, commitParents[0], StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var existing = lanes.IndexOf(parent);
                    var lane = existing >= 0 ? existing : FreeOrAppend(lanes);
                    lanes[lane] = parent;
                    opened.Add(lane);
                }
            }
            else
            {
                lanes[column] = null;
            }

            for (var lane = 0; lane < lanes.Count; lane++)
            {
                if (lanes[lane] is null)
                {
                    continue;
                }

                if (opened.Contains(lane))
                {
                    segments.Add(new GitGraphSegment(column, lane, Upper: false, ColorLane: lane));
                }

                // A lane passing by continues; a lane this commit merges into an existing one both
                // continues and receives the merge line.
                if (lane != column && segments.Any(segment => segment.Upper && segment.FromLane == lane && segment.ToLane == lane))
                {
                    segments.Add(new GitGraphSegment(lane, lane, Upper: false, ColorLane: lane));
                }
            }

            laneCount = Math.Max(laneCount, lanes.Count);
            rows.Add(new GitGraphRow(column, commitParents.Count > 1, segments));
        }

        return new GitGraphLayout(rows, Math.Max(1, laneCount));
    }

    /// <summary>Centre of a lane, across the row.</summary>
    internal static double LaneX(int lane) => (LaneWidth / 2) + (lane * LaneWidth);

    /// <summary>The path of a segment: straight within a lane, a curve from one lane to another.</summary>
    internal static string PathOf(GitGraphSegment segment)
    {
        var (fromY, toY) = segment.Upper ? (0d, RowHeight / 2) : (RowHeight / 2, RowHeight);
        var fromX = LaneX(segment.FromLane);
        var toX = LaneX(segment.ToLane);
        if (segment.FromLane == segment.ToLane)
        {
            return $"M {MindMapGeometry.Format(fromX)} {MindMapGeometry.Format(fromY)} L {MindMapGeometry.Format(toX)} {MindMapGeometry.Format(toY)}";
        }

        var middle = (fromY + toY) / 2;
        return $"M {MindMapGeometry.Format(fromX)} {MindMapGeometry.Format(fromY)} C {MindMapGeometry.Format(fromX)} {MindMapGeometry.Format(middle)}, {MindMapGeometry.Format(toX)} {MindMapGeometry.Format(middle)}, {MindMapGeometry.Format(toX)} {MindMapGeometry.Format(toY)}";
    }

    private static int FreeOrAppend(List<string?> lanes)
    {
        for (var lane = 0; lane < lanes.Count; lane++)
        {
            if (lanes[lane] is null)
            {
                return lane;
            }
        }

        lanes.Add(null);
        return lanes.Count - 1;
    }
}
