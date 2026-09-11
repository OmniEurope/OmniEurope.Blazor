using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The automatic layout of a mind map: children spread on arcs around their parent, each level on a
/// shorter radius, then overlapping boxes pushed apart.
/// </summary>
internal static class MindMapLayout
{
    private const int OverlapIterations = 30;
    private const double OverlapPadding = 20;
    private const double CellSize = 180;

    /// <summary>
    /// New centres for every node. Nodes the root cannot reach are lined up along the bottom edge,
    /// 160 units apart, so that none is lost off the canvas.
    /// </summary>
    public static Dictionary<string, (double X, double Y)> Radial(
        IReadOnlyList<OmniMindMapNode> nodes,
        IReadOnlyList<OmniMindMapEdge> edges,
        string? rootId,
        double canvasWidth,
        double canvasHeight,
        Func<OmniMindMapNode, (double Width, double Height)> sizeOf)
    {
        var known = nodes.Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
        var root = nodes.FirstOrDefault(node => string.Equals(node.Id, rootId, StringComparison.Ordinal))
            ?? nodes.FirstOrDefault();
        var positions = new Dictionary<string, (double X, double Y)>(StringComparer.Ordinal);
        if (root is null)
        {
            return positions;
        }

        var children = nodes.ToDictionary(node => node.Id, _ => new List<string>(), StringComparer.Ordinal);
        foreach (var edge in edges.Where(edge => known.Contains(edge.From) && known.Contains(edge.To)))
        {
            children[edge.From].Add(edge.To);
        }

        var width = Math.Max(canvasWidth, 800);
        var height = Math.Max(canvasHeight, 600);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        Place(root.Id, width / 2, height / 2, 0, Math.PI * 2, Math.Min(width, height) * 0.35);

        var boxes = new List<Box>(nodes.Count);
        var nextFreeX = 50d;
        foreach (var node in nodes)
        {
            var (x, y) = positions.TryGetValue(node.Id, out var placed) ? placed : (nextFreeX, height - 80);
            if (!positions.ContainsKey(node.Id))
            {
                nextFreeX += 160;
            }

            var (boxWidth, boxHeight) = sizeOf(node);
            boxes.Add(new Box(node.Id, x, y, boxWidth, boxHeight));
        }

        ResolveOverlaps(boxes);
        return boxes.ToDictionary(box => box.Id, box => (box.X, box.Y), StringComparer.Ordinal);

        void Place(string id, double x, double y, double startAngle, double endAngle, double radius)
        {
            if (!visited.Add(id))
            {
                return;
            }

            positions[id] = (x, y);
            var pending = children[id].Where(child => !visited.Contains(child)).ToArray();
            if (pending.Length == 0)
            {
                return;
            }

            var step = (endAngle - startAngle) / pending.Length;
            for (var index = 0; index < pending.Length; index++)
            {
                var angle = startAngle + (step * (index + 0.5));
                Place(
                    pending[index],
                    x + (Math.Cos(angle) * radius),
                    y + (Math.Sin(angle) * radius),
                    angle - (step * 0.45),
                    angle + (step * 0.45),
                    radius * 0.72);
            }
        }
    }

    /// <summary>
    /// Pushes overlapping boxes apart along the axis of least overlap, both by half of it, until
    /// nothing overlaps or the iteration budget is spent. A coarse grid keeps each pass close to
    /// linear by only comparing boxes in neighbouring cells.
    /// </summary>
    private static void ResolveOverlaps(List<Box> boxes)
    {
        for (var iteration = 0; iteration < OverlapIterations; iteration++)
        {
            var moved = false;
            var grid = new Dictionary<(long, long), List<Box>>();
            foreach (var current in boxes)
            {
                var cellX = (long)Math.Floor(current.X / CellSize);
                var cellY = (long)Math.Floor(current.Y / CellSize);
                for (var dx = -1; dx <= 1; dx++)
                {
                    for (var dy = -1; dy <= 1; dy++)
                    {
                        if (!grid.TryGetValue((cellX + dx, cellY + dy), out var nearby))
                        {
                            continue;
                        }

                        foreach (var other in nearby)
                        {
                            moved |= Separate(other, current);
                        }
                    }
                }

                if (!grid.TryGetValue((cellX, cellY), out var cell))
                {
                    cell = [];
                    grid[(cellX, cellY)] = cell;
                }

                cell.Add(current);
            }

            if (!moved)
            {
                return;
            }
        }
    }

    private static bool Separate(Box a, Box b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var overlapX = ((a.Width + OverlapPadding) / 2) + ((b.Width + OverlapPadding) / 2) - Math.Abs(dx);
        var overlapY = ((a.Height + OverlapPadding) / 2) + ((b.Height + OverlapPadding) / 2) - Math.Abs(dy);
        if (overlapX <= 0 || overlapY <= 0)
        {
            return false;
        }

        if (overlapX < overlapY)
        {
            var push = (overlapX / 2) + 1;
            var direction = dx >= 0 ? 1 : -1;
            a.X -= push * direction;
            b.X += push * direction;
        }
        else
        {
            var push = (overlapY / 2) + 1;
            var direction = dy >= 0 ? 1 : -1;
            a.Y -= push * direction;
            b.Y += push * direction;
        }

        return true;
    }

    private sealed class Box(string id, double x, double y, double width, double height)
    {
        public string Id { get; } = id;

        public double X { get; set; } = x;

        public double Y { get; set; } = y;

        public double Width { get; } = width;

        public double Height { get; } = height;
    }
}
