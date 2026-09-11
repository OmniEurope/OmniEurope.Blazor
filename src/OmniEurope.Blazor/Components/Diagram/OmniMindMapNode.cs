using System.Text.Json;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// One node of a mind map: a label in a rounded box, placed by the coordinates of its centre.
/// </summary>
/// <remarks>
/// The members mirror the stored graph format one for one (<c>id</c>, <c>label</c>, <c>group</c>,
/// <c>x</c>, <c>y</c>, <c>fontSize</c>, <c>bold</c>, <c>italic</c>, <c>nodeWidth</c>,
/// <c>nodeHeight</c>), so a document read with <see cref="OmniMindMapDocument.FromJson"/> writes
/// back the same graph. Links between nodes are not held here: they are the document's
/// <see cref="OmniMindMapDocument.Edges"/>.
/// </remarks>
public sealed record OmniMindMapNode
{
    /// <summary>The font size a node gets when the stored graph does not give one.</summary>
    public const int DefaultFontSize = 14;

    /// <summary>
    /// Identifier, unique within the document. Only identifiers made of 1 to 64 letters, digits,
    /// <c>_</c> or <c>-</c> are drawn; any other node is kept in the document but not rendered.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>The text shown in the node.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// The colour group, one of <see cref="OmniMindMapGroups.Palette"/>. An unknown group is kept
    /// as written and drawn with the <see cref="OmniMindMapGroups.Root"/> colours.
    /// </summary>
    public string Group { get; init; } = OmniMindMapGroups.Root;

    /// <summary>Horizontal position of the node centre, in map units.</summary>
    public double X { get; init; }

    /// <summary>Vertical position of the node centre, in map units.</summary>
    public double Y { get; init; }

    /// <summary>Font size of the label, in map units.</summary>
    public int FontSize { get; init; } = DefaultFontSize;

    /// <summary>Whether the label is bold.</summary>
    public bool Bold { get; init; }

    /// <summary>Whether the label is italic.</summary>
    public bool Italic { get; init; }

    /// <summary>Fixed width of the box; 0 sizes it to the label.</summary>
    public int Width { get; init; }

    /// <summary>Fixed height of the box; 0 sizes it to the label.</summary>
    public int Height { get; init; }

    /// <summary>
    /// Properties of the stored node this model does not know, kept so that writing the document
    /// back does not drop them.
    /// </summary>
    public IReadOnlyDictionary<string, JsonElement> AdditionalProperties { get; init; } =
        OmniMindMapDocument.NoAdditionalProperties;
}
