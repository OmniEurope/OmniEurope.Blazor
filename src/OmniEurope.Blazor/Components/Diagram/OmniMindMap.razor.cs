namespace OmniEurope.Blazor.Components;

/// <summary>
/// An interactive mind map drawn in SVG: nodes are dragged, linked, coloured and renamed, the map
/// is panned and zoomed, and every action has a keyboard path.
/// </summary>
/// <remarks>
/// The document belongs to .NET. Razor renders the whole drawing; <c>omni-mindmap.js</c> only runs
/// the pointer gestures, moving the drawing while the pointer is down, then reports the outcome
/// (where the nodes were dropped, where the pan stopped) so that the change is committed here, in
/// the history and through <see cref="DocumentChanged"/>. No inline style is ever written: colours
/// are classes over the <c>--omni-mindmap-*</c> tokens, geometry is SVG attributes.
/// </remarks>
public partial class OmniMindMap
{
    internal const int MaxNodes = 500;
    internal const int MaxEdges = 1000;
    internal const int MaxHistory = 30;
    internal const double MinZoom = 0.1;
    internal const double MaxZoom = 5;
    internal const double ZoomStep = 1.2;
    internal const double KeyboardMoveStep = 10;
    internal const double MenuWidth = 224;
    private const string ModulePath = Internal.OmniModules.MindMap;

    private readonly string _generatedId = $"omni-mindmap-{Guid.NewGuid():N}";
    private readonly MindMapModel _model = new();
    private readonly MindMapSelection _selection = new();
    private readonly MindMapHistory _history = new();
    private readonly MindMapLabelSizes _sizes;
    private readonly MindMapViewport _viewport;
    private readonly MindMapEditor _editor;
    private readonly MindMapContextMenu _menu;
    private readonly MindMapKeyboard _keyboard;

    private ElementReference _canvas;
    private IJSObjectReference? _module;
    private DotNetObjectReference<MindMapInteropBridge>? _bridge;
    private OmniMindMapDocument? _lastDocumentParameter;
    private OmniMindMapViewState? _lastViewParameter;
    private bool _documentInitialized;
    private bool _viewInitialized;
    private string _announcement = string.Empty;
    private bool _announceToggle;
    private bool _selectionNotifyPending;
    private bool _disposed;

    /// <summary>Creates an empty map, ready to take its <see cref="Document"/>.</summary>
    public OmniMindMap()
    {
        _sizes = new MindMapLabelSizes(_model);
        _viewport = new MindMapViewport(this, _model, _sizes);
        _editor = new MindMapEditor(this, _model, _selection, _history, _sizes, _viewport);
        _menu = new MindMapContextMenu(this, _model, _selection, _viewport, _editor);
        _keyboard = new MindMapKeyboard(this, _model, _selection, _viewport, _editor, _menu);
        Gestures = new MindMapGestures(this, _model, _selection, _viewport, _editor, _menu);
    }

    /// <summary>The map to draw. Null draws an empty map.</summary>
    [Parameter]
    public OmniMindMapDocument? Document { get; set; }

    /// <summary>
    /// Raised with the new document after every change: a node added, moved, renamed, restyled or
    /// removed, a link drawn or removed, a layout, an undo or a redo.
    /// </summary>
    [Parameter]
    public EventCallback<OmniMindMapDocument> DocumentChanged { get; set; }

    /// <summary>
    /// Shows the map without letting it change: nodes can be selected, the map panned, zoomed,
    /// centred and fitted, but nothing is added, moved, restyled or removed.
    /// </summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Texts of the actions offered by the context menu and <see cref="OmniMindMapToolbar"/>. Texts
    /// left null come from the library resources.
    /// </summary>
    [Parameter]
    public OmniMindMapLabels? ContextLabels { get; set; }

    /// <summary>Accessible name of the canvas; null (the default) takes the localized "Mind map".</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// Where the map is looked at from. Null on first render fits the whole map in the canvas.
    /// </summary>
    [Parameter]
    public OmniMindMapViewState? ViewState { get; set; }

