using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// What the helpers of an <see cref="OmniMindMap"/> (editor, viewport, context menu, keyboard,
/// gestures) need from the component: its parameters, its texts, and the notifications it raises.
/// Taking this rather than the component keeps the helpers from depending on it.
/// </summary>
internal interface IMindMapHost
{
    /// <summary>Whether the map only shows the document, without letting it change.</summary>
    bool ReadOnly { get; }

    /// <summary>The layered layout options, or null for the radial layout around the root.</summary>
    OmniGraphLayoutOptions? LayeredLayout { get; }

    /// <summary>Raised with the new document after every change.</summary>
    EventCallback<OmniMindMapDocument> DocumentChanged { get; }

    /// <summary>Raised with the selected node when the single selection changes.</summary>
    EventCallback<OmniMindMapNode?> OnNodeSelect { get; }

    /// <summary>Raised when the reader pans or zooms.</summary>
    EventCallback<OmniMindMapViewState> ViewStateChanged { get; }

    /// <summary>The label a new node gets.</summary>
    string NewNodeLabel { get; }

    /// <summary>The banner text asking for the target of a link.</summary>
    string LinkSelectTargetLabel { get; }

    /// <summary>The banner text while a link is drawn: pick its source, then its target.</summary>
    string LinkBannerText { get; }

    /// <summary>A localized text of the mind map family.</summary>
    string Text(string name, params object[] arguments);

    /// <summary>Sets the text the live region announces.</summary>
    void Announce(string text);

    /// <summary>Tells the toolbar and the panel of the map that its state changed.</summary>
    void NotifyStateChanged();

    /// <summary>Asks for the selected node to be renamed.</summary>
    Task RequestRenameAsync();

    /// <summary>Pans so the selected node sits in the middle of the canvas.</summary>
    Task CenterOnSelectionAsync();
}
