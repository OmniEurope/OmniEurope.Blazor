using System.Text.Json;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The parts of a stored mind map graph as <see cref="MindMapJson"/> reads and writes them; the
/// public document builds itself from them and hands them back to be written.
/// </summary>
internal sealed record MindMapGraph(
    string? RootId,
    IReadOnlyList<OmniMindMapNode> Nodes,
    IReadOnlyList<OmniMindMapEdge> Edges,
    IReadOnlyList<OmniMindMapNote> Notes,
    IReadOnlyDictionary<string, JsonElement> AdditionalProperties);
