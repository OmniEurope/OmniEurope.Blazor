using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The layered drawing of a directed graph, in the Sugiyama style: cycles broken by reversing the
/// edges a depth-first walk finds going back, nodes given a layer by their longest path, long edges
/// cut into chains of virtual nodes, each layer ordered by the barycentre of its neighbours to cut
/// crossings, then nodes placed along their layer as close to their neighbours as the spacing allows.
/// </summary>
/// <remarks>
/// Pure and deterministic: the same graph, in the same order, always gets the same drawing, so a
/// host that lays out on every refresh does not see its graph jump. Every step is linear or close to
/// it in the number of nodes and edge segments; the costliest, the crossing count, is a Fenwick-tree
/// inversion count per pair of layers.
/// </remarks>
internal static class GraphLayeredLayout
{
    /// <summary>Alternating down and up sweeps of the crossing reduction.</summary>
    internal const int OrderingPasses = 8;

    /// <summary>Alternating down and up sweeps of the placement along the layers.</summary>
    internal const int PlacementPasses = 6;

    internal static OmniGraphLayoutResult Build(
        IReadOnlyList<OmniGraphLayoutNode> nodes,
        IReadOnlyList<OmniGraphLayoutEdge> edges,
        OmniGraphLayoutOptions options)
    {
        var nodeSpacing = Sanitize(options.NodeSpacing);
        var layerSpacing = Sanitize(options.LayerSpacing);
        var leftToRight = options.Direction == OmniGraphDirection.LeftToRight;

        // Real nodes, first occurrence of an identifier wins.
        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        var ids = new List<string>(nodes.Count);
        var mainSizes = new List<double>(nodes.Count);
        var crossSizes = new List<double>(nodes.Count);
        foreach (var node in nodes)
        {
            if (node?.Id is null || !index.TryAdd(node.Id, ids.Count))
            {
                continue;
            }

            ids.Add(node.Id);
            var width = Sanitize(node.Width);
            var height = Sanitize(node.Height);
            mainSizes.Add(leftToRight ? width : height);
            crossSizes.Add(leftToRight ? height : width);
        }

        var realCount = ids.Count;
        if (realCount == 0)
        {
            return new OmniGraphLayoutResult(new Dictionary<string, OmniGraphPoint>(StringComparer.Ordinal), 0, 0);
        }

        var dag = BreakCycles(realCount, DistinctEdges(edges, index));
        var layerOf = AssignLayers(realCount, dag);

        // Every node of the drawing, real ones first, then the virtual nodes long edges pass through.
        var isVirtual = new List<bool>(realCount);
        for (var node = 0; node < realCount; node++)
        {
            isVirtual.Add(false);
        }

        var predecessors = new List<List<int>>(realCount);
        var successors = new List<List<int>>(realCount);
        for (var node = 0; node < realCount; node++)
        {
            predecessors.Add([]);
            successors.Add([]);
        }

        foreach (var (from, to) in dag)
        {
            var previous = from;
            for (var layer = layerOf[from] + 1; layer < layerOf[to]; layer++)
            {
                var chain = isVirtual.Count;
                isVirtual.Add(true);
                layerOf.Add(layer);
                mainSizes.Add(0);
                crossSizes.Add(0);
                predecessors.Add([]);
                successors.Add([]);
                Link(previous, chain);
                previous = chain;
            }

            Link(previous, to);
        }

        var total = isVirtual.Count;
        var layerCount = layerOf.Max() + 1;
        var layers = new List<int>[layerCount];
        for (var layer = 0; layer < layerCount; layer++)
        {
            layers[layer] = [];
        }

        for (var node = 0; node < total; node++)
        {
            layers[layerOf[node]].Add(node);
        }

        var position = new int[total];
        OrderLayers(layers, position, predecessors, successors);

        var cross = PlaceAlongLayers(layers, predecessors, successors, crossSizes, isVirtual, nodeSpacing);

        // Along the flow: each layer as thick as its thickest node, the layers a fixed gap apart.
        var mainCentre = new double[layerCount];
        var mainStart = 0d;
        for (var layer = 0; layer < layerCount; layer++)
        {
            var thickness = layers[layer].Count == 0 ? 0 : layers[layer].Max(node => mainSizes[node]);
            mainCentre[layer] = mainStart + (thickness / 2);
            mainStart += thickness + (layer < layerCount - 1 ? layerSpacing : 0);
        }

        var crossMin = double.MaxValue;
        var crossMax = double.MinValue;
        for (var node = 0; node < total; node++)
        {
            crossMin = Math.Min(crossMin, cross[node] - (crossSizes[node] / 2));
            crossMax = Math.Max(crossMax, cross[node] + (crossSizes[node] / 2));
        }

        var positions = new Dictionary<string, OmniGraphPoint>(realCount, StringComparer.Ordinal);
        for (var node = 0; node < realCount; node++)
        {
            var along = mainCentre[layerOf[node]];
            var across = cross[node] - crossMin;
            positions[ids[node]] = leftToRight ? new OmniGraphPoint(along, across) : new OmniGraphPoint(across, along);
        }

        var extentMain = mainStart;
        var extentCross = crossMax - crossMin;
        return leftToRight
            ? new OmniGraphLayoutResult(positions, extentMain, extentCross)
            : new OmniGraphLayoutResult(positions, extentCross, extentMain);

        void Link(int from, int to)
        {
            successors[from].Add(to);
            predecessors[to].Add(from);
        }
    }

