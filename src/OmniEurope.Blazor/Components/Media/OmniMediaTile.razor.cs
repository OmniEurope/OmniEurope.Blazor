using System.Globalization;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A live video tile: shows a <c>MediaStream</c> the application owns (a WebRTC peer, a screen share, a camera), with the
/// name of its owner, status icons, host actions, a full-screen button, and an optional drawing layer laid exactly over
/// the picture. Nothing leaves the browser: the stream and the strokes stay there, and the host transports the strokes
/// itself.
/// </summary>
/// <remarks>
/// <para>
/// The stream is a reference to a JavaScript <c>MediaStream</c> the application made in its own module (for instance
/// returned from a function of that module as an <see cref="IJSObjectReference"/>). The tile sets it as the
/// <c>srcObject</c> of its video and nothing else: it never stops, clones or adds a track, and it releases only its own
/// handle (disposing the tile clears <c>srcObject</c>). The stream, its tracks and the reference stay the application's to
/// stop and dispose, and the reference must stay alive while it is the <see cref="Stream"/> of a tile. Without a stream
/// the tile shows a disc with the initials of its owner, still with its name and status icons.
/// </para>
/// <para>
/// The video is <c>autoplay</c> and <c>playsinline</c> and silent by default (<see cref="Muted"/>), which lets it start
/// without a user gesture and keeps one's own stream from echoing; a host sets <see cref="Muted"/> to false for the stream
/// of a remote peer (the browser may then wait for the first press or key before it plays the sound).
/// </para>
/// <para>
/// The drawing layer (<see cref="Drawing"/>) is an <see cref="OmniInkCanvas"/> the tile's script places over the picture
/// the video really shows, from the size of the video and the element: in <see cref="OmniMediaFit.Contain"/> the picture
/// without the bands beside it, in <see cref="OmniMediaFit.Cover"/> the whole picture, cut off by the tile. A stroke
/// coordinate is 0 to 1 of that picture, so a stroke means the same on every tile size and a host can send it to a tile
/// of another size. The canvas events are passed on (<see cref="StrokesChanged"/>, <see cref="StrokeAdded"/>,
/// <see cref="StrokesRemoved"/>) and the strokes a host gives (<see cref="Strokes"/>) are drawn without raising them, as for
/// the canvas. The drawing toolbar is above the picture, not over it.
/// </para>
/// </remarks>
public partial class OmniMediaTile
{
    private const string ModulePath = Internal.OmniModules.MediaTile;
    private const string DefaultStrokeColor = "#ff3b30";
    private const string DefaultPickerColor = "#000000";

    private ElementReference _root;
    private OmniInkCanvas? _ink;
    private Task<IJSObjectReference?>? _moduleTask;
    private DotNetObjectReference<Internal.MediaTileInteropBridge>? _bridge;
    private IJSObjectReference? _appliedStream;
    private bool? _appliedMuted;
    private string? _appliedLayout;
    private IReadOnlyList<OmniInkStroke>? _seenStrokes;
    private IReadOnlyList<OmniInkStroke> _current = [];
    private OmniInkTool? _chosenTool;
    private OmniInkTool _seenTool;
    private string? _chosenColor;
    private string? _seenColor;
    private double? _chosenWidth;
    private double _seenWidth = double.NaN;
    private bool _fullscreen;
    private bool _attached;
    private bool _disposed;

    /// <summary>
    /// A reference to the JavaScript <c>MediaStream</c> to show, made by the application (in its own module) and kept by it:
    /// the tile sets it as the <c>srcObject</c> of its video, replaces it when this parameter changes and clears it when it
    /// becomes null, which shows the no-stream state (the initials on a disc). The tile never stops the tracks. Null by
    /// default.
    /// </summary>
    [Parameter]
    public IJSObjectReference? Stream { get; set; }

    /// <summary>
    /// The name of the stream's owner, in a label over the bottom of the video (on a translucent chip, so it reads on any
    /// video). It also gives the default <see cref="Label"/> and, without <see cref="Initials"/>, the initials of the
    /// no-stream state. Null, the default, shows no name.
    /// </summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>
    /// The letters on the disc of the no-stream state. Null, the default, takes the first letter of each of the first two
    /// words of <see cref="Name"/>, in upper case; with neither, the disc shows a person icon.
    /// </summary>
    [Parameter]
    public string? Initials { get; set; }

