namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Offset index for a virtualized row set. Row heights start from an estimate and are replaced
/// by real measurements as rows are rendered, so rows of different heights stay correctly placed.
/// Prefix sums are held in a Fenwick tree, which keeps offset lookups and measurement updates
/// logarithmic even for very large row counts.
/// </summary>
internal sealed class GridVirtualWindow
{
    private const double MeasurementTolerance = 0.5d;
    private const int FallbackViewportRows = 12;

    private double[] _heights = [];
    private bool[] _measured = [];
    private double[] _tree = [];
    private int _count;
    private double _estimate = 32d;
    private int _highestBit;

    internal int Count => _count;

    internal double EstimatedRowHeight => _estimate;

    internal double TotalHeight => (_count * _estimate) + PrefixDelta(_count);

    internal bool IsMeasured(int index) => index >= 0 && index < _count && _measured[index];

    internal double HeightOf(int index) => IsMeasured(index) ? _heights[index] : _estimate;

    /// <summary>Sets the row count and the estimate used for rows that have not been measured yet.</summary>
    internal void Configure(int count, double estimatedRowHeight)
    {
        var estimate = estimatedRowHeight > 0d ? estimatedRowHeight : 1d;
        var normalized = Math.Max(0, count);
        if (normalized == _count && Math.Abs(estimate - _estimate) <= double.Epsilon)
        {
            return;
        }

        if (normalized != _count)
        {
            Array.Resize(ref _heights, normalized);
            Array.Resize(ref _measured, normalized);
            _count = normalized;
            _highestBit = HighestPowerOfTwo(normalized);
        }

        _estimate = estimate;
        Rebuild();
    }

    /// <summary>Forgets every measurement, for instance after a sort or a filter changed the rows.</summary>
    internal void ResetMeasurements()
    {
        Array.Clear(_heights);
        Array.Clear(_measured);
        Rebuild();
    }

    /// <summary>Records a measured row height. Returns <c>true</c> when the layout actually moved.</summary>
    internal bool Measure(int index, double height)
    {
        if (index < 0 || index >= _count || double.IsNaN(height) || height <= 0d)
        {
            return false;
        }

        var previous = HeightOf(index);
        if (_measured[index] && Math.Abs(previous - height) < MeasurementTolerance)
        {
            return false;
        }

        var delta = height - previous;
        _heights[index] = height;
        _measured[index] = true;
        if (Math.Abs(delta) < double.Epsilon)
        {
            return false;
        }

        Add(index + 1, delta);
        return true;
    }

    /// <summary>Pixel offset of the top edge of <paramref name="index"/> inside the scrolled content.</summary>
    internal double OffsetOf(int index)
    {
        var clamped = Math.Clamp(index, 0, _count);
        return (clamped * _estimate) + PrefixDelta(clamped);
    }

    /// <summary>Index of the row containing <paramref name="offset"/>, i.e. the count of rows entirely above it.</summary>
    internal int IndexAt(double offset)
    {
        if (_count == 0 || offset <= 0d)
        {
            return 0;
        }

        var position = 0;
        var remaining = offset;
        for (var bit = _highestBit; bit > 0; bit >>= 1)
        {
            var candidate = position + bit;
            if (candidate > _count)
            {
                continue;
            }

            var blockHeight = _tree[candidate] + (bit * _estimate);
            if (blockHeight <= remaining)
            {
                position = candidate;
                remaining -= blockHeight;
            }
        }

        return Math.Min(position, Math.Max(0, _count - 1));
    }

    /// <summary>Rows to render for the current scroll position, with the spacer heights framing them.</summary>
    internal GridVirtualRange Compute(double scrollTop, double viewportHeight, int overscan)
    {
        if (_count == 0)
        {
            return new GridVirtualRange(0, 0, 0d, 0d);
        }

        var height = viewportHeight > 0d ? viewportHeight : _estimate * FallbackViewportRows;
        var top = Math.Clamp(double.IsNaN(scrollTop) ? 0d : scrollTop, 0d, Math.Max(0d, TotalHeight - height));
        var safeOverscan = Math.Max(0, overscan);

        var start = Math.Max(0, IndexAt(top) - safeOverscan);
        var end = Math.Min(_count, IndexAt(top + height) + 1 + safeOverscan);
        if (end <= start)
        {
            end = Math.Min(_count, start + 1);
        }

        var topSpacer = OffsetOf(start);
        var bottomSpacer = Math.Max(0d, TotalHeight - OffsetOf(end));
        return new GridVirtualRange(start, end - start, topSpacer, bottomSpacer);
    }

    private void Rebuild()
    {
        _tree = new double[_count + 1];
        for (var index = 0; index < _count; index++)
        {
            if (!_measured[index])
            {
                continue;
            }

            var delta = _heights[index] - _estimate;
            if (Math.Abs(delta) >= double.Epsilon)
            {
                Add(index + 1, delta);
            }
        }
    }

    private void Add(int position, double value)
    {
        for (var index = position; index <= _count; index += index & -index)
        {
            _tree[index] += value;
        }
    }

    private double PrefixDelta(int count)
    {
        var sum = 0d;
        for (var index = count; index > 0; index -= index & -index)
        {
            sum += _tree[index];
        }

        return sum;
    }

    private static int HighestPowerOfTwo(int value)
    {
        var bit = 1;
        while (bit << 1 <= value)
        {
            bit <<= 1;
        }

        return value == 0 ? 0 : bit;
    }
}
