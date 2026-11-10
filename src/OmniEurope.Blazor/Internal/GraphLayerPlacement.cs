namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Positions across the flow. Each node aims at the median of its neighbours on the side a sweep
/// looks at; the positions closest to those aims that keep the layer's order and spacing are the
/// isotonic regression of the aims, solved exactly by pooling adjacent violators.
/// </summary>
internal sealed class GraphLayerPlacement(
    List<int>[] layers,
    List<List<int>> predecessors,
    List<List<int>> successors,
    List<double> crossSizes,
    List<bool> isVirtual,
    double nodeSpacing)
{
    private readonly double[] _cross = new double[crossSizes.Count];
    private readonly List<double> _aims = [];
    private readonly List<(double Sum, int Count)> _blocks = [];
    private readonly List<double> _around = [];

    /// <summary>The position of every node across the flow, after <see cref="GraphLayeredLayout.PlacementPasses"/> sweeps.</summary>
    internal static double[] Place(
        List<int>[] layers,
        List<List<int>> predecessors,
        List<List<int>> successors,
        List<double> crossSizes,
        List<bool> isVirtual,
        double nodeSpacing) =>
        new GraphLayerPlacement(layers, predecessors, successors, crossSizes, isVirtual, nodeSpacing).Run();

    private double[] Run()
    {
        PackLayers();
        for (var pass = 0; pass < GraphLayeredLayout.PlacementPasses; pass++)
        {
            Sweep(down: pass % 2 == 0);
        }

        return _cross;
    }

    /// <summary>Packs each layer from offset zero at the spacing, then centres every layer on the widest one.</summary>
    private void PackLayers()
    {
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

                _cross[members[place]] = offset;
            }

            extents[layer] = offset;
            widest = Math.Max(widest, offset);
        }

        for (var layer = 0; layer < layers.Length; layer++)
        {
            var shift = (widest - extents[layer]) / 2;
            foreach (var node in layers[layer])
            {
                _cross[node] += shift;
            }
        }
    }

    private void Sweep(bool down)
    {
        if (down)
        {
            for (var layer = 1; layer < layers.Length; layer++)
            {
                PlaceLayer(layers[layer], predecessors);
            }
        }
        else
        {
            for (var layer = layers.Length - 2; layer >= 0; layer--)
            {
                PlaceLayer(layers[layer], successors);
            }
        }
    }

    private double Separation(int left, int right)
    {
        // Long edges travel in bundles: two virtual nodes side by side sit half a gap apart.
        var gap = isVirtual[left] && isVirtual[right] ? nodeSpacing / 2 : nodeSpacing;
        return ((crossSizes[left] + crossSizes[right]) / 2) + gap;
    }

    private void PlaceLayer(List<int> members, List<List<int>> neighbours)
    {
        if (members.Count == 0)
        {
            return;
        }

        CollectAims(members, neighbours);
        PoolAdjacentViolators();
        AssignBlocks(members);
    }

    /// <summary>The aim of each member: the median of its neighbours, in the shifted space where the order constraint becomes plain monotonicity.</summary>
    private void CollectAims(List<int> members, List<List<int>> neighbours)
    {
        _aims.Clear();
        var offset = 0d;
        for (var place = 0; place < members.Count; place++)
        {
            var node = members[place];
            if (place > 0)
            {
                offset += Separation(members[place - 1], node);
            }

            _aims.Add(MedianAim(node, neighbours[node]) - offset);
        }
    }

    /// <summary>The median position of the neighbours of <paramref name="node"/>, or its own position when it has none.</summary>
    private double MedianAim(int node, List<int> neighbours)
    {
        if (neighbours.Count == 0)
        {
            return _cross[node];
        }

        _around.Clear();
        foreach (var other in neighbours)
        {
            _around.Add(_cross[other]);
        }

        _around.Sort();
        var middle = _around.Count / 2;
        return _around.Count % 2 == 1 ? _around[middle] : (_around[middle - 1] + _around[middle]) / 2;
    }

    /// <summary>Merges neighbouring aims that run against the order into blocks of equal mean.</summary>
    private void PoolAdjacentViolators()
    {
        _blocks.Clear();
        foreach (var aim in _aims)
        {
            _blocks.Add((aim, 1));
            while (_blocks.Count > 1 && _blocks[^2].Sum / _blocks[^2].Count > _blocks[^1].Sum / _blocks[^1].Count)
            {
                var last = _blocks[^1];
                var before = _blocks[^2];
                _blocks.RemoveAt(_blocks.Count - 1);
                _blocks[^1] = (before.Sum + last.Sum, before.Count + last.Count);
            }
        }
    }

    private void AssignBlocks(List<int> members)
    {
        var at = 0;
        var offset = 0d;
        foreach (var (sum, count) in _blocks)
        {
            var value = sum / count;
            for (var member = 0; member < count; member++, at++)
            {
                if (at > 0)
                {
                    offset += Separation(members[at - 1], members[at]);
                }

                _cross[members[at]] = value + offset;
            }
        }
    }
}