    private static double Sanitize(double value) => double.IsFinite(value) && value > 0 ? value : 0;

    /// <summary>The edges between two known, different nodes, each pair once.</summary>
    private static List<(int From, int To)> DistinctEdges(IReadOnlyList<OmniGraphLayoutEdge> edges, Dictionary<string, int> index)
    {
        var seen = new HashSet<long>();
        var result = new List<(int From, int To)>(edges.Count);
        foreach (var edge in edges)
        {
            if (edge?.From is null || edge.To is null
                || !index.TryGetValue(edge.From, out var from)
                || !index.TryGetValue(edge.To, out var to)
                || from == to
                || !seen.Add(((long)from << 32) | (uint)to))
            {
                continue;
            }

            result.Add((from, to));
        }

        return result;
    }

    /// <summary>
    /// Reverses every edge an iterative depth-first walk, started from each node in input order, finds
    /// pointing back to a node still on its path: what remains has no cycle. A pair left in both
    /// directions by the reversal is kept once.
    /// </summary>
    private static List<(int From, int To)> BreakCycles(int count, List<(int From, int To)> edges)
    {
        var outgoing = new List<int>[count];
        for (var node = 0; node < count; node++)
        {
            outgoing[node] = [];
        }

        for (var edge = 0; edge < edges.Count; edge++)
        {
            outgoing[edges[edge].From].Add(edge);
        }

        var state = new byte[count];
        var reversed = new bool[edges.Count];
        var stack = new Stack<(int Node, int Next)>();
        for (var root = 0; root < count; root++)
        {
            if (state[root] != 0)
            {
                continue;
            }

            state[root] = 1;
            stack.Push((root, 0));
            while (stack.Count > 0)
            {
                var (node, next) = stack.Pop();
                if (next >= outgoing[node].Count)
                {
                    state[node] = 2;
                    continue;
                }

                stack.Push((node, next + 1));
                var edge = outgoing[node][next];
                var target = edges[edge].To;
                if (state[target] == 0)
                {
                    state[target] = 1;
                    stack.Push((target, 0));
                }
                else if (state[target] == 1)
                {
                    reversed[edge] = true;
                }
            }
        }

        var seen = new HashSet<long>();
        var dag = new List<(int From, int To)>(edges.Count);
        for (var edge = 0; edge < edges.Count; edge++)
        {
            var (from, to) = reversed[edge] ? (edges[edge].To, edges[edge].From) : edges[edge];
            if (seen.Add(((long)from << 32) | (uint)to))
            {
                dag.Add((from, to));
            }
        }

        return dag;
    }

