namespace OmniEurope.Blazor.Components;

/// <summary>
/// A freehand drawing pad: the pointer (mouse, pen with its pressure, finger) draws lines on a canvas, which
/// are kept as a list of <see cref="OmniInkStroke"/> and can be exported as a transparent PNG.
/// </summary>
/// <remarks>
/// <para>
/// The list is the value (<see cref="Value"/> and <see cref="ValueChanged"/>): the canvas replaces it with a new
/// list for every stroke, undo and clear, never changes one it was given, and redraws when the host gives it
/// another. Points are relative to the drawing area and widths to its width
/// (<see cref="OmniInkStroke.ReferenceWidth"/>), so a drawing looks the same at any size and survives a resize
/// or a change of theme (a stroke without an explicit colour follows the text colour of the theme).
/// </para>
/// <para>
/// Keyboard: the drawing area takes the focus; Ctrl+Z (Cmd+Z) undoes the last stroke and Delete clears the
/// drawing, both announced. The toolbar (<see cref="ShowToolbar"/>) offers the pen and the eraser
/// (<see cref="Tool"/>), the colour and width of the next strokes, Undo and Clear. <see cref="ReadOnly"/> keeps the drawing visible and focusable but draws nothing and
/// disables the toolbar; <see cref="Disabled"/> also removes the area from the tab order. The programmatic
/// methods (<see cref="UndoAsync"/>, <see cref="ClearAsync"/>) belong to the host and work in both states.
/// </para>
/// <para>
/// A shared drawing: the canvas transports nothing, the host does. <see cref="StrokeAdded"/> and
/// <see cref="StrokesRemoved"/> report what the user did on this canvas (never what the host gave through
/// <see cref="Value"/>), so the host can send it to the other browsers and give them back as a new
/// <see cref="Value"/>. Every stroke drawn here has an <see cref="OmniInkStroke.Id"/>, and Undo first removes the last
/// stroke this canvas drew that is still in the drawing, so a user does not undo a stroke someone else drew after theirs.
/// The drawing surface paints no background of its own (transparent between strokes); the frame around it takes its
/// background and border colour from custom properties a parent can override (see the guide,
/// <c>--omni-ink-background</c>), so the canvas can lie over a picture or a video.
/// </para>
/// </remarks>
public partial class OmniInkCanvas
{
    private const string ModulePath = Internal.OmniModules.InkCanvas;
    private const int MaxExportSide = 8192;
    private const string DefaultPickerColor = "#000000";

    private readonly string _generatedId = $"omni-ink-canvas-{Guid.NewGuid():N}";
    private ElementReference _root;
    private ElementReference _canvas;
    private Task<IJSObjectReference?>? _moduleTask;
    private DotNetObjectReference<Internal.InkCanvasInteropBridge>? _bridge;
    private IReadOnlyList<OmniInkStroke>? _seenValue;
    private IReadOnlyList<OmniInkStroke> _strokes = [];
    private string? _seenColor;
    private double _seenStrokeWidth = double.NaN;
    private readonly HashSet<string> _ownIds = [];
    private OmniInkTool? _chosenTool;
    private OmniInkTool _seenTool;
    private string? _chosenColor;
    private string? _themeColor;
    private double? _chosenWidth;
    private string? _appliedLayout;
    private string _announcement = string.Empty;
    private bool _announceToggle;
    private bool _pushPending = true;
    private bool _attached;
    private bool _disposed;

    /// <summary>
    /// The strokes of the drawing. The canvas never changes the list: every stroke, undo and clear raises
    /// <see cref="ValueChanged"/> with a new one, and a list the host gives that is not the one the canvas last
    /// raised is drawn instead of the current drawing. Empty by default; never null.
    /// </summary>
    [Parameter]
    public IReadOnlyList<OmniInkStroke> Value { get; set; } = [];

    /// <summary>Raised with the new list of strokes after a stroke, an erasure, an undo or a clear.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<OmniInkStroke>> ValueChanged { get; set; }

    /// <summary>
    /// Raised with each stroke the user draws on this canvas, after <see cref="ValueChanged"/> (the stroke is the last
    /// of the new list, with its <see cref="OmniInkStroke.Id"/>). Never raised for strokes in a <see cref="Value"/> the
    /// host gives, nor for a refused stroke (a locked canvas), so a host can send exactly the user&apos;s strokes to others.
    /// </summary>
    [Parameter]
    public EventCallback<OmniInkStroke> StrokeAdded { get; set; }

