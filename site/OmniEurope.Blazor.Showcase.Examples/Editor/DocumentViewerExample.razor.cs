using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>A read-only document viewer with a picture page and an HTML page whose fields report their tag.</summary>
public partial class DocumentViewerExample
{
    /// <summary>The zoom: 1 is 100 %. Written back by the toolbar and by the width fit.</summary>
    [Parameter]
    public double Zoom { get; set; } = 1;

    /// <summary>Raised when the zoom changes.</summary>
    [Parameter]
    public EventCallback<double> ZoomChanged { get; set; }

    /// <summary>Whether the pages follow the width of the viewer or keep the zoom.</summary>
    [Parameter]
    public OmniDocumentFit Fit { get; set; } = OmniDocumentFit.Width;

    /// <summary>Raised when the toolbar changes the fit.</summary>
    [Parameter]
    public EventCallback<OmniDocumentFit> FitChanged { get; set; }

    /// <summary>Shows the toolbar: zoom out, zoom in, fit width and the page indicator.</summary>
    [Parameter]
    public bool ShowToolbar { get; set; } = true;

    /// <summary>Highlights the fields of the HTML page.</summary>
    [Parameter]
    public bool HighlightFields { get; set; } = true;

    private string LastField { get; set; } = "none yet";

    private Task OnZoomChangedAsync(double zoom)
    {
        Zoom = zoom;
        return ZoomChanged.InvokeAsync(zoom);
    }

    private Task OnFitChangedAsync(OmniDocumentFit fit)
    {
        Fit = fit;
        return FitChanged.InvokeAsync(fit);
    }

    private Task OnFieldClickAsync(string tag)
    {
        LastField = tag;
        return Task.CompletedTask;
    }
}
