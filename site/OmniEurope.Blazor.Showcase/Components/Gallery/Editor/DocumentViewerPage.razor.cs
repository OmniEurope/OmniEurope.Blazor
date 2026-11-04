using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Editor;

/// <summary>The document viewer page: a picture page and an HTML page with fields, the fit, the toolbar and the highlight.</summary>
public partial class DocumentViewerPage
{
    [Inject]
    private IStringLocalizer<EditorStrings> Text { get; set; } = default!;

    private OmniDocumentFit Fit { get; set; } = OmniDocumentFit.Width;

    private double Zoom { get; set; } = 1;

    private bool ShowToolbar { get; set; } = true;

    private bool HighlightFields { get; set; } = true;

    private IReadOnlyList<OmniOption<OmniDocumentFit>> FitOptions =>
    [
        new(OmniDocumentFit.Width, Text["DocumentViewerFitWidth"]),
        new(OmniDocumentFit.None, Text["DocumentViewerFitNone"])
    ];
}