    /// <summary>Raised when the reader pans or zooms, so the host can keep the view.</summary>
    [Parameter]
    public EventCallback<OmniMindMapViewState> ViewStateChanged { get; set; }

    /// <summary>
    /// Raised with the selected node when the single selection changes, and with null when no
    /// single node is selected any more (nothing, a link, or several nodes).
    /// </summary>
    [Parameter]
    public EventCallback<OmniMindMapNode?> OnNodeSelect { get; set; }

    /// <summary>
    /// Raised when the reader asks to rename a node (double click, F2, Enter or the context menu).
    /// Without a handler, an <see cref="OmniMindMapNodeProperties"/> of this map moves the focus to
    /// its text field instead.
    /// </summary>
    [Parameter]
    public EventCallback<OmniMindMapNode> OnNodeRename { get; set; }

    /// <summary>Content placed above the canvas, typically an <see cref="OmniMindMapToolbar"/>.</summary>
    [Parameter]
    public RenderFragment? ToolbarContent { get; set; }

    /// <summary>
    /// Content placed beside the canvas, typically an <see cref="OmniMindMapNodeProperties"/>.
    /// </summary>
    [Parameter]
    public RenderFragment? PanelContent { get; set; }

    /// <summary>
    /// Draws every link as a directed edge: it stops on the border of the node it points to, under an
    /// arrowhead. Off by default, the links then being drawn centre to centre as before.
    /// </summary>
    [Parameter]
    public bool Directed { get; set; }

    /// <summary>
    /// The host's content for each node, drawn inside its box in place of the label: a card with an
    /// icon, a status, a count. The box keeps its size rules, so a node a template draws should carry
    /// its <see cref="OmniMindMapNode.Width"/> and <see cref="OmniMindMapNode.Height"/>. The content is
    /// visual: the node is still announced by its label and selected, moved and linked as any node,
    /// so it should hold no control of its own. Null draws the label.
    /// </summary>
    [Parameter]
    public RenderFragment<OmniMindMapNode>? NodeTemplate { get; set; }

    /// <summary>
    /// Makes the automatic layout, applied to a document without positions and by the auto layout
    /// action, the layered one of <see cref="OmniGraphLayout"/> with these options, for a map read as
    /// a directed graph. Null keeps the radial layout around the root.
    /// </summary>
    [Parameter]
    public OmniGraphLayoutOptions? LayeredLayout { get; set; }

    /// <summary>Raised after any change a toolbar or panel of this map has to reflect.</summary>
    internal event Action? StateChanged;

    /// <summary>Raised when a rename is asked for and the host did not handle it.</summary>
    internal event Func<Task>? RenameRequested;

    /// <summary>What the map does with the gestures <c>omni-mindmap.js</c> reports.</summary>
    internal MindMapGestures Gestures { get; }

    internal OmniMindMapViewState CurrentView => _viewport.Current;

    internal OmniMindMapNode? SelectedNode => _editor.SelectedNode;

    internal bool HasSelection => _selection.HasAny;

    internal bool CanUndo => !ReadOnly && _history.CanUndo;

    internal bool IsLinking => _selection.IsLinking;

    internal string Announcement => _announcement.TrimEnd(' ');

    internal string AddNodeLabel => ContextLabels?.AddNode ?? Localize("MindMapAddNode");

    internal string NewNodeLabel => ContextLabels?.NewNodeLabel ?? Localize("MindMapNewNode");

    internal string RenameLabel => ContextLabels?.Rename ?? Localize("MindMapRename");

    internal string DuplicateLabel => ContextLabels?.Duplicate ?? Localize("MindMapDuplicate");

    internal string AddLinkLabel => ContextLabels?.AddLink ?? Localize("MindMapAddLink");

    internal string LinkSelectSourceLabel => ContextLabels?.LinkSelectSource ?? Localize("MindMapLinkSelectSource");

