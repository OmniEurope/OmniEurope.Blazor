using System.Text.Json;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The shared empty set of unknown stored properties, the default of every mind map record that
/// keeps them, so a graph without extra properties allocates no dictionary per item.
/// </summary>
internal static class MindMapExtraProperties
{
    public static readonly IReadOnlyDictionary<string, JsonElement> None =
        new Dictionary<string, JsonElement>(StringComparer.Ordinal);
}
