namespace OmniEurope.Blazor.Components;

/// <summary>
/// The colour groups a node can take. Each group is drawn from the
/// <c>--omni-mindmap-{group}-fill</c>, <c>-stroke</c> and <c>-text</c> tokens of the stylesheet, so
/// a theme repaints them without touching the component.
/// </summary>
public static class OmniMindMapGroups
{
    /// <summary>White box, the default and the colour of the central node.</summary>
    public const string Root = "root";

    /// <summary>Green.</summary>
    public const string Green = "green";

    /// <summary>Blue.</summary>
    public const string Blue = "blue";

    /// <summary>Yellow.</summary>
    public const string Yellow = "yellow";

    /// <summary>Red.</summary>
    public const string Red = "red";

    /// <summary>Pink.</summary>
    public const string Pink = "pink";

    /// <summary>Orange.</summary>
    public const string Orange = "orange";

    /// <summary>Purple.</summary>
    public const string Purple = "purple";

    /// <summary>Teal.</summary>
    public const string Teal = "teal";

    /// <summary>Indigo.</summary>
    public const string Indigo = "indigo";

    /// <summary>Gray.</summary>
    public const string Gray = "gray";

    /// <summary>
    /// The groups offered to the reader, in the order the colour pickers present them.
    /// </summary>
    public static IReadOnlyList<string> Palette { get; } =
        [Green, Blue, Yellow, Red, Purple, Orange, Pink, Teal, Indigo, Root, Gray];

    /// <summary>
    /// The groups a node created by double-clicking the canvas cycles through, so that neighbouring
    /// new nodes do not all come out the same colour.
    /// </summary>
    internal static IReadOnlyList<string> Rotation { get; } =
        [Green, Blue, Yellow, Red, Pink, Orange, Purple, Teal, Indigo, Gray];

    /// <summary>The group a node is drawn with: its own when known, <see cref="Root"/> otherwise.</summary>
    internal static string Resolve(string? group) =>
        group is not null && Palette.Contains(group, StringComparer.Ordinal) ? group : Root;
}