    internal string LinkSelectTargetLabel => ContextLabels?.LinkSelectTarget ?? Localize("MindMapLinkSelectTarget");

    internal string CenterLabel => ContextLabels?.Center ?? Localize("MindMapCenter");

    internal string DeleteLabel => ContextLabels?.Delete ?? Localize("MindMapDelete");

    internal string AutoLayoutLabel => ContextLabels?.AutoLayout ?? Localize("MindMapAutoLayout");

    internal string FitViewLabel => ContextLabels?.FitView ?? Localize("MindMapFitView");

    internal string CanvasId => $"{BaseId}-canvas";

    /// <summary>The banner text while a link is drawn: pick its source, then its target.</summary>
    internal string LinkBannerText => _selection.LinkSource is null ? LinkSelectSourceLabel : LinkSelectTargetLabel;

    private string BaseId => Id ?? _generatedId;

    private string HintId => $"{BaseId}-hint";

    private string ArrowId => $"{BaseId}-arrow";

    private string? ArrowReference => Directed ? $"url(#{ArrowId})" : null;

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Localize("MindMapCanvasLabel") : Label;

    private string KeyboardHint => Localize(ReadOnly ? "MindMapKeyboardHintReadOnly" : "MindMapKeyboardHint");

    private string? ActiveDescendant => SelectedNode is { } node ? NodeElementId(node.Id) : null;

    private string CanvasCss => CssClassBuilder.Combine(
    [
        "omni-mindmap__canvas",
        _selection.IsLinking ? "omni-mindmap__canvas--linking" : null,
        ReadOnly ? "omni-mindmap__canvas--readonly" : null
    ]);

    /// <summary>The localized name of a colour group, as the pickers show it.</summary>
    internal string GroupName(string group) => Localize(group switch
    {
        OmniMindMapGroups.Green => "MindMapColorGreen",
        OmniMindMapGroups.Blue => "MindMapColorBlue",
        OmniMindMapGroups.Yellow => "MindMapColorYellow",
        OmniMindMapGroups.Red => "MindMapColorRed",
        OmniMindMapGroups.Pink => "MindMapColorPink",
        OmniMindMapGroups.Orange => "MindMapColorOrange",
        OmniMindMapGroups.Purple => "MindMapColorPurple",
        OmniMindMapGroups.Teal => "MindMapColorTeal",
        OmniMindMapGroups.Indigo => "MindMapColorIndigo",
        OmniMindMapGroups.Gray => "MindMapColorGray",
        _ => "MindMapColorWhite"
    });

    /// <summary>A localized text of the mind map family, for the toolbar and the panel.</summary>
    internal string Text(string name, params object[] arguments) => Localize(name, arguments);

    /// <summary>
    /// Adopts a new <see cref="Document"/> instance (null means an empty map): the first one is laid out
    /// when it carries no positions and starts the undo history; a later one keeps the selection whose
    /// nodes survive and leaves the history alone. Takes a new <see cref="ViewState"/>, or schedules a fit
    /// of the whole map when the first one is null, and ends a link in progress once
    /// <see cref="ReadOnly"/> is set.
    /// </summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!_documentInitialized || !ReferenceEquals(Document, _lastDocumentParameter))
        {
            var initial = !_documentInitialized;
            _documentInitialized = true;
            _lastDocumentParameter = Document;
            var incoming = Document ?? OmniMindMapDocument.Empty;
            if (initial || !ReferenceEquals(incoming, _model.Document))
            {
                _selectionNotifyPending |= _editor.Adopt(incoming, initial);
                if (!initial)
                {
                    _menu.Dismiss();
                }

                NotifyStateChanged();
            }
        }

        if (!_viewInitialized || !Equals(ViewState, _lastViewParameter))
        {
            var initial = !_viewInitialized;
            _viewInitialized = true;
            _lastViewParameter = ViewState;
            if (ViewState is { } view)
            {
                _viewport.Adopt(view);
            }
            else if (initial)
            {
                _viewport.FitPending = true;
            }
        }

