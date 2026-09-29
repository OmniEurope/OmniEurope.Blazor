using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The sizes of the texts an <see cref="OmniMindMap"/> draws: measured by the browser when it has
/// measured the same text, estimated otherwise, and the node boxes that follow from them.
/// </summary>
internal sealed class MindMapLabelSizes(MindMapModel model)
{
    private readonly Dictionary<string, MindMapMeasuredLabel> _measured = new(StringComparer.Ordinal);

    /// <summary>Whether the drawn texts changed since they were last measured.</summary>
    public bool Pending { get; set; } = true;

    /// <summary>
    /// Keeps the browser's measurements of texts still drawn as measured; returns whether any size
    /// changed by more than half a unit.
    /// </summary>
    public bool Apply(IEnumerable<MindMapMeasurement> results)
    {
        var changed = false;
        foreach (var result in results)
        {
            var signature = SignatureOf(result.Key);
            if (signature is null || !double.IsFinite(result.Width) || !double.IsFinite(result.Height))
            {
                continue;
            }

            if (!_measured.TryGetValue(result.Key, out var known)
                || !string.Equals(known.Signature, signature, StringComparison.Ordinal)
                || Math.Abs(known.Width - result.Width) > 0.5
                || Math.Abs(known.Height - result.Height) > 0.5)
            {
                _measured[result.Key] = new MindMapMeasuredLabel(signature, result.Width, result.Height);
                changed = true;
            }
        }

        return changed;
    }

    /// <summary>
    /// The box of a node: its fixed size when it has one, otherwise the label plus padding, never
    /// narrower than 80 units.
    /// </summary>
    public (double Width, double Height) SizeOf(OmniMindMapNode node)
    {
        var key = NodeKey(node.Id);
        var (textWidth, textHeight) = _measured.TryGetValue(key, out var measured)
            && string.Equals(measured.Signature, LabelSignature(node), StringComparison.Ordinal)
                ? (measured.Width, measured.Height)
                : MindMapGeometry.EstimateText(node.Label, FontSizeOf(node), node.Bold);
        var width = node.Width > 0 ? node.Width : Math.Max(textWidth + (MindMapGeometry.PaddingX * 2), MindMapGeometry.MinimumAutoWidth);
        var height = node.Height > 0 ? node.Height : textHeight + (MindMapGeometry.PaddingY * 2);
        return (width, height);
    }

    /// <summary>The size of the text of a note.</summary>
    public (double Width, double Height) NoteSize((int Index, OmniMindMapNote Note) note)
    {
        var key = NoteKey(note.Index);
        return _measured.TryGetValue(key, out var measured) && string.Equals(measured.Signature, note.Note.Text, StringComparison.Ordinal)
            ? (measured.Width, measured.Height)
            : MindMapGeometry.EstimateText(note.Note.Text, 12, bold: false);
    }

    /// <summary>The font size a node's label is drawn at.</summary>
    public static int FontSizeOf(OmniMindMapNode node) => node.FontSize > 0 ? node.FontSize : OmniMindMapNode.DefaultFontSize;

    /// <summary>The measurement key of a node's label.</summary>
    public static string NodeKey(string nodeId) => $"n:{nodeId}";

    /// <summary>The measurement key of a note, by its index in the document.</summary>
    public static string NoteKey(int index) => string.Create(CultureInfo.InvariantCulture, $"note:{index}");

    private static string LabelSignature(OmniMindMapNode node) =>
        string.Create(CultureInfo.InvariantCulture, $"{node.Label}{FontSizeOf(node)}{node.Bold}{node.Italic}");

    private string? SignatureOf(string key)
    {
        if (key.StartsWith("n:", StringComparison.Ordinal))
        {
            return model.TryGet(key[2..], out var node) ? LabelSignature(node) : null;
        }

        if (key.StartsWith("note:", StringComparison.Ordinal)
            && int.TryParse(key.AsSpan(5), NumberStyles.None, CultureInfo.InvariantCulture, out var index)
            && index < model.Document.Notes.Count)
        {
            return model.Document.Notes[index].Text;
        }

        return null;
    }
}