    /// <summary>
    /// Raised with the strokes this canvas removed, in drawing order, after <see cref="ValueChanged"/>: by the eraser, by
    /// Undo (Ctrl+Z, toolbar or <see cref="UndoAsync"/>) and by Clear (Delete, toolbar or <see cref="ClearAsync"/>). Never raised
    /// for strokes that disappear because the host gave another <see cref="Value"/>.
    /// </summary>
    [Parameter]
    public EventCallback<IReadOnlyList<OmniInkStroke>> StrokesRemoved { get; set; }

    /// <summary>
    /// What the pointer does: <see cref="OmniInkTool.Pen"/>, the default, draws; <see cref="OmniInkTool.Eraser"/>
    /// removes every whole stroke the pointer path touches, in one change raised when the pointer is released. The
    /// toolbar pen and eraser buttons override it, raising <see cref="ToolChanged"/>, until this parameter changes.
    /// <see cref="ReadOnly"/> and <see cref="Disabled"/> block the eraser like the pen. The keyboard is unaffected.
    /// </summary>
    [Parameter]
    public OmniInkTool Tool { get; set; }

    /// <summary>Raised with the tool the user chose on the toolbar.</summary>
    [Parameter]
    public EventCallback<OmniInkTool> ToolChanged { get; set; }

    /// <summary>
    /// The colour of the next strokes, as a CSS colour. Null, the default, draws in the text colour of the theme
    /// (<see cref="OmniInkStroke.ThemeColor"/>), and the toolbar colour field then shows that colour as the browser
    /// resolved it on the canvas (read when the script attaches, and again when the script sees the theme attribute, the
    /// style or class of the theme scope or the system colour scheme change); it is black until then, for instance
    /// before the first render in the browser. The toolbar colour field overrides it until this parameter changes.
    /// </summary>
    [Parameter]
    public string? Color { get; set; }

    /// <summary>
    /// The width of the next strokes in CSS pixels at <see cref="OmniInkStroke.ReferenceWidth"/> (see
    /// <see cref="OmniInkStroke"/>); above zero, 2 by default. The toolbar width slider overrides it until this
    /// parameter changes.
    /// </summary>
    [Parameter]
    public double StrokeWidth { get; set; } = 2;

    /// <summary>
    /// The width of the drawing area as a CSS length (<c>30rem</c>, <c>480px</c>, <c>100%</c>). Null, the default,
    /// fills the width of the parent (100%).
    /// </summary>
    [Parameter]
    public string? Width { get; set; }

    /// <summary>
    /// The height of the drawing area as a CSS length (<c>12rem</c>, <c>200px</c>, <c>40vh</c>). Null, the default,
    /// is 12rem. Ignored when <see cref="AspectRatio"/> is set.
    /// </summary>
    [Parameter]
    public string? Height { get; set; }

    /// <summary>
    /// The width divided by the height of the drawing area (3 for a signature pad three times wider than high),
    /// which then follows its width. Null, the default, uses <see cref="Height"/>.
    /// </summary>
    [Parameter]
    public double? AspectRatio { get; set; }

    /// <summary>Whether the canvas is disabled: nothing is drawn, the toolbar is disabled and the area leaves the tab order. Off by default.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Whether the drawing can be seen and focused but not changed: no stroke, undo or clear from the pointer, the
    /// keyboard or the toolbar. Off by default.
    /// </summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Whether the toolbar (colour, width, Undo, Clear) is shown above the drawing area. True by default.</summary>
    [Parameter]
    public bool ShowToolbar { get; set; } = true;

    /// <summary>The accessible name of the drawing area. Null, the default, is the localized "Drawing area".</summary>
    [Parameter]
    public string? Label { get; set; }

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    private string BaseId => Id ?? _generatedId;

    private string HelpId => $"{BaseId}-help";

    private string EffectiveLabel => LocalizeOr(Label, "InkCanvasLabel");

    private bool Locked => Disabled || ReadOnly;

    private bool CanUndo => !Locked && _strokes.Count > 0;

    private OmniInkTool CurrentTool => _chosenTool ?? Tool;

    private string? PenColor => _chosenColor ?? Color;

    private string StrokeColor => PenColor ?? OmniInkStroke.ThemeColor;

