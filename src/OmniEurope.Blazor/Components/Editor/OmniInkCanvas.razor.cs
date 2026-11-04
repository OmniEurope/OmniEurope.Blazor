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
/// drawing, both announced. The toolbar (<see cref="ShowToolbar"/>) offers the colour and width of the next
/// strokes, Undo and Clear. <see cref="ReadOnly"/> keeps the drawing visible and focusable but draws nothing and
/// disables the toolbar; <see cref="Disabled"/> also removes the area from the tab order. The programmatic
/// methods (<see cref="UndoAsync"/>, <see cref="ClearAsync"/>) belong to the host and work in both states.
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
    private string? _chosenColor;
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

    /// <summary>Raised with the new list of strokes after a stroke, an undo or a clear.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<OmniInkStroke>> ValueChanged { get; set; }

    /// <summary>
    /// The colour of the next strokes, as a CSS colour. Null, the default, draws in the text colour of the theme
    /// (<see cref="OmniInkStroke.ThemeColor"/>). The toolbar colour field overrides it until this parameter changes.
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

    private string? PenColor => _chosenColor ?? Color;

    private string StrokeColor => PenColor ?? OmniInkStroke.ThemeColor;

    private string PickerColor => IsHexColor(PenColor) ? PenColor! : DefaultPickerColor;

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
            }
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
                _bridge = DotNetObjectReference.Create(new Internal.InkCanvasInteropBridge(HandleStrokeAsync, HandleUndoKeyAsync, HandleClearKeyAsync));
                await module.InvokeVoidAsync("attach", _canvas, _bridge);
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
        if (_moduleTask is not null)
        {
            try
            {
                var module = await _moduleTask;
                if (module is not null)
                {
                    await module.InvokeVoidAsync("detach", _canvas);
                    await module.DisposeAsync();
                }
            }
            catch (JSDisconnectedException)
            {
            }
        }

        _bridge?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Removes the last stroke and raises <see cref="ValueChanged"/> with the shorter list; does nothing when the
    /// drawing is empty. The host may call it whatever <see cref="ReadOnly"/> and <see cref="Disabled"/> say.
    /// </summary>
    /// <returns>A task that completes when the value has been raised.</returns>
    public Task UndoAsync() => InvokeAsync(async () =>
    {
        if (_strokes.Count == 0)
        {
            return;
        }

        await ReplaceAsync(_strokes.Take(_strokes.Count - 1).ToList(), Localize("InkUndone"));
    });

    /// <summary>
    /// Removes every stroke and raises <see cref="ValueChanged"/> with an empty list; does nothing when the drawing
    /// is already empty. The host may call it whatever <see cref="ReadOnly"/> and <see cref="Disabled"/> say.
    /// </summary>
    /// <returns>A task that completes when the value has been raised.</returns>
    public Task ClearAsync() => InvokeAsync(async () =>
    {
        if (_strokes.Count == 0)
        {
            return;
        }

        await ReplaceAsync([], Localize("InkCleared"));
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
    /// <returns>The bytes of a PNG image with an alpha channel, fully transparent where nothing is drawn.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> or <paramref name="height"/> is outside 1 to 8192.</exception>
    /// <exception cref="ObjectDisposedException">The canvas has been disposed.</exception>
    public async Task<byte[]> ExportPngAsync(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(width, MaxExportSide);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(height, MaxExportSide);
        ObjectDisposedException.ThrowIf(_disposed, this);
        var module = await ModuleAsync();
        // Null only when the canvas was disposed while its script loaded.
        ObjectDisposedException.ThrowIf(module is null, this);
        var (colors, widths, points) = Internal.InkStrokeCodec.Pack(_strokes);
        return await module!.InvokeAsync<byte[]>("exportPng", colors, widths, points, width, height);
    }

    /// <summary>The PNG of <see cref="ExportPngAsync"/> as a <c>data:image/png;base64,</c> address, for an <c>img</c> source or a download link.</summary>
    /// <param name="width">The width of the image in pixels, from 1 to 8192.</param>
    /// <param name="height">The height of the image in pixels, from 1 to 8192.</param>
    /// <returns>The data URL of the image.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> or <paramref name="height"/> is outside 1 to 8192.</exception>
    /// <exception cref="ObjectDisposedException">The canvas has been disposed.</exception>
    public async Task<string> ExportDataUrlAsync(int width, int height)
        => "data:image/png;base64," + Convert.ToBase64String(await ExportPngAsync(width, height));

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

        var next = new List<OmniInkStroke>(_strokes.Count + 1);
        next.AddRange(_strokes);
        next.Add(stroke);
        _strokes = next;
        await ValueChanged.InvokeAsync(next);
        StateHasChanged();
    });

    internal Task HandleUndoKeyAsync() => Locked || _disposed ? Task.CompletedTask : UndoAsync();

    internal Task HandleClearKeyAsync() => Locked || _disposed ? Task.CompletedTask : ClearAsync();

    private void ChooseColor(string? color) => _chosenColor = color;

    private void ChooseWidth(double width) => _chosenWidth = width;

    // The new list replaces the old one in the canvas and, once raised, in the host's binding; the script is told
    // to draw it, as the strokes no longer match what it holds.
    private async Task ReplaceAsync(IReadOnlyList<OmniInkStroke> next, string announcement)
    {
        _strokes = next;
        _pushPending = true;
        Announce(announcement);
        await ValueChanged.InvokeAsync(next);
        StateHasChanged();
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
