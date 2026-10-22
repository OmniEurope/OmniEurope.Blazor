namespace OmniEurope.Blazor.Components;

/// <summary>
/// One side of the undo history of an <see cref="OmniHtmlEditor"/>: documents, the latest on top, at most
/// <see cref="Capacity"/> of them. Each entry is a whole document, so an unbounded history grows with every
/// pause in typing; the oldest entry is dropped to make room.
/// </summary>
internal sealed class HtmlEditorHistory
{
    /// <summary>The most documents kept on one side.</summary>
    internal const int Capacity = 100;

    private readonly List<string> _documents = [];

    internal int Count => _documents.Count;

    internal void Push(string document)
    {
        if (_documents.Count == Capacity)
        {
            _documents.RemoveAt(0);
        }

        _documents.Add(document);
    }

    internal bool TryPop(out string document)
    {
        if (_documents.Count == 0)
        {
            document = string.Empty;
            return false;
        }

        document = _documents[^1];
        _documents.RemoveAt(_documents.Count - 1);
        return true;
    }

    internal void Clear() => _documents.Clear();
}
