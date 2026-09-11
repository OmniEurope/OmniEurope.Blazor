using System.Text.Json;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A mind map: its nodes, the links between them, the notes attached to them, and which node is
/// the centre of the map.
/// </summary>
/// <remarks>
/// The document is immutable. <see cref="OmniMindMap"/> never edits the instance it was given: each
/// change produces a new document, raised through <see cref="OmniMindMap.DocumentChanged"/>, which
/// is what makes undo a matter of keeping the previous instances.
/// <para>
/// <see cref="FromJson"/> and <see cref="ToJson"/> read and write the graph format the Pronoia
/// editor stores: <c>{"rootId", "nodes", "edges", "notes"}</c>. Properties the model does not know
/// are kept in <c>AdditionalProperties</c> and written back, so the round trip loses nothing.
/// </para>
/// </remarks>
public sealed record OmniMindMapDocument
{
    internal static readonly IReadOnlyDictionary<string, JsonElement> NoAdditionalProperties =
        new Dictionary<string, JsonElement>(StringComparer.Ordinal);

    /// <summary>An empty map.</summary>
    public static OmniMindMapDocument Empty { get; } = new();

    /// <summary>Identifier of the central node, used as the origin of the automatic layout.</summary>
    public string? RootId { get; init; }

    /// <summary>The nodes, in drawing order.</summary>
    public IReadOnlyList<OmniMindMapNode> Nodes { get; init; } = [];

    /// <summary>The links between nodes.</summary>
    public IReadOnlyList<OmniMindMapEdge> Edges { get; init; } = [];

    /// <summary>The notes attached to nodes.</summary>
    public IReadOnlyList<OmniMindMapNote> Notes { get; init; } = [];

    /// <summary>Top-level properties of the stored graph this model does not know.</summary>
    public IReadOnlyDictionary<string, JsonElement> AdditionalProperties { get; init; } = NoAdditionalProperties;

    /// <summary>
    /// Reads a stored graph. Missing values take the defaults the original editor gave them (empty
    /// label, <see cref="OmniMindMapGroups.Root"/> group, position 0, font size 14); nodes without a
    /// string identifier and links without two string ends are dropped, as that editor dropped them.
    /// </summary>
    /// <exception cref="JsonException">The text is not a JSON object.</exception>
    public static OmniMindMapDocument FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return MindMapJson.Read(json);
    }

    /// <summary>Writes the graph in the stored format, compact and in the format's property order.</summary>
    public string ToJson() => MindMapJson.Write(this);
}
