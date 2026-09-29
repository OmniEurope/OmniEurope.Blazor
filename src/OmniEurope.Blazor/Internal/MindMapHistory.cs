using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The undo and redo history of an <see cref="OmniMindMap"/>: the documents it went through, the
/// current one last, at most <see cref="OmniMindMap.MaxHistory"/> of them.
/// </summary>
internal sealed class MindMapHistory
{
    private readonly List<OmniMindMapDocument> _undo = [];
    private readonly List<OmniMindMapDocument> _redo = [];

    /// <summary>Whether a document precedes the current one.</summary>
    public bool CanUndo => _undo.Count > 1;

    /// <summary>Whether an undone document can be brought back.</summary>
    public bool CanRedo => _redo.Count > 0;

    /// <summary>Starts a new history at this document.</summary>
    public void Reset(OmniMindMapDocument start)
    {
        _undo.Clear();
        _redo.Clear();
        _undo.Add(start);
    }

    /// <summary>
    /// Records a new current document, unless it already is the current one: the redo history is
    /// dropped and the oldest documents beyond the limit are forgotten.
    /// </summary>
    public void Record(OmniMindMapDocument next)
    {
        if (_undo.Count != 0 && ReferenceEquals(_undo[^1], next))
        {
            return;
        }

        _undo.Add(next);
        _redo.Clear();
        while (_undo.Count > OmniMindMap.MaxHistory)
        {
            _undo.RemoveAt(0);
        }
    }

    /// <summary>Steps back; returns the document that is current again. Requires <see cref="CanUndo"/>.</summary>
    public OmniMindMapDocument Undo()
    {
        _redo.Add(_undo[^1]);
        _undo.RemoveAt(_undo.Count - 1);
        return _undo[^1];
    }

    /// <summary>Steps forward; returns the document brought back. Requires <see cref="CanRedo"/>.</summary>
    public OmniMindMapDocument Redo()
    {
        var snapshot = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        _undo.Add(snapshot);
        return snapshot;
    }
}
