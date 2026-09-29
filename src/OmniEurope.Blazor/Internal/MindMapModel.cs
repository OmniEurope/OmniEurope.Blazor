using System.Diagnostics.CodeAnalysis;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The document an <see cref="OmniMindMap"/> draws, with the indexes its drawing reads: the nodes
/// that can be drawn (a valid, unique identifier), the links whose two ends are drawn, the notes
/// grouped by node, and the next free <c>node_N</c> number.
/// </summary>
internal sealed class MindMapModel
{
    private readonly Dictionary<string, OmniMindMapNode> _byId = new(StringComparer.Ordinal);
    private readonly List<OmniMindMapNode> _drawable = [];
    private readonly List<(int Index, OmniMindMapNode From, OmniMindMapNode To)> _renderedEdges = [];
    private readonly Dictionary<string, List<(int Index, OmniMindMapNote Note)>> _notesByNode = new(StringComparer.Ordinal);
    private int _nextId = 1;

    /// <summary>The document as it stands.</summary>
    public OmniMindMapDocument Document { get; private set; } = OmniMindMapDocument.Empty;

    /// <summary>The nodes that are drawn, in document order.</summary>
    public IReadOnlyList<OmniMindMapNode> Drawable => _drawable;

    /// <summary>The links whose two ends are drawn, with their index in the document.</summary>
    public IReadOnlyList<(int Index, OmniMindMapNode From, OmniMindMapNode To)> RenderedEdges => _renderedEdges;

    /// <summary>The number the next new node identifier starts from.</summary>
    public int NextNumber => _nextId;

    /// <summary>The root node when it is drawn, otherwise the first drawn node, or null.</summary>
    public OmniMindMapNode? Root =>
        Document.RootId is not null && _byId.TryGetValue(Document.RootId, out var root) ? root : _drawable.FirstOrDefault();

    /// <summary>Whether a node of this identifier is drawn.</summary>
    public bool Contains(string nodeId) => _byId.ContainsKey(nodeId);

    /// <summary>The drawn node of this identifier.</summary>
    public bool TryGet(string nodeId, [NotNullWhen(true)] out OmniMindMapNode? node) => _byId.TryGetValue(nodeId, out node);

    /// <summary>The drawn node of an identifier known to be drawn.</summary>
    public OmniMindMapNode Get(string nodeId) => _byId[nodeId];

    /// <summary>The notes attached to a drawn node, with their index in the document.</summary>
    public IReadOnlyList<(int Index, OmniMindMapNote Note)> NotesOf(string nodeId) =>
        _notesByNode.TryGetValue(nodeId, out var notes) ? notes : [];

    /// <summary>Takes a new document and rebuilds every index from it.</summary>
    public void Set(OmniMindMapDocument document)
    {
        Document = document;
        _drawable.Clear();
        _byId.Clear();
        foreach (var node in document.Nodes)
        {
            if (IsDrawableId(node.Id) && _byId.TryAdd(node.Id, node))
            {
                _drawable.Add(node);
            }
        }

        _renderedEdges.Clear();
        for (var index = 0; index < document.Edges.Count; index++)
        {
            var edge = document.Edges[index];
            if (_byId.TryGetValue(edge.From, out var from) && _byId.TryGetValue(edge.To, out var to))
            {
                _renderedEdges.Add((index, from, to));
            }
        }

        _notesByNode.Clear();
        for (var index = 0; index < document.Notes.Count; index++)
        {
            var note = document.Notes[index];
            if (_byId.ContainsKey(note.AttachedTo))
            {
                if (!_notesByNode.TryGetValue(note.AttachedTo, out var notes))
                {
                    notes = [];
                    _notesByNode[note.AttachedTo] = notes;
                }

                notes.Add((index, note));
            }
        }

        _nextId = Math.Max(_nextId, NextNumericId(document));
    }

    /// <summary>A <c>node_N</c> identifier no node of the document carries.</summary>
    public string TakeNextId()
    {
        string id;
        do
        {
            id = $"node_{_nextId++}";
        }
        while (Document.Nodes.Any(node => string.Equals(node.Id, id, StringComparison.Ordinal)));

        return id;
    }

    /// <summary>The document with every drawn node passed through <paramref name="change"/>.</summary>
    public OmniMindMapDocument ReplaceNodes(Func<OmniMindMapNode, OmniMindMapNode> change) =>
        Document with
        {
            Nodes = [.. Document.Nodes.Select(node => _byId.TryGetValue(node.Id, out var drawn) && ReferenceEquals(drawn, node) ? change(node) : node)]
        };

    /// <summary>
    /// The number after the highest <c>node_N</c> identifier, so that a new node never reuses one.
    /// </summary>
    private static int NextNumericId(OmniMindMapDocument document)
    {
        var highest = 0;
        foreach (var node in document.Nodes)
        {
            if (node.Id.StartsWith("node_", StringComparison.Ordinal)
                && int.TryParse(node.Id.AsSpan(5), NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            {
                highest = Math.Max(highest, number);
            }
        }

        return highest + 1;
    }

    private static bool IsDrawableId(string id) =>
        id.Length is > 0 and <= 64 && id.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-');
}