    private string PickerColor => IsHexColor(PenColor) ? PenColor! : PenColor is null && _themeColor is not null ? _themeColor : DefaultPickerColor;

    private double PenWidth => _chosenWidth ?? StrokeWidth;

    /// <summary>
    /// Refuses a missing <see cref="Value"/>, a <see cref="StrokeWidth"/> or <see cref="AspectRatio"/> that is not
    /// above zero, and a <see cref="Width"/> or <see cref="Height"/> that is not a number followed by a unit, then
    /// takes a list the host gives that differs from the one the canvas holds, and drops the toolbar's colour and
    /// width choices when the matching parameter changes.
    /// </summary>
    /// <exception cref="ArgumentNullException"><see cref="Value"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="StrokeWidth"/> or <see cref="AspectRatio"/> is not a finite number above zero.</exception>
    /// <exception cref="ArgumentException"><see cref="Width"/> or <see cref="Height"/> is not an accepted CSS length.</exception>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(Value);
        if (!double.IsFinite(StrokeWidth) || StrokeWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(StrokeWidth), StrokeWidth, $"{nameof(StrokeWidth)} must be a number above zero.");
        }

        if (AspectRatio is { } ratio && (!double.IsFinite(ratio) || ratio <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(AspectRatio), ratio, $"{nameof(AspectRatio)} must be a number above zero.");
        }

        if (Width is not null && !Internal.CssLength.IsWidth(Width, positive: true))
        {
            throw new ArgumentException($"'{Width}' is not a CSS length {nameof(OmniInkCanvas)} accepts for {nameof(Width)} (a number above zero followed by px, rem, em, ch, vw or %).", nameof(Width));
        }

        if (Height is not null && !Internal.CssLength.IsHeight(Height))
        {
            throw new ArgumentException($"'{Height}' is not a CSS length {nameof(OmniInkCanvas)} accepts for {nameof(Height)} (a number followed by px, rem, em, vh or %).", nameof(Height));
        }

        if (!ReferenceEquals(Value, _seenValue))
        {
            _seenValue = Value;
            if (!ReferenceEquals(Value, _strokes))
            {
                _strokes = Value;
                _pushPending = true;
                ForgetGoneStrokes();
            }
        }

        if (Tool != _seenTool)
        {
            _seenTool = Tool;
            _chosenTool = null;
        }

        if (!string.Equals(Color, _seenColor, StringComparison.Ordinal))
        {
            _seenColor = Color;
            _chosenColor = null;
        }

        if (StrokeWidth != _seenStrokeWidth)
        {
            _seenStrokeWidth = StrokeWidth;
            _chosenWidth = null;
        }
    }

    /// <summary>
    /// Attaches the pointer and keyboard handling on the first render, then gives the script the layout when it
    /// changed and the strokes when the host replaced them. Does nothing once disposed, and ignores a lost circuit.
    /// </summary>
    /// <param name="firstRender">True on the first render.</param>
    /// <returns>A task that completes when the script calls are done.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            var module = await ModuleAsync();
            if (module is null)
            {
                return;
            }

            if (!_attached)
            {
                _attached = true;
                _bridge = DotNetObjectReference.Create(new Internal.InkCanvasInteropBridge(HandleStrokeAsync, HandleUndoKeyAsync, HandleClearKeyAsync, HandleThemeColorAsync, HandleEraseAsync));
                var themeColor = await module.InvokeAsync<string?>("attach", _canvas, _bridge);
                if (IsHexColor(themeColor) && themeColor != _themeColor)
                {
                    _themeColor = themeColor;
                    StateHasChanged();
                }
            }

            var layout = $"{Width}|{Height}|{AspectRatio?.ToString(CultureInfo.InvariantCulture)}";
            if (layout != _appliedLayout)
            {
                _appliedLayout = layout;
                await module.InvokeVoidAsync("setLayout", _root, Width, Height, AspectRatio);
            }

            if (_pushPending)
            {
                _pushPending = false;
                var (colors, widths, points) = Internal.InkStrokeCodec.Pack(_strokes);
                await module.InvokeVoidAsync("setStrokes", _canvas, colors, widths, points);
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the canvas with it.
        }
    }

    /// <summary>Detaches and releases the canvas script and its .NET reference.</summary>
    /// <returns>A task that completes when the script is released.</returns>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        await Internal.OmniScriptModule.DetachAndReleaseAsync(_moduleTask, _canvas);

        _bridge?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Removes the last stroke this canvas drew that is still in the drawing (the last stroke of the drawing when
    /// this canvas drew none of them, as with a single user), raises <see cref="ValueChanged"/> with the shorter list
    /// then <see cref="StrokesRemoved"/> with that stroke; does nothing when the drawing is empty. The host may call
    /// it whatever <see cref="ReadOnly"/> and <see cref="Disabled"/> say.
    /// </summary>
    /// <returns>A task that completes when the value has been raised.</returns>
    public Task UndoAsync() => InvokeAsync(async () =>
    {
        if (_strokes.Count == 0)
        {
            return;
        }

        await RemoveAsync([LastOwnIndex()], Localize("InkUndone"));
    });

    /// <summary>
    /// Removes every stroke and raises <see cref="ValueChanged"/> with an empty list then <see cref="StrokesRemoved"/>
    /// with all the strokes removed; does nothing when the drawing is already empty. The host may call it whatever
    /// <see cref="ReadOnly"/> and <see cref="Disabled"/> say.
    /// </summary>
    /// <returns>A task that completes when the value has been raised.</returns>
    public Task ClearAsync() => InvokeAsync(async () =>
    {
        if (_strokes.Count == 0)
        {
            return;
        }

        await RemoveAsync(Enumerable.Range(0, _strokes.Count).ToArray(), Localize("InkCleared"));
    });

    /// <summary>
    /// Draws the strokes on a transparent background and returns the PNG file. The strokes are laid out over
    /// <paramref name="width"/> by <paramref name="height"/> pixels from their relative positions, and a line is
    /// <c>width / </c><see cref="OmniInkStroke.ReferenceWidth"/> times the width of its stroke: give the aspect ratio of
    /// the drawing area to keep its proportions. A stroke without an explicit colour is black. The image contains
    /// the strokes of the canvas, not its toolbar or border.
    /// </summary>
    /// <param name="width">The width of the image in pixels, from 1 to 8192.</param>
    /// <param name="height">The height of the image in pixels, from 1 to 8192.</param>
    /// <param name="cancellationToken">Cancels the export, before or while the script draws; no partial image is returned.</param>
    /// <returns>The bytes of a PNG image with an alpha channel, fully transparent where nothing is drawn.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> or <paramref name="height"/> is outside 1 to 8192.</exception>
    /// <exception cref="ObjectDisposedException">The canvas has been disposed.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    public async Task<byte[]> ExportPngAsync(int width, int height, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(width, MaxExportSide);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(height, MaxExportSide);
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        // The shared script load is not cancelled with this call: only this caller stops waiting for it.
        var module = await ModuleAsync().WaitAsync(cancellationToken);
        // Null only when the canvas was disposed while its script loaded.
        ObjectDisposedException.ThrowIf(module is null, this);
        var (colors, widths, points) = Internal.InkStrokeCodec.Pack(_strokes);
        return await module!.InvokeAsync<byte[]>("exportPng", cancellationToken, colors, widths, points, width, height);
    }

    /// <summary>The PNG of <see cref="ExportPngAsync"/> as a <c>data:image/png;base64,</c> address, for an <c>img</c> source or a download link.</summary>
    /// <param name="width">The width of the image in pixels, from 1 to 8192.</param>
    /// <param name="height">The height of the image in pixels, from 1 to 8192.</param>
    /// <param name="cancellationToken">Cancels the export, before or while the script draws; no partial address is returned.</param>
    /// <returns>The data URL of the image.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> or <paramref name="height"/> is outside 1 to 8192.</exception>
    /// <exception cref="ObjectDisposedException">The canvas has been disposed.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    public async Task<string> ExportDataUrlAsync(int width, int height, CancellationToken cancellationToken = default)
        => "data:image/png;base64," + Convert.ToBase64String(await ExportPngAsync(width, height, cancellationToken));

    /// <summary>A stroke the script finished drawing: it is already on the canvas, so only the value is updated.</summary>
    internal Task HandleStrokeAsync(string color, double width, double[] points) => InvokeAsync(async () =>
    {
        if (_disposed)
        {
            return;
        }

        if (Locked || !Internal.InkStrokeCodec.TryUnpack(color, width, points, out var stroke))
        {
            // The script drew something the value does not hold: it gets the current strokes back.
            _pushPending = true;
            StateHasChanged();
            return;
        }

        stroke = stroke with { Id = Guid.NewGuid().ToString("N") };
        _ownIds.Add(stroke.Id!);
        var next = new List<OmniInkStroke>(_strokes.Count + 1);
        next.AddRange(_strokes);
        next.Add(stroke);
        _strokes = next;
        await ValueChanged.InvokeAsync(next);
        await StrokeAdded.InvokeAsync(stroke);
        StateHasChanged();
    });

    // The eraser touched these strokes of the list the script holds, count strokes long. A list that is not the one
    // held here (the host gave another in the meantime), a locked canvas or an index out of range removes nothing, and
    // the script gets the current strokes back.
    internal Task HandleEraseAsync(int[] indices, int count) => InvokeAsync(async () =>
    {
        if (_disposed)
        {
            return;
        }

        var touched = indices?.Distinct().Order().ToArray() ?? [];
        if (Locked || count != _strokes.Count || touched.Length == 0 || touched[0] < 0 || touched[^1] >= _strokes.Count)
        {
            _pushPending = true;
            StateHasChanged();
            return;
        }

        await RemoveAsync(touched, Localize("InkErased"));
    });

    internal Task HandleUndoKeyAsync() => Locked || _disposed ? Task.CompletedTask : UndoAsync();

    internal Task HandleClearKeyAsync() => Locked || _disposed ? Task.CompletedTask : ClearAsync();

    // The script reports the theme colour it resolved on the canvas when it changes (theme attribute, scope style or
    // class, system colour scheme); the colour field shows it while the pen is the theme colour.
    internal Task HandleThemeColorAsync(string color)
    {
        if (_disposed || !IsHexColor(color) || color == _themeColor)
        {
            return Task.CompletedTask;
        }

        _themeColor = color;
        return InvokeAsync(StateHasChanged);
    }

    private Task SelectToolAsync(OmniInkTool tool)
    {
        if (tool == CurrentTool)
        {
            return Task.CompletedTask;
        }

        _chosenTool = tool;
        return ToolChanged.InvokeAsync(tool);
    }

    private void ChooseColor(string? color) => _chosenColor = color;

    private void ChooseWidth(double width) => _chosenWidth = width;

    // The strokes at these indices (ascending, distinct, in range) leave the drawing: the new list replaces the old
    // one in the canvas and, once raised, in the host's binding; the script is told to draw it, as the strokes no
    // longer match what it holds. The removed strokes are reported after the value.
    private async Task RemoveAsync(int[] indices, string announcement)
    {
        var removed = indices.Select(index => _strokes[index]).ToList();
        var kept = new List<OmniInkStroke>(_strokes.Count - removed.Count);
        for (var index = 0; index < _strokes.Count; index++)
        {
            if (Array.BinarySearch(indices, index) < 0)
            {
                kept.Add(_strokes[index]);
            }
        }

        _strokes = kept;
        _pushPending = true;
        Announce(announcement);
        foreach (var stroke in removed)
        {
            if (stroke.Id is not null)
            {
                _ownIds.Remove(stroke.Id);
            }
        }

        await ValueChanged.InvokeAsync(kept);
        await StrokesRemoved.InvokeAsync(removed);
        StateHasChanged();
    }

    // The last stroke this canvas drew that the drawing still holds; the last stroke when there is none.
    private int LastOwnIndex()
    {
        for (var index = _strokes.Count - 1; index >= 0; index--)
        {
            if (_strokes[index].Id is { } id && _ownIds.Contains(id))
            {
                return index;
            }
        }

        return _strokes.Count - 1;
    }

    // A list the host gave replaces the drawing: the ids of strokes drawn here that it no longer holds are forgotten.
    private void ForgetGoneStrokes()
    {
        if (_ownIds.Count > 0)
        {
            _ownIds.IntersectWith(_strokes.Where(stroke => stroke.Id is not null).Select(stroke => stroke.Id!));
        }
    }

    private Task<IJSObjectReference?> ModuleAsync()
        => _moduleTask ??= Internal.OmniScriptModule.ImportAsync(JavaScript, ModulePath, () => _disposed);

    private void Announce(string message)
    {
        _announceToggle = !_announceToggle;
        _announcement = _announceToggle ? message : message + ' ';
    }

    private static bool IsHexColor(string? value)
        => value is { Length: 7 } && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit);
}
