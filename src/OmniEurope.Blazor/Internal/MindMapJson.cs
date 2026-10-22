using System.Text.Json;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Reads and writes the stored mind map graph by hand rather than through reflection: the property
/// order of the format is part of what a consumer compares, unknown properties must survive, and
/// the package stays free of serializer metadata that trimming would have to keep.
/// </summary>
internal static class MindMapJson
{
    /// <summary>The most nodes, links or notes a stored graph may hold; a map past it could not be laid out or drawn.</summary>
    public const int MaximumItems = 50_000;

    // The original editor wrote its graph with JSON.stringify, which leaves accented letters and
    // apostrophes as they are. Writing them the same way keeps a document that went through this
    // model byte for byte what that editor stored, so a host comparing stored text to detect its own
    // echo still can. The output is JSON data for storage; it is never inserted into markup.
    private static readonly JsonWriterOptions WriterOptions = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private static readonly string[] DocumentKeys = ["rootId", "nodes", "edges", "notes"];
    private static readonly string[] NodeKeys = ["id", "label", "group", "x", "y", "fontSize", "bold", "italic", "nodeWidth", "nodeHeight"];
    private static readonly string[] EdgeKeys = ["from", "to"];
    private static readonly string[] NoteKeys = ["attachedTo", "text"];

    public static OmniMindMapDocument Read(string json)
    {
        using var parsed = JsonDocument.Parse(json);
        var root = parsed.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("A mind map document must be a JSON object.");
        }

        return new OmniMindMapDocument
        {
            RootId = String(root, "rootId", null),
            Nodes = [.. Items(root, "nodes").Select(ReadNode).OfType<OmniMindMapNode>()],
            Edges = [.. Items(root, "edges").Select(ReadEdge).OfType<OmniMindMapEdge>()],
            Notes = [.. Items(root, "notes").Select(ReadNote).OfType<OmniMindMapNote>()],
            AdditionalProperties = Extra(root, DocumentKeys)
        };
    }

    public static string Write(OmniMindMapDocument document)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, WriterOptions))
        {
            writer.WriteStartObject();
            if (document.RootId is null)
            {
                writer.WriteNull("rootId");
            }
            else
            {
                writer.WriteString("rootId", document.RootId);
            }

            writer.WriteStartArray("nodes");
            foreach (var node in document.Nodes)
            {
                writer.WriteStartObject();
                writer.WriteString("id", node.Id);
                writer.WriteString("label", node.Label);
                writer.WriteString("group", node.Group);
                writer.WriteNumber("x", node.X);
                writer.WriteNumber("y", node.Y);
                writer.WriteNumber("fontSize", node.FontSize);
                writer.WriteBoolean("bold", node.Bold);
                writer.WriteBoolean("italic", node.Italic);
                writer.WriteNumber("nodeWidth", node.Width);
                writer.WriteNumber("nodeHeight", node.Height);
                WriteExtra(writer, node.AdditionalProperties);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteStartArray("edges");
            foreach (var edge in document.Edges)
            {
                writer.WriteStartObject();
                writer.WriteString("from", edge.From);
                writer.WriteString("to", edge.To);
                WriteExtra(writer, edge.AdditionalProperties);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteStartArray("notes");
            foreach (var note in document.Notes)
            {
                writer.WriteStartObject();
                writer.WriteString("attachedTo", note.AttachedTo);
                writer.WriteString("text", note.Text);
                WriteExtra(writer, note.AdditionalProperties);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            WriteExtra(writer, document.AdditionalProperties);
            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static OmniMindMapNode? ReadNode(JsonElement element)
    {
        var id = String(element, "id", null);
        if (id is null)
        {
            return null;
        }

        return new OmniMindMapNode
        {
            Id = id,
            Label = String(element, "label", null) ?? string.Empty,
            Group = String(element, "group", null) is { Length: > 0 } group ? group : OmniMindMapGroups.Root,
            X = Number(element, "x"),
            Y = Number(element, "y"),
            FontSize = Integer(element, "fontSize", OmniMindMapNode.DefaultFontSize),
            Bold = Boolean(element, "bold"),
            Italic = Boolean(element, "italic"),
            Width = Integer(element, "nodeWidth", 0),
            Height = Integer(element, "nodeHeight", 0),
            AdditionalProperties = Extra(element, NodeKeys)
        };
    }

    private static OmniMindMapEdge? ReadEdge(JsonElement element)
    {
        var from = String(element, "from", null);
        var to = String(element, "to", null);
        return from is null || to is null
            ? null
            : new OmniMindMapEdge { From = from, To = to, AdditionalProperties = Extra(element, EdgeKeys) };
    }

    private static OmniMindMapNote? ReadNote(JsonElement element)
    {
        var attachedTo = String(element, "attachedTo", null);
        return attachedTo is null
            ? null
            : new OmniMindMapNote
            {
                AttachedTo = attachedTo,
                Text = String(element, "text", null) ?? string.Empty,
                AdditionalProperties = Extra(element, NoteKeys)
            };
    }

    private static IEnumerable<JsonElement> Items(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        if (array.GetArrayLength() > MaximumItems)
        {
            throw new JsonException($"A mind map document may hold at most {MaximumItems} {name}.");
        }

        return array.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.Object).ToArray();
    }

    // Only ever read on objects: the root is checked by Read, the items are filtered by Items.
    private static string? String(JsonElement element, string name, string? fallback) =>
        element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : fallback;

    private static double Number(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number)
        {
            return 0;
        }

        return Finite(value, name);
    }

    private static int Integer(JsonElement element, string name, int fallback)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number)
        {
            return fallback;
        }

        if (value.TryGetInt32(out var whole))
        {
            return whole;
        }

        // A number a double can hold but an int cannot (1e30) is not a size: the default stands.
        var number = Math.Round(Finite(value, name));
        return number is >= int.MinValue and <= int.MaxValue ? (int)number : fallback;
    }

    // A number past the range of a double reads as an infinity, which no map position or size can be: the
    // documented failure is a JsonException, not an infinite coordinate that breaks the layout.
    private static double Finite(JsonElement value, string name)
    {
        var number = value.GetDouble();
        return double.IsFinite(number)
            ? number
            : throw new JsonException($"The value of '{name}' is not a number a map can hold.");
    }

    private static bool Boolean(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;

    private static IReadOnlyDictionary<string, JsonElement> Extra(JsonElement element, string[] known)
    {
        Dictionary<string, JsonElement>? extra = null;
        foreach (var property in element.EnumerateObject())
        {
            if (Array.IndexOf(known, property.Name) < 0)
            {
                extra ??= new Dictionary<string, JsonElement>(StringComparer.Ordinal);
                extra[property.Name] = property.Value.Clone();
            }
        }

        return extra ?? OmniMindMapDocument.NoAdditionalProperties;
    }

    private static void WriteExtra(Utf8JsonWriter writer, IReadOnlyDictionary<string, JsonElement> extra)
    {
        foreach (var (name, value) in extra)
        {
            writer.WritePropertyName(name);
            value.WriteTo(writer);
        }
    }
}
