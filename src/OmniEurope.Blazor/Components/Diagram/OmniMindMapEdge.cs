using System.Text.Json;

namespace OmniEurope.Blazor.Components;

/// <summary>A link drawn from one node to another.</summary>
/// <remarks>
/// A link whose ends do not both name a node of the document is kept, so the stored graph
/// survives a round trip, but it is not drawn.
/// </remarks>
public sealed record OmniMindMapEdge
{
    /// <summary>Identifier of the node the link starts from.</summary>
    public required string From { get; init; }

    /// <summary>Identifier of the node the link points to.</summary>
    public required string To { get; init; }

    /// <summary>Properties of the stored link this model does not know.</summary>
    public IReadOnlyDictionary<string, JsonElement> AdditionalProperties { get; init; } =
        OmniMindMapDocument.NoAdditionalProperties;
}
