using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>A structured document editor with a service order, and the live change set it reports written below it.</summary>
public partial class StructuredDocumentExample
{
    /// <summary>Turns all editing off: the text, the fields and the drawing buttons.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Offers a drawing under each paragraph.</summary>
    [Parameter]
    public bool AllowDrawings { get; set; } = true;

    private OmniDocument Document { get; set; } = StructuredDocumentSamples.ServiceOrder();

    private OmniDocumentChangeSet? Changes { get; set; }

    private int ChangeCount => Changes is null ? 0 : Changes.Runs.Count + Changes.Fields.Count + Changes.Drawings.Count;

    private string ChangeText => Describe(Changes);

    // The class a run is drawn with, from the opaque key the host put on it.
    private static string? StyleClassOf(string styleKey) => styleKey == StructuredDocumentSamples.EmphasisKey ? "gallery-structured-emphasis" : null;

    private Task OnChangeAsync(OmniDocumentChangeSet changes)
    {
        Changes = changes;
        return Task.CompletedTask;
    }

    private void StartOver()
    {
        Document = StructuredDocumentSamples.ServiceOrder();
        Changes = null;
    }

    // The change set as JSON-like text: a host would apply it to its own document; the PNG shows as its size.
    private static string Describe(OmniDocumentChangeSet? changes)
    {
        var text = new StringBuilder("{\n");
        text.Append("  \"runs\": [").Append(List(changes?.Runs.Select(run => $"{{ \"runId\": {Quote(run.RunId)}, \"text\": {Quote(run.Text)} }}"))).Append("],\n");
        text.Append("  \"fields\": [").Append(List(changes?.Fields.Select(field => $"{{ \"fieldId\": {Quote(field.FieldId)}, \"tag\": {Quote(field.Tag)}, \"value\": {(field.Value is null ? "null" : Quote(field.Value))} }}"))).Append("],\n");
        text.Append("  \"drawings\": [").Append(List(changes?.Drawings.Select(drawing => $"{{ \"blockId\": {Quote(drawing.BlockId)}, \"strokes\": {drawing.Drawing.Strokes.Count}, \"sheetPt\": \"{drawing.Drawing.WidthPt} x {drawing.Drawing.HeightPt}\", \"pngBytes\": {drawing.Png.Length} }}"))).Append("]\n}");
        return text.ToString();
    }

    private static string List(IEnumerable<string>? items)
        => items is null || !items.Any() ? string.Empty : "\n    " + string.Join(",\n    ", items) + "\n  ";

    private static string Quote(string value) => JsonSerializer.Serialize(value);
}