        if (ReadOnly && _selection.IsLinking)
        {
            _selection.EndLink();
        }
    }

    /// <summary>Raises <see cref="OnNodeSelect"/> with null when a new document dropped the selected node.</summary>
    /// <returns>A task that completes once the event has been handled.</returns>
    protected override async Task OnParametersSetAsync()
    {
        if (_selectionNotifyPending)
        {
            _selectionNotifyPending = false;
            await OnNodeSelect.InvokeAsync(SelectedNode);
        }
    }

    /// <summary>
    /// Attaches the canvas script on the first render and reads the canvas size, then runs the pending
    /// work: measuring the node texts, fitting the map in the canvas, moving the focus to the menu or
    /// the canvas. A lost circuit is ignored.
    /// </summary>
    /// <param name="firstRender">True on the first render of the component.</param>
    /// <returns>A task that completes once the pending work is done.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            if (firstRender)
            {
                _module = await JavaScript.InvokeAsync<IJSObjectReference>("import", ModulePath);
                _bridge = DotNetObjectReference.Create(new MindMapInteropBridge(this));
                var size = await _module.InvokeAsync<MindMapCanvasSize?>("attach", _canvas, _bridge);
                if (size is { Width: > 0, Height: > 0 })
                {
                    _viewport.SetCanvas(size.Width, size.Height);
                }
            }

            if (_module is null)
            {
                return;
            }

            var rerender = false;
            if (_sizes.Pending)
            {
                _sizes.Pending = false;
                var results = await _module.InvokeAsync<MindMapMeasurement[]?>("measure", _canvas);
                rerender |= results is not null && _sizes.Apply(results);
            }

            if (_viewport.FitPending && _model.Drawable.Count > 0)
            {
                _viewport.FitPending = false;
                rerender |= await _viewport.FitAsync();

                // The canvas may not have its final size yet (a container still laying out, a tab
                // being shown): until the reader acts on the map or the host moves the view, a resize
                // fits again (see MindMapViewport.TakeFromCanvas).
                _viewport.FollowsCanvas = true;
            }

            if (_menu.FocusMenuPending)
            {
                _menu.FocusMenuPending = false;
                await _module.InvokeVoidAsync("focusMenu", _canvas);
            }

            if (_menu.FocusCanvasPending)
            {
                _menu.FocusCanvasPending = false;
                await _module.InvokeVoidAsync("focusCanvas", _canvas);
            }

            if (rerender)
            {
                StateHasChanged();
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone; there is no canvas left to drive.
        }
    }

    /// <summary>Detaches the canvas script, releases its module and the script's reference to the component; a lost circuit is ignored.</summary>
    /// <returns>A task that completes once the script is released.</returns>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("detach", _canvas);
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The circuit is already gone, and the listeners with it.
            }
        }

        _bridge?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Runs a notification from the script on the renderer, then redraws.</summary>
    internal Task DispatchAsync(Func<Task> work) => InvokeAsync(async () =>
    {
        if (_disposed)
        {
            return;
        }

        await work();
        StateHasChanged();
    });

    /// <summary>Moves the focus to an element of a toolbar or panel of this map, by identifier.</summary>
    internal async Task FocusElementAsync(string elementId)
    {
        if (_module is null || _disposed)
        {
            return;
        }

        try
        {
            await _module.InvokeVoidAsync("focusById", elementId);
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone; there is nothing left to focus.
        }
    }

    // ----- Commands, shared by the keyboard, the context menu, the toolbar and the panel -----

    internal Task AddNodeAsync() => _editor.AddNodeAsync();

    internal Task DeleteSelectionAsync() => _editor.DeleteSelectionAsync();

    internal Task DuplicateSelectionAsync() => _editor.DuplicateSelectionAsync();

    internal Task StartLinkModeAsync(string? source = null) => _editor.StartLinkModeAsync(source);

    internal Task AutoLayoutAsync() => _editor.AutoLayoutAsync();

    /// <summary>Replaces the node of the same identifier, as the properties panel edits it.</summary>
    internal Task UpdateNodeAsync(OmniMindMapNode updated) => _editor.UpdateNodeAsync(updated);

    /// <summary>Fits every node in the canvas; returns whether the view changed.</summary>
    internal Task<bool> FitViewAsync() => _viewport.FitAsync();

    /// <summary>Pans so the selected node sits in the middle of the canvas.</summary>
    internal Task CenterOnSelectionAsync() =>
        SelectedNode is { } node ? _viewport.CenterOnAsync(node) : Task.CompletedTask;

    /// <summary>
    /// Asks for the selected node to be renamed: through <see cref="OnNodeRename"/> when the host
    /// handles it, otherwise through the properties panel of this map.
    /// </summary>
    internal async Task RequestRenameAsync()
    {
        if (ReadOnly || SelectedNode is not { } node)
        {
            return;
        }

        if (OnNodeRename.HasDelegate)
        {
            await OnNodeRename.InvokeAsync(node);
        }
        else if (RenameRequested is not null)
        {
            await RenameRequested.Invoke();
        }
    }

    /// <summary>Sets the text the live region announces.</summary>
    internal void Announce(string text)
    {
        // Alternating a trailing no-break space makes a repeated message a different string, so a
        // screen reader announces it again instead of treating it as unchanged.
        _announcement = _announceToggle ? text + " " : text;
        _announceToggle = !_announceToggle;
    }

    /// <summary>Tells the toolbar and the panel of this map that its state changed.</summary>
    internal void NotifyStateChanged() => StateChanged?.Invoke();

    // ----- Rendering helpers ---------------------------------------------------------------

    /// <summary>
    /// The box of a node: its fixed size when it has one, otherwise the label plus padding, never
    /// narrower than 80 units.
    /// </summary>
    internal (double Width, double Height) SizeOf(OmniMindMapNode node) => _sizes.SizeOf(node);

    private string NodeElementId(string nodeId) => $"{BaseId}-node-{nodeId}";

    private string NodeCss(OmniMindMapNode node) => CssClassBuilder.Combine(
    [
        "omni-mindmap__node",
        $"omni-mindmap__node--{OmniMindMapGroups.Resolve(node.Group)}",
        _selection.IsSelected(node.Id) ? "omni-mindmap__node--selected" : null,
        _selection.IsLinking && string.Equals(_selection.LinkSource, node.Id, StringComparison.Ordinal) ? "omni-mindmap__node--link-source" : null
    ]);

    // Weight and slant are classes rather than presentation attributes: the SVG slant attribute is
    // spelled like an inline style declaration, which the package CSP scan refuses in markup.
    private static string LabelCss(OmniMindMapNode node) => CssClassBuilder.Combine(
    [
        "omni-mindmap__node-label",
        node.Bold ? "omni-mindmap__node-label--bold" : null,
        node.Italic ? "omni-mindmap__node-label--italic" : null
    ]);

    private string NodeAccessibleName(OmniMindMapNode node)
    {
        var notes = _model.NotesOf(node.Id);
        return notes.Count == 0
            ? node.Label
            : Text("MindMapNodeWithNote", node.Label, string.Join(" ", notes.Select(note => note.Note.Text)));
    }

    private string EdgePath((int Index, OmniMindMapNode From, OmniMindMapNode To) edge)
    {
        if (!Directed)
        {
            return MindMapGeometry.EdgePath(edge.From.X, edge.From.Y, edge.To.X, edge.To.Y);
        }

        var (fromWidth, fromHeight) = SizeOf(edge.From);
        var (toWidth, toHeight) = SizeOf(edge.To);
        return MindMapGeometry.DirectedEdgePath(edge.From.X, edge.From.Y, fromWidth, fromHeight, edge.To.X, edge.To.Y, toWidth, toHeight);
    }
}
