namespace OmniEurope.Blazor.Components;

/// <summary>
/// The categories one <see cref="OmniChart"/> shows, set by an <see cref="OmniRangeNavigator"/>: a first
/// and a last index, both included, counted like the category axis and the data table (index <c>i</c>
/// is the point of index <c>i</c> of every series). Without a window every category is shown.
/// </summary>
internal sealed class OmniChartRange
{
    private object? _owner;

    /// <summary>The first and last categories shown; null while every category is shown.</summary>
    internal (int First, int Last)? Window { get; private set; }

    /// <summary>The index the window starts at, zero without one.</summary>
    internal int First => Window?.First ?? 0;

    /// <summary>Shows categories <paramref name="first"/> to <paramref name="last"/>; false when nothing changed.</summary>
    internal bool Set(object owner, int first, int last)
    {
        if (ReferenceEquals(_owner, owner) && Window == (first, last))
        {
            return false;
        }

        _owner = owner;
        Window = (first, last);
        return true;
    }

    /// <summary>Shows every category again when <paramref name="owner"/> set the window; false otherwise.</summary>
    internal bool Clear(object owner)
    {
        if (!ReferenceEquals(_owner, owner))
        {
            return false;
        }

        _owner = null;
        Window = null;
        return true;
    }

    /// <summary>Whether category <paramref name="index"/> is shown: always without a window.</summary>
    internal bool Contains(int index) => Window is not { } window || (index >= window.First && index <= window.Last);

    /// <summary>The points of <paramref name="data"/> whose index is shown.</summary>
    internal IEnumerable<OmniChartPoint> Points(IReadOnlyList<OmniChartPoint> data) =>
        Window is null ? data : data.Where((_, index) => Contains(index));

    /// <summary>
    /// Category <paramref name="index"/> of <paramref name="count"/> as the axis sees it: its rank in the
    /// window and the size of the window, or unchanged without a window.
    /// </summary>
    internal (int Index, int Count) Local(int index, int count) =>
        Window is { } window ? (index - window.First, window.Last - window.First + 1) : (index, count);

    /// <summary>The first and last of <paramref name="count"/> categories that are shown (last below first when none is).</summary>
    internal (int First, int Last) Bounds(int count) =>
        (Math.Max(0, Window?.First ?? 0), Math.Min(count - 1, Window?.Last ?? count - 1));

    /// <summary>The sum of the values of <paramref name="series"/> at each of <paramref name="count"/> categories.</summary>
    internal static IReadOnlyList<double> Totals(IEnumerable<IReadOnlyList<OmniChartPoint>> series, int count)
    {
        var totals = new double[count];
        foreach (var data in series)
        {
            for (var index = 0; index < data.Count && index < count; index++)
            {
                totals[index] += data[index].Y;
            }
        }

        return totals;
    }

    /// <summary>
    /// One category every <paramref name="step"/> from <paramref name="first"/> to <paramref name="last"/>,
    /// both always kept: the label of the last replaces the one before it when the two would be closer
    /// than a step.
    /// </summary>
    internal static IReadOnlyList<int> Thin(int first, int last, int step)
    {
        if (step <= 1)
        {
            return [.. Enumerable.Range(first, last - first + 1)];
        }

        var visible = new List<int>();
        for (var index = first; index < last; index += step)
        {
            visible.Add(index);
        }

        if (visible.Count > 1 && last - visible[^1] < step)
        {
            visible.RemoveAt(visible.Count - 1);
        }

        visible.Add(last);
        return visible;
    }
}