    /// <summary>
    /// The longest path from a source gives each node its layer; then, from the sinks back, a node is
    /// moved forward to just before its nearest successor, so a source feeding only a deep node sits
    /// beside it rather than at the far start with one very long edge.
    /// </summary>
    private static List<int> AssignLayers(int count, List<(int From, int To)> dag)
    {
        var incoming = new int[count];
        var outgoing = new List<int>[count];
        for (var node = 0; node < count; node++)
        {
            outgoing[node] = [];
        }

        foreach (var (from, to) in dag)
        {
            outgoing[from].Add(to);
            incoming[to]++;
        }

        var layer = new int[count];
        var order = new List<int>(count);
        var ready = new Queue<int>();
        for (var node = 0; node < count; node++)
        {
            if (incoming[node] == 0)
            {
                ready.Enqueue(node);
            }
        }

        while (ready.Count > 0)
        {
            var node = ready.Dequeue();
            order.Add(node);
            foreach (var next in outgoing[node])
            {
                layer[next] = Math.Max(layer[next], layer[node] + 1);
                if (--incoming[next] == 0)
                {
                    ready.Enqueue(next);
                }
            }
        }

        for (var step = order.Count - 1; step >= 0; step--)
        {
            var node = order[step];
            if (outgoing[node].Count == 0)
            {
                continue;
            }

            var nearest = outgoing[node].Min(next => layer[next]);
            layer[node] = Math.Max(layer[node], nearest - 1);
        }

        return [.. layer];
    }

    /// <summary>
    /// Barycentre sweeps, down then up, keeping the ordering that crossed least. A node with no
    /// neighbour on the side a sweep looks at keeps its place.
    /// </summary>
    private static void OrderLayers(List<int>[] layers, int[] position, List<List<int>> predecessors, List<List<int>> successors)
    {
        Renumber(layers, position);
        var best = layers.Select(layer => layer.ToArray()).ToArray();
        var bestCrossings = CountCrossings(layers, position, successors);
        for (var pass = 0; pass < OrderingPasses && bestCrossings > 0; pass++)
        {
            var down = pass % 2 == 0;
            if (down)
            {
                for (var layer = 1; layer < layers.Length; layer++)
                {
                    Reorder(layers[layer], position, predecessors);
                }
            }
            else
            {
                for (var layer = layers.Length - 2; layer >= 0; layer--)
                {
                    Reorder(layers[layer], position, successors);
                }
            }

            var crossings = CountCrossings(layers, position, successors);
            if (crossings < bestCrossings)
            {
                bestCrossings = crossings;
                best = layers.Select(layer => layer.ToArray()).ToArray();
            }
        }

        for (var layer = 0; layer < layers.Length; layer++)
        {
            layers[layer].Clear();
            layers[layer].AddRange(best[layer]);
        }

        Renumber(layers, position);
    }

    private static void Renumber(List<int>[] layers, int[] position)
    {
        foreach (var layer in layers)
        {
            for (var place = 0; place < layer.Count; place++)
            {
                position[layer[place]] = place;
            }
        }
    }

    private static void Reorder(List<int> layer, int[] position, List<List<int>> neighbours)
    {
        var keyed = new (double Key, int Place, int Node)[layer.Count];
        for (var place = 0; place < layer.Count; place++)
        {
            var node = layer[place];
            var around = neighbours[node];
            double key = place;
            if (around.Count > 0)
            {
                var sum = 0d;
                foreach (var other in around)
                {
                    sum += position[other];
                }

                key = sum / around.Count;
            }

            keyed[place] = (key, place, node);
        }

        Array.Sort(keyed, static (left, right) =>
        {
            var byKey = left.Key.CompareTo(right.Key);
            return byKey != 0 ? byKey : left.Place.CompareTo(right.Place);
        });
        for (var place = 0; place < keyed.Length; place++)
        {
            layer[place] = keyed[place].Node;
            position[keyed[place].Node] = place;
        }
    }

    /// <summary>Crossings between every pair of adjacent layers, counted as inversions.</summary>
    internal static long CountCrossings(List<int>[] layers, int[] position, List<List<int>> successors)
    {
        long crossings = 0;
        for (var layer = 0; layer < layers.Length - 1; layer++)
        {
            var segments = new List<(int Upper, int Lower)>();
            foreach (var node in layers[layer])
            {
                foreach (var next in successors[node])
                {
                    segments.Add((position[node], position[next]));
                }
            }

            if (segments.Count < 2)
            {
                continue;
            }

            segments.Sort();
            var width = layers[layer + 1].Count;
            var tree = new int[width + 1];
            var seen = 0;
            foreach (var (_, lower) in segments)
            {
                // Segments already seen that end further along cross this one.
                var atOrBefore = 0;
                for (var at = lower + 1; at > 0; at -= at & -at)
                {
                    atOrBefore += tree[at];
                }

                crossings += seen - atOrBefore;
                for (var at = lower + 1; at <= width; at += at & -at)
                {
                    tree[at]++;
                }

                seen++;
            }
        }

        return crossings;
    }