    /// <summary>Whether to show the muted-microphone icon (its owner is muted). False by default.</summary>
    [Parameter]
    public bool MicrophoneMuted { get; set; }

    /// <summary>Whether to show the screen-sharing icon (the stream is a shared screen). False by default.</summary>
    [Parameter]
    public bool ScreenSharing { get; set; }

    /// <summary>Whether to show the camera icon (the stream is a camera that is filming). False by default.</summary>
    [Parameter]
    public bool CameraActive { get; set; }

    /// <summary>
    /// Whether the video plays without sound. True by default: playback starts without a user gesture and one's own
    /// stream never echoes. A host sets it to false for a remote peer's stream.
    /// </summary>
    [Parameter]
    public bool Muted { get; set; } = true;

    /// <summary>
    /// How the picture fills the tile: <see cref="OmniMediaFit.Contain"/>, the default, shows all of it with bands where
    /// its ratio differs; <see cref="OmniMediaFit.Cover"/> fills the tile and cuts off the rest.
    /// </summary>
    [Parameter]
    public OmniMediaFit Fit { get; set; }

    /// <summary>
    /// Whether the full-screen button is shown in the action bar. True by default. <see cref="ToggleFullscreenAsync"/>
    /// works whatever this says.
    /// </summary>
    [Parameter]
    public bool ShowFullscreen { get; set; } = true;

    /// <summary>
    /// The host's buttons (capture, record, stop...), in the action bar over the top of the video, before the full-screen
    /// button. Null, the default, shows only the full-screen button.
    /// </summary>
    [Parameter]
    public RenderFragment? ActionsContent { get; set; }

    /// <summary>The accessible name of the tile. Null, the default, is the localized "Video of {name}" (or "Video" without a name).</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// Whether the drawing layer is shown and accepts strokes. Off by default. The layer is as large as the video picture
    /// and has no size while there is no stream or the picture has no size yet. With it off the strokes are neither shown
    /// nor part of an export.
    /// </summary>
    [Parameter]
    public bool Drawing { get; set; }

    /// <summary>
    /// The strokes of the drawing layer, the value of its <see cref="OmniInkCanvas"/> (see <see cref="OmniInkCanvas.Value"/>):
    /// coordinates 0 to 1 of the video picture. Empty by default; never null.
    /// </summary>
    [Parameter]
    public IReadOnlyList<OmniInkStroke> Strokes { get; set; } = [];

    /// <summary>Raised with the new list of strokes after the user draws, erases, undoes or clears (see <see cref="OmniInkCanvas.ValueChanged"/>).</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<OmniInkStroke>> StrokesChanged { get; set; }

    /// <summary>Raised with each stroke the user draws on this tile, never for <see cref="Strokes"/> the host gives (see <see cref="OmniInkCanvas.StrokeAdded"/>).</summary>
    [Parameter]
    public EventCallback<OmniInkStroke> StrokeAdded { get; set; }

    /// <summary>Raised with the strokes this tile removed (eraser, undo, clear), never for <see cref="Strokes"/> the host gives (see <see cref="OmniInkCanvas.StrokesRemoved"/>).</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<OmniInkStroke>> StrokesRemoved { get; set; }

    /// <summary>What the pointer does on the drawing layer: draw (<see cref="OmniInkTool.Pen"/>, the default) or erase whole strokes. The toolbar overrides it, raising <see cref="DrawingToolChanged"/>, until this parameter changes.</summary>
    [Parameter]
    public OmniInkTool DrawingTool { get; set; }

    /// <summary>Raised with the tool the user chose on the drawing toolbar.</summary>
    [Parameter]
    public EventCallback<OmniInkTool> DrawingToolChanged { get; set; }

    /// <summary>
    /// The colour of the next strokes, as a CSS colour. A bright red (<c>#ff3b30</c>) by default, which reads on most
    /// video. The toolbar colour field overrides it until this parameter changes.
    /// </summary>
    [Parameter]
    public string StrokeColor { get; set; } = DefaultStrokeColor;

    /// <summary>
    /// The width of the next strokes in CSS pixels at <see cref="OmniInkStroke.ReferenceWidth"/> (see <see cref="OmniInkCanvas.StrokeWidth"/>);
    /// above zero, 2 by default. The toolbar width slider overrides it until this parameter changes.
    /// </summary>
    [Parameter]
    public double StrokeWidth { get; set; } = 2;

