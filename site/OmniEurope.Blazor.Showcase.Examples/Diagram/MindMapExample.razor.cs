using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Diagram;

/// <summary>A mind map whose mode, links, toolbar, panel and labels are parameters.</summary>
public partial class MindMapExample
{
    /// <summary>The map being edited.</summary>
    [Parameter]
    public OmniMindMapDocument Document { get; set; } = MindMapSample.Create();

    /// <summary>Raised with the new map after each change.</summary>
    [Parameter]
    public EventCallback<OmniMindMapDocument> DocumentChanged { get; set; }

    /// <summary>The pan and zoom of the canvas; null fits the map to the canvas.</summary>
    [Parameter]
    public OmniMindMapViewState? ViewState { get; set; }

    /// <summary>Raised with the new view after each pan or zoom.</summary>
    [Parameter]
    public EventCallback<OmniMindMapViewState> ViewStateChanged { get; set; }

    /// <summary>Raised with the selected node, or null when the selection is cleared.</summary>
    [Parameter]
    public EventCallback<OmniMindMapNode?> OnNodeSelect { get; set; }

    /// <summary>Whether nodes can only be selected, with the map panned and zoomed.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Whether each link starts at the edge of a node and ends with an arrow on the edge of the other.</summary>
    [Parameter]
    public bool Directed { get; set; }

    /// <summary>Whether the action bar is shown above the canvas.</summary>
    [Parameter]
    public bool ShowToolbar { get; set; } = true;

    /// <summary>Whether the properties panel of the selected node is shown.</summary>
    [Parameter]
    public bool ShowPanel { get; set; } = true;

    /// <summary>Whether the map renames three actions for its own vocabulary.</summary>
    [Parameter]
    public bool UseCustomLabels { get; set; }

    // The example keeps what the map reports: when the host binds nothing, the next render would otherwise
    // hand the map back its old document and undo the edit.
    private async Task OnDocumentChanged(OmniMindMapDocument document)
    {
        Document = document;
        await DocumentChanged.InvokeAsync(document);
    }

    private async Task OnViewStateChanged(OmniMindMapViewState view)
    {
        ViewState = view;
        await ViewStateChanged.InvokeAsync(view);
    }
}
