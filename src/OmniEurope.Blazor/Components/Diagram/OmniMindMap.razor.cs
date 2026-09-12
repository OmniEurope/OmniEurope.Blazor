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
    private const double FitMaxZoom = 2;
    private const double FitPadding = 60;
    private const double MenuItemHeight = 44;
    private const double MenuSeparatorHeight = 9;
    private const double MenuColorsHeight = 148;
    private const double MenuPadding = 14;
    private const string ModulePath = "./_content/OmniEurope.Blazor/omni-mindmap.js";

    private readonly string _generatedId = $"omni-mindmap-{Guid.NewGuid():N}";
    private readonly List<OmniMindMapDocument> _undo = [];
    private readonly List<OmniMindMapDocument> _redo = [];
    private readonly HashSet<string> _multi = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MindMapMeasuredLabel> _measured = new(StringComparer.Ordinal);
    private readonly Dictionary<string, OmniMindMapNode> _byId = new(StringComparer.Ordinal);
    private readonly List<OmniMindMapNode> _drawable = [];
    private readonly List<(int Index, OmniMindMapNode From, OmniMindMapNode To)> _renderedEdges = [];
    private readonly Dictionary<string, List<(int Index, OmniMindMapNote Note)>> _notesByNode = new(StringComparer.Ordinal);

    private ElementReference _canvas;
    private IJSObjectReference? _module;
    private DotNetObjectReference<MindMapInteropBridge>? _bridge;
    private OmniMindMapDocument _document = OmniMindMapDocument.Empty;
    private OmniMindMapDocument? _lastDocumentParameter;
    private OmniMindMapViewState? _lastViewParameter;
    private bool _documentInitialized;
    private bool _viewInitialized;
    private string? _selectedId;
    private int _selectedEdge = -1;
    private double _panX;
    private double _panY;
    private double _zoom = 1;
    private double _canvasWidth = 800;
    private double _canvasHeight = 600;
    private bool _linking;
    private string? _linkSource;
    private MindMapMenuState? _menu;
    private string _announcement = string.Empty;
    private bool _announceToggle;
    private bool _fitPending;
    private bool _viewFollowsCanvas;
    private bool _measurePending = true;
    private bool _focusMenuPending;
    private bool _focusCanvasPending;
    private bool _selectionNotifyPending;
    private bool _disposed;
    private int _nextId = 1;

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

    /// <summary>Accessible name of the canvas. Defaults to the localized "mind map".</summary>
    [Parameter]
    public string? AriaLabel { get; set; }

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
    public EventCallback<OmniMindMapNode?> NodeSelected { get; set; }

    /// <summary>
    /// Raised when the reader asks to rename a node (double click, F2, Enter or the context menu).
    /// Without a handler, an <see cref="OmniMindMapNodeProperties"/> of this map moves the focus to
    /// its text field instead.
    /// </summary>
    [Parameter]
    public EventCallback<OmniMindMapNode> NodeRenameRequested { get; set; }

    /// <summary>Content placed above the canvas, typically an <see cref="OmniMindMapToolbar"/>.</summary>
    [Parameter]
    public RenderFragment? ToolbarContent { get; set; }

    /// <summary>
    /// Content placed beside the canvas, typically an <see cref="OmniMindMapNodeProperties"/>.
    /// </summary>
    [Parameter]
    public RenderFragment? PanelContent { get; set; }

    /// <summary>Raised after any change a toolbar or panel of this map has to reflect.</summary>
    internal event Action? StateChanged;

    /// <summary>Raised when a rename is asked for and the host did not handle it.</summary>
    internal event Func<Task>? RenameRequested;

    internal OmniMindMapDocument CurrentDocument => _document;

    internal OmniMindMapViewState CurrentView => new(_panX, _panY, _zoom);

    internal OmniMindMapNode? SelectedNode =>
        _selectedId is not null && _byId.TryGetValue(_selectedId, out var node) ? node : null;

    internal IReadOnlyCollection<string> MultiSelection => _multi;

    internal int SelectedEdgeIndex => _selectedEdge;

    internal bool HasSelection => _selectedId is not null || _multi.Count > 0 || _selectedEdge >= 0;

    internal bool CanUndo => !ReadOnly && _undo.Count > 1;

    internal bool CanRedo => !ReadOnly && _redo.Count > 0;

    internal bool IsLinking => _linking;

    internal string? LinkSource => _linkSource;

    internal string Announcement => _announcement.TrimEnd(' ');

    internal bool IsMenuOpen => _menu is not null;

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

    private string BaseId => Id ?? _generatedId;

    private string HintId => $"{BaseId}-hint";

    private string EffectiveAriaLabel => string.IsNullOrWhiteSpace(AriaLabel) ? Localize("MindMapCanvasLabel") : AriaLabel;

    private string KeyboardHint => Localize(ReadOnly ? "MindMapKeyboardHintReadOnly" : "MindMapKeyboardHint");

    private string LinkBannerText => _linkSource is null ? LinkSelectSourceLabel : LinkSelectTargetLabel;

    private string? ActiveDescendant => SelectedNode is { } node ? NodeElementId(node.Id) : null;

    private string CanvasCss => CssClassBuilder.Combine(
    [
        "omni-mindmap__canvas",
        _linking ? "omni-mindmap__canvas--linking" : null,
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

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!_documentInitialized || !ReferenceEquals(Document, _lastDocumentParameter))
        {
            var initial = !_documentInitialized;
            _documentInitialized = true;
            _lastDocumentParameter = Document;
            var incoming = Document ?? OmniMindMapDocument.Empty;
            if (initial || !ReferenceEquals(incoming, _document))
            {
                AdoptDocument(incoming, initial);
            }
        }

        if (!_viewInitialized || !Equals(ViewState, _lastViewParameter))
        {
            var initial = !_viewInitialized;
            _viewInitialized = true;
            _lastViewParameter = ViewState;
            if (ViewState is { } view)
            {
                if (view != CurrentView)
                {
                    _viewFollowsCanvas = false;
                }

                _panX = view.PanX;
                _panY = view.PanY;
                _zoom = ClampZoom(view.Zoom);
                _fitPending = false;
            }
            else if (initial)
            {
                _fitPending = true;
            }
        }

        if (ReadOnly && _linking)
        {
            _linking = false;
            _linkSource = null;
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_selectionNotifyPending)
        {
            _selectionNotifyPending = false;
            await NodeSelected.InvokeAsync(SelectedNode);
        }
    }

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
                    _canvasWidth = size.Width;
                    _canvasHeight = size.Height;
                }
            }

            if (_module is null)
            {
                return;
            }

            var rerender = false;
            if (_measurePending)
            {
                _measurePending = false;
                rerender |= await MeasureAsync();
            }

            if (_fitPending && _drawable.Count > 0)
            {
                _fitPending = false;
                rerender |= await FitViewAsync();

                // The canvas may not have its final size yet (a container still laying out, a tab
                // being shown): until the reader or the host moves the view, a resize fits again.
                _viewFollowsCanvas = true;
            }

            if (_focusMenuPending)
            {
                _focusMenuPending = false;
                await _module.InvokeVoidAsync("focusMenu", _canvas);
            }

            if (_focusCanvasPending)
            {
                _focusCanvasPending = false;
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

    // ----- Script notifications -------------------------------------------------------------

    internal async Task HandleNodePressedAsync(string nodeId)
    {
        _menu = null;
        if (!_byId.ContainsKey(nodeId))
        {
            return;
        }

        if (_linking)
        {
            await PickLinkEndAsync(nodeId);
            return;
        }

        await SelectNodeAsync(nodeId);
    }

    internal async Task HandleNodesMovedAsync(IReadOnlyList<MindMapNodeMove> moves)
    {
        if (ReadOnly || moves.Count == 0)
        {
            return;
        }

        var targets = moves
            .Where(move => _byId.ContainsKey(move.Id) && double.IsFinite(move.X) && double.IsFinite(move.Y))
            .GroupBy(move => move.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
        if (targets.Count == 0)
        {
            return;
        }

        await CommitAsync(ReplaceNodes(node => targets.TryGetValue(node.Id, out var move)
            ? node with { X = Math.Round(move.X), Y = Math.Round(move.Y) }
            : node), null);
    }

    internal async Task HandleEdgePressedAsync(int index)
    {
        _menu = null;
        if (_linking || !_renderedEdges.Any(edge => edge.Index == index))
        {
            return;
        }

        var hadNode = _selectedId is not null;
        _selectedId = null;
        _multi.Clear();
        _selectedEdge = index;
        Announce(Text("MindMapAnnounceLinkSelected"));
        NotifyStateChanged();
        if (hadNode)
        {
            await NodeSelected.InvokeAsync(null);
        }
    }

    internal async Task HandleBackgroundPressedAsync()
    {
        _menu = null;
        if (HasSelection)
        {
            await ClearSelectionAsync();
        }
    }

    internal async Task HandleLassoAsync(double left, double top, double right, double bottom)
    {
        var inside = _drawable
            .Where(node => node.X >= left && node.X <= right && node.Y >= top && node.Y <= bottom)
            .Select(node => node.Id)
            .ToArray();
        if (inside.Length == 1)
        {
            await SelectNodeAsync(inside[0]);
            return;
        }

        var hadNode = _selectedId is not null;
        _selectedId = null;
        _selectedEdge = -1;
        _multi.Clear();
        _multi.UnionWith(inside);
        if (inside.Length > 1)
        {
            Announce(Text("MindMapAnnounceManySelected", inside.Length));
        }

        NotifyStateChanged();
        if (hadNode)
        {
            await NodeSelected.InvokeAsync(null);
        }
    }

    internal Task HandleViewChangedAsync(double panX, double panY, double zoom) =>
        double.IsFinite(panX) && double.IsFinite(panY) && double.IsFinite(zoom)
            ? SetViewAsync(panX, panY, zoom)
            : Task.CompletedTask;

    internal async Task HandleNodeDoubleClickedAsync(string nodeId)
    {
        if (!_byId.ContainsKey(nodeId))
        {
            return;
        }

        await SelectNodeAsync(nodeId);
        await RequestRenameAsync();
    }

    internal Task HandleCanvasDoubleClickedAsync(double mapX, double mapY) =>
        ReadOnly || !double.IsFinite(mapX) || !double.IsFinite(mapY)
            ? Task.CompletedTask
            : AddNodeAtAsync(mapX, mapY, OmniMindMapGroups.Rotation[_nextId % OmniMindMapGroups.Rotation.Count]);

    internal async Task HandleContextMenuRequestedAsync(string? nodeId, double left, double top, double mapX, double mapY)
    {
        if (nodeId is not null && _byId.ContainsKey(nodeId))
        {
            if (!_multi.Contains(nodeId))
            {
                await SelectNodeAsync(nodeId);
            }
            else
            {
                _selectedId = nodeId;
                _selectedEdge = -1;
            }

            _menu = new MindMapMenuState(nodeId, left, top, mapX, mapY);
        }
        else
        {
            _menu = new MindMapMenuState(null, left, top, mapX, mapY);
        }

        _focusMenuPending = true;
        NotifyStateChanged();
    }

    internal Task HandleMenuDismissedAsync()
    {
        _menu = null;
        return Task.CompletedTask;
    }

    internal async Task HandleResizedAsync(double width, double height)
    {
        if (width <= 0 || height <= 0 || (width == _canvasWidth && height == _canvasHeight))
        {
            return;
        }

        _canvasWidth = width;
        _canvasHeight = height;
        if (_viewFollowsCanvas)
        {
            await FitViewAsync();
            _viewFollowsCanvas = true;
        }
    }

    // ----- Commands, shared by the keyboard, the context menu, the toolbar and the panel -----

    internal Task AddNodeAsync()
    {
        // Around the middle of what is visible, on a golden-angle spiral so repeated additions do
        // not stack exactly on top of each other.
        var angle = _nextId * 2.399963;
        var x = ((_canvasWidth / 2) - _panX) / _zoom + (Math.Cos(angle) * 40);
        var y = ((_canvasHeight / 2) - _panY) / _zoom + (Math.Sin(angle) * 40);
        return AddNodeAtAsync(x, y, OmniMindMapGroups.Green);
    }

    internal async Task AddNodeAtAsync(double x, double y, string group)
    {
        if (ReadOnly)
        {
            return;
        }

        if (_drawable.Count >= MaxNodes)
        {
            Announce(Text("MindMapAnnounceLimit"));
            NotifyStateChanged();
            return;
        }

        var node = new OmniMindMapNode
        {
            Id = TakeNextId(),
            Label = NewNodeLabel,
            Group = group,
            X = Math.Round(x),
            Y = Math.Round(y)
        };
        await CommitAsync(_document with { Nodes = [.. _document.Nodes, node] }, null);
        await SelectNodeAsync(node.Id);
        Announce(Text("MindMapAnnounceAdded", node.Label));
    }

    internal async Task DeleteSelectionAsync()
    {
        if (ReadOnly)
        {
            return;
        }

        HashSet<string> removed;
        if (_multi.Count > 0)
        {
            removed = new HashSet<string>(_multi, StringComparer.Ordinal);
        }
        else if (_selectedId is not null)
        {
            removed = new HashSet<string>(StringComparer.Ordinal) { _selectedId };
        }
        else if (_selectedEdge >= 0 && _selectedEdge < _document.Edges.Count)
        {
            var index = _selectedEdge;
            _selectedEdge = -1;
            await CommitAsync(_document with { Edges = [.. _document.Edges.Where((_, position) => position != index)] }, Text("MindMapAnnounceLinkDeleted"));
            return;
        }
        else
        {
            return;
        }

        var next = _document with
        {
            Nodes = [.. _document.Nodes.Where(node => !removed.Contains(node.Id))],
            Edges = [.. _document.Edges.Where(edge => !removed.Contains(edge.From) && !removed.Contains(edge.To))],
            Notes = [.. _document.Notes.Where(note => !removed.Contains(note.AttachedTo))]
        };
        var hadNode = _selectedId is not null;
        _selectedId = null;
        _multi.Clear();
        _selectedEdge = -1;
        await CommitAsync(next, Text("MindMapAnnounceDeleted", removed.Count));
        if (hadNode)
        {
            await NodeSelected.InvokeAsync(null);
        }
    }

    internal async Task DuplicateSelectionAsync()
    {
        if (ReadOnly || SelectedNode is not { } source || _drawable.Count >= MaxNodes)
        {
            return;
        }

        var copy = source with { Id = TakeNextId(), X = source.X + 40, Y = source.Y + 30 };
        var edges = _document.Edges.ToList();
        foreach (var incoming in _document.Edges.Where(edge => string.Equals(edge.To, source.Id, StringComparison.Ordinal)))
        {
            if (edges.Count < MaxEdges)
            {
                edges.Add(incoming with { To = copy.Id });
            }
        }

        await CommitAsync(_document with { Nodes = [.. _document.Nodes, copy], Edges = edges }, null);
        await SelectNodeAsync(copy.Id);
        Announce(Text("MindMapAnnounceDuplicated", copy.Label));
    }

    internal Task StartLinkModeAsync(string? source = null)
    {
        if (ReadOnly)
        {
            return Task.CompletedTask;
        }

        _linking = true;
        _linkSource = source is not null && _byId.ContainsKey(source) ? source : null;
        if (_linkSource is not null)
        {
            _selectedId = _linkSource;
            _multi.Clear();
            _selectedEdge = -1;
        }

        Announce(LinkBannerText);
        NotifyStateChanged();
        return Task.CompletedTask;
    }

    internal void CancelLinkMode()
    {
        if (!_linking)
        {
            return;
        }

        _linking = false;
        _linkSource = null;
        Announce(Text("MindMapAnnounceLinkCancelled"));
        NotifyStateChanged();
    }

    internal async Task CenterOnSelectionAsync()
    {
        if (SelectedNode is not { } node)
        {
            return;
        }

        await SetViewAsync((_canvasWidth / 2) - (node.X * _zoom), (_canvasHeight / 2) - (node.Y * _zoom), _zoom);
    }

    internal async Task AutoLayoutAsync()
    {
        if (ReadOnly || _drawable.Count == 0)
        {
            return;
        }

        var positions = MindMapLayout.Radial(_drawable, _document.Edges, _document.RootId, _canvasWidth, _canvasHeight, SizeOf);
        await CommitAsync(ReplaceNodes(node => positions.TryGetValue(node.Id, out var position)
            ? node with { X = Math.Round(position.X), Y = Math.Round(position.Y) }
            : node), Text("MindMapAnnounceLaidOut"));
    }

    /// <summary>
    /// Zooms and pans so every node is visible with a 60 pixel margin, never above 2x so a map of
    /// one node does not fill the screen. Returns whether the view changed.
    /// </summary>
    internal async Task<bool> FitViewAsync()
    {
        if (_drawable.Count == 0)
        {
            return false;
        }

        double left = double.MaxValue, top = double.MaxValue, right = double.MinValue, bottom = double.MinValue;
        foreach (var node in _drawable)
        {
            var (width, height) = SizeOf(node);
            left = Math.Min(left, node.X - (width / 2));
            top = Math.Min(top, node.Y - (height / 2));
            right = Math.Max(right, node.X + (width / 2));
            bottom = Math.Max(bottom, node.Y + (height / 2));
        }

        var scaleX = (_canvasWidth - (FitPadding * 2)) / Math.Max(1, right - left);
        var scaleY = (_canvasHeight - (FitPadding * 2)) / Math.Max(1, bottom - top);
        var zoom = Math.Clamp(Math.Min(scaleX, scaleY), MinZoom, FitMaxZoom);
        var before = CurrentView;
        await SetViewAsync(FitPadding - (left * zoom), FitPadding - (top * zoom), zoom);
        return before != CurrentView;
    }

    internal async Task ZoomAsync(double factor)
    {
        var zoom = ClampZoom(_zoom * factor);
        var centerX = _canvasWidth / 2;
        var centerY = _canvasHeight / 2;
        await SetViewAsync(
            centerX - ((centerX - _panX) * (zoom / _zoom)),
            centerY - ((centerY - _panY) * (zoom / _zoom)),
            zoom);
        Announce(Text("MindMapAnnounceZoom", Math.Round(zoom * 100)));
    }

    internal async Task UndoAsync()
    {
        if (!CanUndo)
        {
            return;
        }

        _redo.Add(_undo[^1]);
        _undo.RemoveAt(_undo.Count - 1);
        await RestoreAsync(_undo[^1], Text("MindMapAnnounceUndone"));
    }

    internal async Task RedoAsync()
    {
        if (!CanRedo)
        {
            return;
        }

        var snapshot = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        _undo.Add(snapshot);
        await RestoreAsync(snapshot, Text("MindMapAnnounceRedone"));
    }

    /// <summary>Replaces the node of the same identifier, as the properties panel edits it.</summary>
    internal async Task UpdateNodeAsync(OmniMindMapNode updated)
    {
        if (ReadOnly || !_byId.TryGetValue(updated.Id, out var current) || current == updated)
        {
            return;
        }

        var replaced = false;
        await CommitAsync(ReplaceNodes(node =>
        {
            if (replaced || !string.Equals(node.Id, updated.Id, StringComparison.Ordinal))
            {
                return node;
            }

            replaced = true;
            return updated;
        }), null);
    }

    internal Task SetNodeGroupAsync(string nodeId, string group) =>
        _byId.TryGetValue(nodeId, out var node) ? UpdateNodeAsync(node with { Group = group }) : Task.CompletedTask;

    internal async Task MoveSelectionAsync(double dx, double dy)
    {
        if (ReadOnly)
        {
            return;
        }

        var moving = _multi.Count > 0
            ? new HashSet<string>(_multi, StringComparer.Ordinal)
            : _selectedId is not null ? new HashSet<string>(StringComparer.Ordinal) { _selectedId } : null;
        if (moving is null)
        {
            return;
        }

        await CommitAsync(ReplaceNodes(node => moving.Contains(node.Id) ? node with { X = node.X + dx, Y = node.Y + dy } : node), null);
    }

    internal async Task RequestRenameAsync()
    {
        if (ReadOnly || SelectedNode is not { } node)
        {
            return;
        }

        if (NodeRenameRequested.HasDelegate)
        {
            await NodeRenameRequested.InvokeAsync(node);
        }
        else if (RenameRequested is not null)
        {
            await RenameRequested.Invoke();
        }
    }

    // ----- Keyboard ------------------------------------------------------------------------

    internal async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.CtrlKey || args.MetaKey)
        {
            if (args.AltKey)
            {
                return;
            }

            if (Is(args, "z") && !args.ShiftKey)
            {
                await UndoAsync();
            }
            else if (Is(args, "y") || (Is(args, "z") && args.ShiftKey))
            {
                await RedoAsync();
            }

            return;
        }

        if (args.AltKey)
        {
            return;
        }

        switch (args.Key)
        {
            case "ArrowUp":
                await ArrowAsync(args, 0, -1);
                return;
            case "ArrowDown":
                await ArrowAsync(args, 0, 1);
                return;
            case "ArrowLeft":
                await ArrowAsync(args, -1, 0);
                return;
            case "ArrowRight":
                await ArrowAsync(args, 1, 0);
                return;
            case "Home":
                await SelectNodeAsync(RootNode?.Id);
                return;
            case "Enter" or "F2":
                if (_linking && _selectedId is not null)
                {
                    await PickLinkEndAsync(_selectedId);
                }
                else
                {
                    await RequestRenameAsync();
                }

                return;
            case "Escape":
                if (_menu is not null)
                {
                    _menu = null;
                }
                else if (_linking)
                {
                    CancelLinkMode();
                }
                else if (HasSelection)
                {
                    await ClearSelectionAsync();
                }

                return;
            case "Delete" or "Backspace":
                await DeleteSelectionAsync();
                return;
            case "+" or "=":
                await ZoomAsync(ZoomStep);
                return;
            case "-" or "_":
                await ZoomAsync(1 / ZoomStep);
                return;
            case "0":
                await FitViewAsync();
                return;
            case "ContextMenu":
                await OpenMenuFromKeyboardAsync();
                return;
            case "F10" when args.ShiftKey:
                await OpenMenuFromKeyboardAsync();
                return;
        }

        if (Is(args, "n"))
        {
            await AddNodeAsync();
        }
        else if (Is(args, "d"))
        {
            await DuplicateSelectionAsync();
        }
        else if (Is(args, "c"))
        {
            await CenterOnSelectionAsync();
        }
    }

    internal Task HandleMenuKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key is "Escape" or "Tab")
        {
            CloseMenu(returnFocus: args.Key == "Escape");
        }

        return Task.CompletedTask;
    }

    private static bool Is(KeyboardEventArgs args, string letter) =>
        string.Equals(args.Key, letter, StringComparison.OrdinalIgnoreCase);

    private async Task ArrowAsync(KeyboardEventArgs args, int directionX, int directionY)
    {
        if (args.ShiftKey)
        {
            await MoveSelectionAsync(directionX * KeyboardMoveStep, directionY * KeyboardMoveStep);
            return;
        }

        await NavigateAsync(directionX, directionY);
    }

    /// <summary>
    /// Moves the selection to the nearest node lying in the pressed direction: within about 63
    /// degrees of it, closest along it, sideways distance counting double.
    /// </summary>
    private async Task NavigateAsync(int directionX, int directionY)
    {
        if (SelectedNode is not { } current)
        {
            await SelectNodeAsync((RootNode ?? _drawable.FirstOrDefault())?.Id);
            return;
        }

        OmniMindMapNode? best = null;
        var bestScore = double.MaxValue;
        foreach (var candidate in _drawable)
        {
            if (string.Equals(candidate.Id, current.Id, StringComparison.Ordinal))
            {
                continue;
            }

            var dx = candidate.X - current.X;
            var dy = candidate.Y - current.Y;
            var along = (dx * directionX) + (dy * directionY);
            var across = Math.Abs((dx * directionY) - (dy * directionX));
            if (along <= 0 || across > along * 2)
            {
                continue;
            }

            var score = along + (across * 2);
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        if (best is not null)
        {
            await SelectNodeAsync(best.Id);
        }
    }

    private async Task OpenMenuFromKeyboardAsync()
    {
        if (SelectedNode is { } node)
        {
            var left = (node.X * _zoom) + _panX;
            var top = (node.Y * _zoom) + _panY;
            await HandleContextMenuRequestedAsync(node.Id, left, top, node.X, node.Y);
        }
        else
        {
            var mapX = ((_canvasWidth / 2) - _panX) / _zoom;
            var mapY = ((_canvasHeight / 2) - _panY) / _zoom;
            await HandleContextMenuRequestedAsync(null, _canvasWidth / 2, _canvasHeight / 2, mapX, mapY);
        }
    }

    // ----- Context menu actions ------------------------------------------------------------

    private string? CloseMenu(bool returnFocus)
    {
        var nodeId = _menu?.NodeId;
        _menu = null;
        _focusCanvasPending = returnFocus;
        NotifyStateChanged();
        return nodeId;
    }

    private Task MenuRenameAsync()
    {
        CloseMenu(returnFocus: true);
        return RequestRenameAsync();
    }

    private Task MenuDuplicateAsync()
    {
        CloseMenu(returnFocus: true);
        return DuplicateSelectionAsync();
    }

    private Task MenuLinkAsync() => StartLinkModeAsync(CloseMenu(returnFocus: true));

    private Task MenuCenterAsync()
    {
        CloseMenu(returnFocus: true);
        return CenterOnSelectionAsync();
    }

    private Task MenuDeleteAsync()
    {
        CloseMenu(returnFocus: true);
        return DeleteSelectionAsync();
    }

    private Task MenuColorAsync(string group)
    {
        var nodeId = CloseMenu(returnFocus: true);
        return nodeId is null ? Task.CompletedTask : SetNodeGroupAsync(nodeId, group);
    }

    private Task MenuAddNodeAsync()
    {
        var menu = _menu;
        CloseMenu(returnFocus: true);
        return menu is null ? Task.CompletedTask : AddNodeAtAsync(menu.MapX, menu.MapY, OmniMindMapGroups.Green);
    }

    private Task MenuAutoLayoutAsync()
    {
        CloseMenu(returnFocus: true);
        return AutoLayoutAsync();
    }

    private Task MenuFitViewAsync()
    {
        CloseMenu(returnFocus: true);
        return FitViewAsync();
    }

    private bool IsMenuNodeGroup(MindMapMenuState menu, string group) =>
        menu.NodeId is not null
        && _byId.TryGetValue(menu.NodeId, out var node)
        && string.Equals(OmniMindMapGroups.Resolve(node.Group), group, StringComparison.Ordinal);

    private double MenuHeight(MindMapMenuState menu)
    {
        var (items, separators, colors) = (menu.NodeId is null, ReadOnly) switch
        {
            (true, true) => (1, 0, false),
            (true, false) => (3, 1, false),
            (false, true) => (1, 0, false),
            (false, false) => (5, 2, true)
        };
        return (items * MenuItemHeight) + (separators * MenuSeparatorHeight) + (colors ? MenuColorsHeight : 0) + MenuPadding;
    }

    private double MenuLeft(MindMapMenuState menu) =>
        Math.Max(0, Math.Min(menu.Left, _canvasWidth - MenuWidth - 8));

    private double MenuTop(MindMapMenuState menu) =>
        Math.Max(0, Math.Min(menu.Top, _canvasHeight - MenuHeight(menu) - 8));

    // ----- State --------------------------------------------------------------------------

    private OmniMindMapNode? RootNode =>
        _document.RootId is not null && _byId.TryGetValue(_document.RootId, out var root) ? root : _drawable.FirstOrDefault();

    private void AdoptDocument(OmniMindMapDocument document, bool initial)
    {
        SetDocument(document);
        if (initial)
        {
            if (_drawable.Count > 0 && _drawable.All(node => node.X == 0 && node.Y == 0))
            {
                // A graph that carries no positions yet gets laid out once, without being reported
                // as a change: it becomes one when the reader first edits the map.
                var positions = MindMapLayout.Radial(_drawable, document.Edges, document.RootId, _canvasWidth, _canvasHeight, SizeOf);
                SetDocument(ReplaceNodes(node => positions.TryGetValue(node.Id, out var position)
                    ? node with { X = Math.Round(position.X), Y = Math.Round(position.Y) }
                    : node));
            }

            _undo.Clear();
            _redo.Clear();
            _undo.Add(_document);
        }
        else
        {
            // A document replaced from outside (a remote refresh) keeps the reader's selection when
            // its nodes survive and leaves the history alone, as the original editor did.
            if (_selectedId is not null && !_byId.ContainsKey(_selectedId))
            {
                _selectedId = null;
                _selectionNotifyPending = true;
            }

            _multi.RemoveWhere(id => !_byId.ContainsKey(id));
            if (_selectedEdge >= _document.Edges.Count)
            {
                _selectedEdge = -1;
            }

            if (_linkSource is not null && !_byId.ContainsKey(_linkSource))
            {
                _linkSource = null;
            }

            _menu = null;
        }

        NotifyStateChanged();
    }

    private void SetDocument(OmniMindMapDocument document)
    {
        _document = document;
        _drawable.Clear();
        _byId.Clear();
        foreach (var node in document.Nodes)
        {
            if (IsDrawableId(node.Id) && _byId.TryAdd(node.Id, node))
            {
                _drawable.Add(node);
            }
        }

        _renderedEdges.Clear();
        for (var index = 0; index < document.Edges.Count; index++)
        {
            var edge = document.Edges[index];
            if (_byId.TryGetValue(edge.From, out var from) && _byId.TryGetValue(edge.To, out var to))
            {
                _renderedEdges.Add((index, from, to));
            }
        }

        _notesByNode.Clear();
        for (var index = 0; index < document.Notes.Count; index++)
        {
            var note = document.Notes[index];
            if (_byId.ContainsKey(note.AttachedTo))
            {
                if (!_notesByNode.TryGetValue(note.AttachedTo, out var notes))
                {
                    notes = [];
                    _notesByNode[note.AttachedTo] = notes;
                }

                notes.Add((index, note));
            }
        }

        _nextId = Math.Max(_nextId, NextNumericId(document));
        _measurePending = true;
    }

    /// <summary>
    /// The number after the highest <c>node_N</c> identifier, so that a new node never reuses one.
    /// </summary>
    private static int NextNumericId(OmniMindMapDocument document)
    {
        var highest = 0;
        foreach (var node in document.Nodes)
        {
            if (node.Id.StartsWith("node_", StringComparison.Ordinal)
                && int.TryParse(node.Id.AsSpan(5), NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            {
                highest = Math.Max(highest, number);
            }
        }

        return highest + 1;
    }

    private string TakeNextId()
    {
        string id;
        do
        {
            id = $"node_{_nextId++}";
        }
        while (_document.Nodes.Any(node => string.Equals(node.Id, id, StringComparison.Ordinal)));

        return id;
    }

    private static bool IsDrawableId(string id) =>
        id.Length is > 0 and <= 64 && id.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-');

    private OmniMindMapDocument ReplaceNodes(Func<OmniMindMapNode, OmniMindMapNode> change) =>
        _document with
        {
            Nodes = [.. _document.Nodes.Select(node => _byId.TryGetValue(node.Id, out var drawn) && ReferenceEquals(drawn, node) ? change(node) : node)]
        };

    private async Task CommitAsync(OmniMindMapDocument next, string? announcement)
    {
        SetDocument(next);
        if (_undo.Count == 0 || !ReferenceEquals(_undo[^1], next))
        {
            _undo.Add(next);
            _redo.Clear();
            while (_undo.Count > MaxHistory)
            {
                _undo.RemoveAt(0);
            }
        }

        if (announcement is not null)
        {
            Announce(announcement);
        }

        NotifyStateChanged();
        await DocumentChanged.InvokeAsync(next);
    }

    private async Task RestoreAsync(OmniMindMapDocument snapshot, string announcement)
    {
        var hadNode = _selectedId is not null;
        SetDocument(snapshot);
        _selectedId = null;
        _multi.Clear();
        _selectedEdge = -1;
        Announce(announcement);
        NotifyStateChanged();
        await DocumentChanged.InvokeAsync(snapshot);
        if (hadNode)
        {
            await NodeSelected.InvokeAsync(null);
        }
    }

    private async Task SelectNodeAsync(string? nodeId)
    {
        _multi.Clear();
        _selectedEdge = -1;
        if (nodeId is not null && !_byId.ContainsKey(nodeId))
        {
            nodeId = null;
        }

        if (string.Equals(_selectedId, nodeId, StringComparison.Ordinal))
        {
            NotifyStateChanged();
            return;
        }

        _selectedId = nodeId;
        var node = SelectedNode;
        Announce(node is null ? Text("MindMapAnnounceNoSelection") : DescribeSelection(node));
        NotifyStateChanged();
        await NodeSelected.InvokeAsync(node);
    }

    private async Task ClearSelectionAsync()
    {
        var hadNode = _selectedId is not null;
        _selectedId = null;
        _multi.Clear();
        _selectedEdge = -1;
        Announce(Text("MindMapAnnounceNoSelection"));
        NotifyStateChanged();
        if (hadNode)
        {
            await NodeSelected.InvokeAsync(null);
        }
    }

    private async Task PickLinkEndAsync(string nodeId)
    {
        if (_linkSource is null)
        {
            _linkSource = nodeId;
            await SelectNodeAsync(nodeId);
            Announce(LinkSelectTargetLabel);
            NotifyStateChanged();
            return;
        }

        if (string.Equals(_linkSource, nodeId, StringComparison.Ordinal))
        {
            return;
        }

        var source = _linkSource;
        _linking = false;
        _linkSource = null;
        var duplicate = _document.Edges.Any(edge =>
            (string.Equals(edge.From, source, StringComparison.Ordinal) && string.Equals(edge.To, nodeId, StringComparison.Ordinal))
            || (string.Equals(edge.From, nodeId, StringComparison.Ordinal) && string.Equals(edge.To, source, StringComparison.Ordinal)));
        await SelectNodeAsync(nodeId);
        if (duplicate || _document.Edges.Count >= MaxEdges)
        {
            NotifyStateChanged();
            return;
        }

        await CommitAsync(
            _document with { Edges = [.. _document.Edges, new OmniMindMapEdge { From = source, To = nodeId }] },
            Text("MindMapAnnounceLinked", _byId[source].Label, _byId[nodeId].Label));
    }

    private async Task SetViewAsync(double panX, double panY, double zoom)
    {
        zoom = ClampZoom(zoom);
        if (panX == _panX && panY == _panY && zoom == _zoom)
        {
            return;
        }

        _panX = panX;
        _panY = panY;
        _zoom = zoom;
        _viewFollowsCanvas = false;
        NotifyStateChanged();
        await ViewStateChanged.InvokeAsync(CurrentView);
    }

    private static double ClampZoom(double zoom) =>
        double.IsFinite(zoom) ? Math.Clamp(zoom, MinZoom, MaxZoom) : 1;

    private void Announce(string text)
    {
        // Alternating a trailing no-break space makes a repeated message a different string, so a
        // screen reader announces it again instead of treating it as unchanged.
        _announcement = _announceToggle ? text + " " : text;
        _announceToggle = !_announceToggle;
    }

    private void NotifyStateChanged() => StateChanged?.Invoke();

    private string DescribeSelection(OmniMindMapNode node)
    {
        var links = _renderedEdges.Count(edge =>
            string.Equals(edge.From.Id, node.Id, StringComparison.Ordinal) || string.Equals(edge.To.Id, node.Id, StringComparison.Ordinal));
        return Text("MindMapAnnounceSelected", node.Label, links);
    }

    // ----- Measurement ---------------------------------------------------------------------

    private async Task<bool> MeasureAsync()
    {
        var results = await _module!.InvokeAsync<MindMapMeasurement[]?>("measure", _canvas);
        if (results is null)
        {
            return false;
        }

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

    private string? SignatureOf(string key)
    {
        if (key.StartsWith("n:", StringComparison.Ordinal))
        {
            return _byId.TryGetValue(key[2..], out var node) ? LabelSignature(node) : null;
        }

        if (key.StartsWith("note:", StringComparison.Ordinal)
            && int.TryParse(key.AsSpan(5), NumberStyles.None, CultureInfo.InvariantCulture, out var index)
            && index < _document.Notes.Count)
        {
            return _document.Notes[index].Text;
        }

        return null;
    }

    private static string LabelSignature(OmniMindMapNode node) =>
        string.Create(CultureInfo.InvariantCulture, $"{node.Label}{FontSizeOf(node)}{node.Bold}{node.Italic}");

    // ----- Rendering helpers ---------------------------------------------------------------

    /// <summary>
    /// The box of a node: its fixed size when it has one, otherwise the label plus padding, never
    /// narrower than 80 units.
    /// </summary>
    internal (double Width, double Height) SizeOf(OmniMindMapNode node)
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

    private (double Width, double Height) NoteSize((int Index, OmniMindMapNote Note) note)
    {
        var key = NoteKey(note.Index);
        return _measured.TryGetValue(key, out var measured) && string.Equals(measured.Signature, note.Note.Text, StringComparison.Ordinal)
            ? (measured.Width, measured.Height)
            : MindMapGeometry.EstimateText(note.Note.Text, 12, bold: false);
    }

    private static int FontSizeOf(OmniMindMapNode node) => node.FontSize > 0 ? node.FontSize : OmniMindMapNode.DefaultFontSize;

    private static string NodeKey(string nodeId) => $"n:{nodeId}";

    private static string NoteKey(int index) => string.Create(CultureInfo.InvariantCulture, $"note:{index}");

    private string NodeElementId(string nodeId) => $"{BaseId}-node-{nodeId}";

    private bool IsSelected(string nodeId) =>
        string.Equals(_selectedId, nodeId, StringComparison.Ordinal) || _multi.Contains(nodeId);

    private IReadOnlyList<(int Index, OmniMindMapNote Note)> NotesOf(string nodeId) =>
        _notesByNode.TryGetValue(nodeId, out var notes) ? notes : [];

    private string NodeCss(OmniMindMapNode node) => CssClassBuilder.Combine(
    [
        "omni-mindmap__node",
        $"omni-mindmap__node--{OmniMindMapGroups.Resolve(node.Group)}",
        IsSelected(node.Id) ? "omni-mindmap__node--selected" : null,
        _linking && string.Equals(_linkSource, node.Id, StringComparison.Ordinal) ? "omni-mindmap__node--link-source" : null
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
        var notes = NotesOf(node.Id);
        return notes.Count == 0
            ? node.Label
            : Text("MindMapNodeWithNote", node.Label, string.Join(" ", notes.Select(note => note.Note.Text)));
    }

    private static string EdgePath((int Index, OmniMindMapNode From, OmniMindMapNode To) edge) =>
        MindMapGeometry.EdgePath(edge.From.X, edge.From.Y, edge.To.X, edge.To.Y);
}