    /// <summary>
    /// Whether the drawing toolbar (pen, eraser, colour, width, Undo, Clear) is shown while <see cref="Drawing"/> is on.
    /// True by default. It is a bar above the video, so it never covers the picture.
    /// </summary>
    [Parameter]
    public bool ShowDrawingToolbar { get; set; } = true;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    private string EffectiveLabel => !string.IsNullOrWhiteSpace(Label)
        ? Label
        : string.IsNullOrWhiteSpace(Name) ? Localize("MediaTileLabelUnnamed") : Localize("MediaTileLabel", Name);

    private string AvatarText => !string.IsNullOrWhiteSpace(Initials) ? Initials.Trim() : InitialsOf(Name);

    private OmniInkTool CurrentTool => _chosenTool ?? DrawingTool;

    private string PenColor => _chosenColor ?? StrokeColor;

    private string PickerColor => IsHexColor(PenColor) ? PenColor : DefaultPickerColor;

    private double PenWidth => _chosenWidth ?? StrokeWidth;

    /// <summary>
    /// Refuses a missing <see cref="Strokes"/> and a <see cref="StrokeWidth"/> that is not above zero, takes a list of strokes
    /// the host gives that differs from the one the tile holds, and drops the toolbar's tool, colour and width choices when
    /// the matching parameter changes.
    /// </summary>
    /// <exception cref="ArgumentNullException"><see cref="Strokes"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="StrokeWidth"/> is not a finite number above zero.</exception>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(Strokes);
        if (!double.IsFinite(StrokeWidth) || StrokeWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(StrokeWidth), StrokeWidth, $"{nameof(StrokeWidth)} must be a number above zero.");
        }

        if (!ReferenceEquals(Strokes, _seenStrokes))
        {
            _seenStrokes = Strokes;
            _current = Strokes;
        }

        if (DrawingTool != _seenTool)
        {
            _seenTool = DrawingTool;
            _chosenTool = null;
        }

        if (!string.Equals(StrokeColor, _seenColor, StringComparison.Ordinal))
        {
            _seenColor = StrokeColor;
            _chosenColor = null;
        }

        if (StrokeWidth != _seenWidth)
        {
            _seenWidth = StrokeWidth;
            _chosenWidth = null;
        }
    }

    /// <summary>
    /// Attaches the script on the first render, then gives it what changed since the last render: the sound, the stream, and
    /// the fit and drawing layer it places the overlay for. Does nothing once disposed, and ignores a lost circuit.
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
                _bridge = DotNetObjectReference.Create(new Internal.MediaTileInteropBridge(HandleFullscreenAsync));
                await module.InvokeVoidAsync("attach", _root, _bridge);
            }

            if (_appliedMuted != Muted)
            {
                _appliedMuted = Muted;
                await module.InvokeVoidAsync("setMuted", _root, Muted);
            }

            if (!ReferenceEquals(_appliedStream, Stream))
            {
                _appliedStream = Stream;
                await module.InvokeVoidAsync("setStream", _root, Stream);
            }

            var layout = $"{Fit}|{Drawing}";
            if (layout != _appliedLayout)
            {
                _appliedLayout = layout;
                await module.InvokeVoidAsync("refresh", _root);
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the tile with it.
        }
    }

    /// <summary>Detaches the script, clears the video's source (the stream and its tracks stay the application's) and releases the script and its .NET reference.</summary>
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
                    await module.InvokeVoidAsync("detach", _root);
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
    /// Makes the tile full screen, or brings it back. The browser may refuse (no user gesture, no support): the tile then
    /// stays as it is, and its button and label always show the real state. The state follows the browser, so leaving full
    /// screen with Escape is seen too.
    /// </summary>
    /// <returns>A task that completes when the browser has answered.</returns>
    /// <exception cref="ObjectDisposedException">The tile has been disposed.</exception>
    public async Task ToggleFullscreenAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var module = await ModuleAsync();
        ObjectDisposedException.ThrowIf(module is null, this);
        var now = await module!.InvokeAsync<bool>("toggleFullscreen", _root);
        await InvokeAsync(() =>
        {
            if (!_disposed && now != _fullscreen)
            {
                _fullscreen = now;
                StateHasChanged();
            }
        });
    }

    /// <summary>
    /// Draws the current video frame at its native size (<c>videoWidth</c> by <c>videoHeight</c>, scaled down with its ratio
    /// when a side is above 8192), then the strokes over it at the same relative positions and widths scaled like
    /// <see cref="OmniInkCanvas.ExportPngAsync"/> does, and returns the PNG file. The strokes are those of the drawing layer
    /// (none when <see cref="Drawing"/> is off); a stroke without an explicit colour is black. The image holds neither the
    /// name chip nor the icons nor the toolbar. The frame is read in the browser: nothing is sent anywhere.
    /// </summary>
    /// <param name="cancellationToken">Cancels the export, before or while the script draws; no partial image is returned.</param>
    /// <returns>The bytes of a PNG image.</returns>
    /// <exception cref="InvalidOperationException">There is no <see cref="Stream"/>, or its video has no frame yet (the picture is not decoded).</exception>
    /// <exception cref="ObjectDisposedException">The tile has been disposed.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    public async Task<byte[]> ExportPngAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        if (Stream is null)
        {
            throw new InvalidOperationException($"{nameof(OmniMediaTile)} has no {nameof(Stream)}: there is no video frame to export.");
        }

        // The shared script load is not cancelled with this call: only this caller stops waiting for it.
        var module = await ModuleAsync().WaitAsync(cancellationToken);
        // Null only when the tile was disposed while its script loaded.
        ObjectDisposedException.ThrowIf(module is null, this);
        var (colors, widths, points) = Internal.InkStrokeCodec.Pack(Drawing ? _current : []);
        var png = await module!.InvokeAsync<byte[]?>("exportPng", cancellationToken, _root, colors, widths, points);
        return png ?? throw new InvalidOperationException("The video has no frame to export yet: its first picture is not decoded.");
    }

    /// <summary>The PNG of <see cref="ExportPngAsync"/> as a <c>data:image/png;base64,</c> address, for an <c>img</c> source or a download link.</summary>
    /// <param name="cancellationToken">Cancels the export, before or while the script draws; no partial address is returned.</param>
    /// <returns>The data URL of the image.</returns>
    /// <exception cref="InvalidOperationException">There is no <see cref="Stream"/>, or its video has no frame yet.</exception>
    /// <exception cref="ObjectDisposedException">The tile has been disposed.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    public async Task<string> ExportDataUrlAsync(CancellationToken cancellationToken = default)
        => "data:image/png;base64," + Convert.ToBase64String(await ExportPngAsync(cancellationToken));

    // The full-screen state the script reports at every change, whoever caused it.
    internal Task HandleFullscreenAsync(bool fullscreen) => InvokeAsync(() =>
    {
        if (_disposed || fullscreen == _fullscreen)
        {
            return;
        }

        _fullscreen = fullscreen;
        StateHasChanged();
    });

    // The first letter of each of the first two words, upper case; empty without a word. A letter is a whole text
    // element, so an accent or an emoji is never cut in two.
    internal static string InitialsOf(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var letters = name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(word => new StringInfo(word).SubstringByTextElements(0, 1));
        return string.Concat(letters).ToUpperInvariant();
    }

    private Task HandleStrokesChanged(IReadOnlyList<OmniInkStroke> strokes)
    {
        _current = strokes;
        return StrokesChanged.InvokeAsync(strokes);
    }

    private Task SelectToolAsync(OmniInkTool tool)
    {
        if (tool == CurrentTool)
        {
            return Task.CompletedTask;
        }

        _chosenTool = tool;
        return DrawingToolChanged.InvokeAsync(tool);
    }

    private void ChooseColor(string? color) => _chosenColor = color;

    private void ChooseWidth(double width) => _chosenWidth = width;

    // The two buttons exist only while Drawing is on, and the same render gives the canvas its reference.
    private Task UndoAsync() => _ink!.UndoAsync();

    private Task ClearAsync() => _ink!.ClearAsync();

    private Task<IJSObjectReference?> ModuleAsync()
        => _moduleTask ??= Internal.OmniScriptModule.ImportAsync(JavaScript, ModulePath, () => _disposed);

    private static bool IsHexColor(string? value)
        => value is { Length: 7 } && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit);
}
