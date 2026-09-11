using System.Text.Json;

namespace OmniEurope.Blazor.Components;

/// <summary>A short note drawn beside the node it is attached to.</summary>
public sealed record OmniMindMapNote
{
    /// <summary>Identifier of the node the note belongs to.</summary>
    public required string AttachedTo { get; init; }

    /// <summary>The text of the note.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>Properties of the stored note this model does not know.</summary>
    public IReadOnlyDictionary<string, JsonElement> AdditionalProperties { get; init; } =
        OmniMindMapDocument.NoAdditionalProperties;
}
