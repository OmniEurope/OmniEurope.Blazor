namespace OmniEurope.Blazor.Components;

/// <summary>
/// Which data labels are drawn when they would overlap, the rule of the category axis: a label that
/// would overlap the previous one kept is masked, the last one is always kept, and a kept label that
/// would overlap it is masked in its place. Boxes are estimated in chart units, so the rule errs
/// towards more room.
/// </summary>
internal static class DataLabelThinning
{
    /// <summary>The estimated box of one label, in chart units.</summary>
    internal readonly record struct Box(double Left, double Right, double Top, double Bottom)
    {
        // A small gap keeps two labels that only touch apart.
        private const double Gap = 0.5;

        internal bool Overlaps(Box other) =>
            Left < other.Right + Gap && other.Left < Right + Gap && Top < other.Bottom && other.Top < Bottom;
    }

    /// <summary>The box of a label written at <paramref name="x"/>, <paramref name="y"/> with its SVG anchor and baseline.</summary>
    internal static Box BoxOf(double x, double y, string? anchor, string? baseline, double width, double height)
    {
        var left = anchor switch
        {
            "middle" => x - (width / 2),
            "end" => x - width,
            _ => x
        };
        var top = baseline switch
        {
            "hanging" => y,
            "central" => y - (height / 2),
            _ => y - height
        };
        return new Box(left, left + width, top, top + height);
    }

    /// <summary>Whether each label is drawn; a null box (a point out of the range shown) takes no part and stays drawn.</summary>
    internal static bool[] Keep(IReadOnlyList<Box?> boxes)
    {
        var visible = new bool[boxes.Count];
        var kept = new List<int>();
        for (var index = 0; index < boxes.Count; index++)
        {
            if (boxes[index] is not { } box)
            {
                visible[index] = true;
                continue;
            }

            if (kept.Count == 0 || !boxes[kept[^1]]!.Value.Overlaps(box))
            {
                kept.Add(index);
                visible[index] = true;
            }
        }

        // The last label is always kept: the ones before it that it would overlap give way.
        var last = LastIndex(boxes);
        if (last >= 0 && !visible[last])
        {
            while (kept.Count > 0 && boxes[kept[^1]]!.Value.Overlaps(boxes[last]!.Value))
            {
                visible[kept[^1]] = false;
                kept.RemoveAt(kept.Count - 1);
            }

            visible[last] = true;
        }

        return visible;
    }

    private static int LastIndex(IReadOnlyList<Box?> boxes)
    {
        for (var index = boxes.Count - 1; index >= 0; index--)
        {
            if (boxes[index] is not null)
            {
                return index;
            }
        }

        return -1;
    }
}
