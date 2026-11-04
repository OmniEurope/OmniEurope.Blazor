namespace OmniEurope.Blazor.Components;

/// <summary>How an <see cref="OmniDocumentViewer"/> sizes its pages to the width it has.</summary>
public enum OmniDocumentFit
{
    /// <summary>The pages are drawn at the zoom the reader chose, whatever the width of the viewer.</summary>
    None,

    /// <summary>
    /// The zoom follows the width of the viewer so that the widest page fills it exactly, again each time the
    /// viewer is resized. A zoom chosen by the reader (zoom in, zoom out) ends it.
    /// </summary>
    Width
}
