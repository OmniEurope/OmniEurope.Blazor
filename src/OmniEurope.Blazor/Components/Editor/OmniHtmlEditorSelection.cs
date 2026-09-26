using System.Text.Json;

namespace OmniEurope.Blazor.Components;

/// <summary>Where the caret or the selection of an <see cref="OmniHtmlEditor"/> is, in the visual face.</summary>
/// <param name="IsCollapsed">Whether the selection is a caret, with nothing selected.</param>
/// <param name="Ancestors">
/// The elements holding the start of the selection, innermost first, up to the editing surface
/// itself, which is not included. Empty when the caret sits directly in the surface.
/// </param>
public sealed record OmniHtmlEditorSelection(bool IsCollapsed, IReadOnlyList<OmniHtmlEditorSelectionNode> Ancestors)
{
    /// <summary>The innermost ancestor with the given tag name, or null.</summary>
    public OmniHtmlEditorSelectionNode? Closest(string tagName) =>
        Ancestors.FirstOrDefault(node => string.Equals(node.TagName, tagName, StringComparison.OrdinalIgnoreCase));

    /// <summary>The innermost ancestor carrying the given class, or null.</summary>
    public OmniHtmlEditorSelectionNode? ClosestWithClass(string cssClass) =>
        Ancestors.FirstOrDefault(node => node.CssClasses.Contains(cssClass, StringComparer.Ordinal));

    /// <summary>
    /// Reads what <c>omni-html-editor.js</c> reports: <c>{"collapsed":true,"ancestors":[{"tag":"p","classes":[],"data":{}}]}</c>.
    /// Anything malformed gives a collapsed selection with no ancestor rather than an exception.
    /// </summary>
    internal static OmniHtmlEditorSelection Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new(true, []);
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var collapsed = !root.TryGetProperty("collapsed", out var flag) || flag.ValueKind != JsonValueKind.False;
            var ancestors = new List<OmniHtmlEditorSelectionNode>();
            if (root.TryGetProperty("ancestors", out var list) && list.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in list.EnumerateArray())
                {
                    if (entry.ValueKind != JsonValueKind.Object || !entry.TryGetProperty("tag", out var tag) || tag.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var classes = entry.TryGetProperty("classes", out var names) && names.ValueKind == JsonValueKind.Array
                        ? names.EnumerateArray().Where(name => name.ValueKind == JsonValueKind.String).Select(name => name.GetString()!).ToArray()
                        : [];
                    var data = new Dictionary<string, string>(StringComparer.Ordinal);
                    if (entry.TryGetProperty("data", out var attributes) && attributes.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var attribute in attributes.EnumerateObject().Where(attribute => attribute.Value.ValueKind == JsonValueKind.String))
                        {
                            data[attribute.Name] = attribute.Value.GetString()!;
                        }
                    }

                    ancestors.Add(new(tag.GetString()!, classes, data));
                }
            }

            return new(collapsed, ancestors);
        }
        catch (JsonException)
        {
            return new(true, []);
        }
    }
}

/// <summary>One element around the selection of an <see cref="OmniHtmlEditor"/>.</summary>
/// <param name="TagName">The tag name, in lower case (<c>p</c>, <c>aside</c>, <c>td</c>).</param>
/// <param name="CssClasses">The classes of the element, in document order.</param>
/// <param name="DataAttributes">The <c>data-*</c> attributes, keyed by their full name (<c>data-eid</c>).</param>
public sealed record OmniHtmlEditorSelectionNode(
    string TagName,
    IReadOnlyList<string> CssClasses,
    IReadOnlyDictionary<string, string> DataAttributes);