    /// <summary>
    /// Positions across the flow. Each node aims at the median of its neighbours on the side a sweep
    /// looks at; the positions closest to those aims that keep the layer's order and spacing are the
    /// isotonic regression of the aims, solved exactly by pooling adjacent violators.
    /// </summary>
    private static double[] PlaceAlongLayers(
        List<int>[] layers,
        List<List<int>> predecessors,
        List<List<int>> successors,
        List<double> crossSizes,
        List<bool> isVirtual,
        double nodeSpacing)
    {
        var cross = new double[crossSizes.Count];
        var widest = 0d;
        var extents = new double[layers.Length];
        for (var layer = 0; layer < layers.Length; layer++)
        {
            var offset = 0d;
            var members = layers[layer];
            for (var place = 0; place < members.Count; place++)
            {
                if (place > 0)
                {
                    offset += Separation(members[place - 1], members[place]);
                }

                cross[members[place]] = offset;
            }

            extents[layer] = offset;
            widest = Math.Max(widest, offset);
        }

        // Every layer starts centred on the widest one.
        for (var layer = 0; layer < layers.Length; layer++)
        {
            var shift = (widest - extents[layer]) / 2;
            foreach (var node in layers[layer])
            {
                cross[node] += shift;
            }
        }

        var aims = new List<double>();
        var blocks = new List<(double Sum, int Count)>();
        var around = new List<double>();
        for (var pass = 0; pass < PlacementPasses; pass++)
        {
            var down = pass % 2 == 0;
            if (down)
            {
                for (var layer = 1; layer < layers.Length; layer++)
                {
                    Place(layers[layer], predecessors);
                }
            }
            else
            {
                for (var layer = layers.Length - 2; layer >= 0; layer--)
                {
                    Place(layers[layer], successors);
                }
            }
        }

        return cross;

        double Separation(int left, int right)
        {
            // Long edges travel in bundles: two virtual nodes side by side sit half a gap apart.
            var gap = isVirtual[left] && isVirtual[right] ? nodeSpacing / 2 : nodeSpacing;
            return ((crossSizes[left] + crossSizes[right]) / 2) + gap;
        }

        void Place(List<int> members, List<List<int>> neighbours)
        {
            if (members.Count == 0)
            {
                return;
            }

            aims.Clear();
            var offset = 0d;
            for (var place = 0; place < members.Count; place++)
            {
                var node = members[place];
                if (place > 0)
                {
                    offset += Separation(members[place - 1], node);
                }

                var aim = cross[node];
                if (neighbours[node].Count > 0)
                {
                    around.Clear();
                    foreach (var other in neighbours[node])
                    {
                        around.Add(cross[other]);
                    }

                    around.Sort();
                    var middle = around.Count / 2;
                    aim = around.Count % 2 == 1 ? around[middle] : (around[middle - 1] + around[middle]) / 2;
                }

                // In the shifted space the order constraint becomes plain monotonicity.
                aims.Add(aim - offset);
            }

            blocks.Clear();
            foreach (var aim in aims)
            {
                blocks.Add((aim, 1));
                while (blocks.Count > 1 && blocks[^2].Sum / blocks[^2].Count > blocks[^1].Sum / blocks[^1].Count)
                {
                    var last = blocks[^1];
                    var before = blocks[^2];
                    blocks.RemoveAt(blocks.Count - 1);
                    blocks[^1] = (before.Sum + last.Sum, before.Count + last.Count);
                }
            }

            var at = 0;
            offset = 0;
            foreach (var (sum, count) in blocks)
            {
                var value = sum / count;
                for (var member = 0; member < count; member++, at++)
                {
                    if (at > 0)
                    {
                        offset += Separation(members[at - 1], members[at]);
                    }

                    cross[members[at]] = value + offset;
                }
            }
        }
    }
}
