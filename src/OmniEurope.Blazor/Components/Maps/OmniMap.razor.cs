using System.Globalization;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A map of tiles in Web Mercator, drawn by the package itself, with lines (a route, in stretches of their own colour) and
/// markers (real buttons) over it. The reader pans it by dragging (mouse, pen or finger), zooms it with the wheel (around
/// the pointer), a double click, the + and - buttons or the keyboard, and the host can frame everything it drew
/// (<see cref="FitToContent"/>).
/// </summary>
/// <remarks>
/// <para>
/// The tiles are plain 256-pixel images read from <see cref="TileUrl"/>, OpenStreetMap's standard tiles by default; a host
/// under a strict Content Security Policy allows that server in <c>img-src</c>, and must follow its usage policy (for
/// OpenStreetMap: a visible attribution, which the map always shows, and moderate use). The tiles go out with the page's
/// origin as their referrer (<c>strict-origin-when-cross-origin</c>), which OpenStreetMap requires, even under a
/// <c>no-referrer</c> page policy. Only the tiles in view and one ring around them are in the page; a tile that fails to
/// load leaves the surface colour.
/// </para>
/// <para>
/// Nothing is written as a style attribute: the script places the tiles and the markers through custom properties of the
/// CSSOM and writes the paths of the lines as SVG geometry. The view state is bound like any other: <see cref="Center"/> and
/// <see cref="Zoom"/> with <see cref="CenterChanged"/> and <see cref="ZoomChanged"/>, raised when the reader moved the map.
/// Pan and zoom are never animated, so <c>prefers-reduced-motion</c> has nothing to calm. The height is the CSS variable
/// <c>--omni-map-height</c> (24rem by default), set by a host class.
/// </para>
/// <para>
/// <see cref="Lines"/> and <see cref="Markers"/> are read again when they receive another list: replace the list, do not
/// change it in place. The world does not repeat across the date line: a line or a marker is drawn on its own longitude only.
/// </para>
/// </remarks>
public partial class OmniMap
{
    /// <summary>The default tile server: OpenStreetMap's standard tiles.</summary>
    public const string DefaultTileUrl = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";

    private const int LowestZoom = 0;
    private const int HighestZoom = 22;
    private const double CasingWidth = 4;
    private const double MaxLineWidth = 40;
    private const double Tolerance = 1e-9;

    private readonly string _generatedId = $"omni-map-{Guid.NewGuid():N}";
    private ElementReference _root;
    private Task<IJSObjectReference?>? _moduleTask;
    private DotNetObjectReference<MapInteropBridge>? _bridge;
    private OmniPackageTooltips? _tooltips;
    private OmniGeoPoint _center;
    private int _zoom;
    private OmniGeoPoint? _seenCenter;
    private int _seenZoom;
    private IReadOnlyList<OmniMapLine>? _seenLines;
    private IReadOnlyList<OmniMapMarker>? _seenMarkers;
    private string? _seenOptions;
    private bool _seenFit;
    private string _tileUrl = DefaultTileUrl;
    private bool _pushOptions;
    private bool _pushContent;
    private bool _pushView;
    private bool _pushFit;
    private bool _attached;
    private bool _disposed;

    /// <summary>
    /// The point at the centre of the view, bound with <see cref="CenterChanged"/>. Brussels (50.8466, 4.3528) by default. A
    /// latitude beyond <see cref="OmniGeoPoint.MaxLatitude"/> is drawn on that limit and a longitude is brought back between
    /// -180 and 180. Overridden by <see cref="FitToContent"/> when the content is framed.
    /// </summary>
    [Parameter]
    public OmniGeoPoint Center { get; set; } = new(50.8466, 4.3528);

    /// <summary>Raised with the new centre after the reader moved the map (drag, wheel, double click, keys, zoom buttons) or the map framed its content.</summary>
    [Parameter]
    public EventCallback<OmniGeoPoint> CenterChanged { get; set; }

    /// <summary>
    /// The zoom level, bound with <see cref="ZoomChanged"/>: 0 shows the whole world in one tile, each level doubles the
    /// scale. 13 by default (a city); kept between <see cref="MinZoom"/> and <see cref="MaxZoom"/>.
    /// </summary>
    [Parameter]
    public int Zoom { get; set; } = 13;

    /// <summary>Raised with the new zoom level after the reader zoomed or the map framed its content.</summary>
    [Parameter]
    public EventCallback<int> ZoomChanged { get; set; }

    /// <summary>The lowest zoom level the reader can reach: 2 by default, brought between 0 and 22.</summary>
    [Parameter]
    public int MinZoom { get; set; } = 2;

    /// <summary>The highest zoom level the reader can reach: 19 by default (OpenStreetMap's last level), brought between 0 and 22.</summary>
    [Parameter]
    public int MaxZoom { get; set; } = 19;

    /// <summary>
    /// Whether the map frames every line and marker, with a margin, at the highest zoom that shows them all (a single point
    /// at zoom 16 at most), instead of <see cref="Center"/> and <see cref="Zoom"/>. Framed again when <see cref="Lines"/> or
    /// <see cref="Markers"/> receive another list; the reader stays free to move it in between. False by default.
    /// </summary>
    [Parameter]
    public bool FitToContent { get; set; }

    /// <summary>The lines drawn over the tiles, the first one at the bottom. Empty by default; never null.</summary>
    [Parameter]
    public IReadOnlyList<OmniMapLine> Lines { get; set; } = [];

    /// <summary>The markers drawn over the lines, the first one at the bottom and first in the tab order. Empty by default; never null.</summary>
    [Parameter]
    public IReadOnlyList<OmniMapMarker> Markers { get; set; } = [];

    /// <summary>Raised with the marker the reader pressed (click, or Enter and Space on its focused button).</summary>
    [Parameter]
    public EventCallback<OmniMapMarker> OnMarkerClick { get; set; }

    /// <summary>
    /// Raised with the place under the pointer when the reader clicks the map without dragging it (not on a marker or a
    /// button). Each click of a double click is reported too, before the double click zooms.
    /// </summary>
    [Parameter]
    public EventCallback<OmniGeoPoint> OnMapClick { get; set; }

    /// <summary>
    /// The address template of the tiles: an absolute <c>https://</c> or <c>http://</c> address, or a path rooted in
    /// <c>/</c>, with exactly the placeholders <c>{z}</c>, <c>{x}</c> and <c>{y}</c> (256-pixel Web Mercator tiles).
    /// <see cref="DefaultTileUrl"/> by default. Anything else raises <see cref="ArgumentException"/>.
    /// </summary>
    [Parameter]
    public string TileUrl { get; set; } = DefaultTileUrl;

    /// <summary>
    /// The attribution of the tiles, always shown in the bottom corner of the map. Null, the default, shows the localized
    /// "© OpenStreetMap contributors" as a link to https://www.openstreetmap.org/copyright; a host using another tile server
    /// gives that server's attribution here, shown as text.
    /// </summary>
    [Parameter]
    public string? Attribution { get; set; }

    /// <summary>The accessible name of the map region. Null, the default, is the localized "Map".</summary>
    [Parameter]
    public string? Label { get; set; }

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    private int EffectiveMinZoom => Math.Clamp(MinZoom, LowestZoom, HighestZoom);

    private int EffectiveMaxZoom => Math.Clamp(MaxZoom, LowestZoom, HighestZoom);

    private string BaseId => Id ?? _generatedId;

    private string HelpId => $"{BaseId}-help";

    private string LinesId => $"{BaseId}-lines";

    private bool HasLineTexts => Lines.Any(line => !string.IsNullOrWhiteSpace(line.Tooltip));

    private string DescribedBy => HasLineTexts ? $"{HelpId} {LinesId}" : HelpId;

    /// <summary>
    /// Checks the parameters, then notes what the script must be told after the render: the tile options, the content, a
    /// new view the host asked for, or a new framing of the content.
    /// </summary>
    /// <exception cref="ArgumentNullException"><see cref="Lines"/> or <see cref="Markers"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// <see cref="TileUrl"/> is not a valid template, <see cref="MinZoom"/> is above <see cref="MaxZoom"/>, a point is not a
    /// pair of numbers, a line has fewer than two points or a width that is not above zero and at most 40, or a marker has
    /// no label.
    /// </exception>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(Lines);
        ArgumentNullException.ThrowIfNull(Markers);
        if (EffectiveMinZoom > EffectiveMaxZoom)
        {
            throw new ArgumentException($"OmniMap.MinZoom ({MinZoom}) is above OmniMap.MaxZoom ({MaxZoom}).", nameof(MinZoom));
        }

        _tileUrl = MapInput.TileUrl(TileUrl);
        EnsurePoint(Center, nameof(Center));
        ValidateContent();

        var options = $"{_tileUrl}|{EffectiveMinZoom}|{EffectiveMaxZoom}";
        if (_seenCenter is null)
        {
            _center = Center.ToMercatorRange();
            _zoom = ClampZoom(Zoom);
            _pushOptions = _pushContent = _pushView = true;
            _pushFit = FitToContent;
        }
        else
        {
            NoteChanges(options);
        }

        _seenOptions = options;
        _seenCenter = Center;
        _seenZoom = Zoom;
        _seenLines = Lines;
        _seenMarkers = Markers;
        _seenFit = FitToContent;
    }

    /// <summary>
    /// Attaches the script on the first render and turns the package tooltips on, then tells the script what changed: the
    /// tile options, the lines and markers to place, the view or a framing of the content. Does nothing once disposed, and
    /// ignores a lost circuit.
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
                _bridge = DotNetObjectReference.Create(new MapInteropBridge(HandleViewChangedAsync, HandleMapClickAsync));
                await module.InvokeVoidAsync("attach", _root, _bridge);
                _tooltips = new OmniPackageTooltips(JavaScript);
                await _tooltips.InstallAsync();
            }

            if (_pushOptions || _pushContent || _pushView || _pushFit)
            {
                // The options always go (three values); the content and the view only when they changed, else null.
                var lines = _pushContent ? Lines.Select(Flatten).ToArray() : null;
                var markers = _pushContent ? Markers.Select(marker => new[] { marker.Point.Latitude, marker.Point.Longitude }).ToArray() : null;
                double[]? view = _pushView ? [_center.Latitude, _center.Longitude, _zoom] : null;
                var fit = _pushFit;
                _pushOptions = _pushContent = _pushView = _pushFit = false;
                await module.InvokeVoidAsync("update", _root, _tileUrl, EffectiveMinZoom, EffectiveMaxZoom, lines, markers, view, fit);
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the map with it.
        }
    }

    /// <summary>Zooms in by one level around the centre of the view, as the + button does.</summary>
    /// <returns>A task that completes when the script moved the view; <see cref="ZoomChanged"/> follows.</returns>
    /// <exception cref="ObjectDisposedException">The map has been disposed.</exception>
    public Task ZoomInAsync() => ZoomByAsync(1);

    /// <summary>Zooms out by one level around the centre of the view, as the - button does.</summary>
    /// <returns>A task that completes when the script moved the view; <see cref="ZoomChanged"/> follows.</returns>
    /// <exception cref="ObjectDisposedException">The map has been disposed.</exception>
    public Task ZoomOutAsync() => ZoomByAsync(-1);

    /// <summary>Detaches the script (its listeners and tiles go with it) and releases the script, the tooltips and the .NET reference.</summary>
    /// <returns>A task that completes when the script is released.</returns>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        await OmniScriptModule.DetachAndReleaseAsync(_moduleTask, _root);

        if (_tooltips is not null)
        {
            await _tooltips.DisposeAsync();
        }

        _bridge?.Dispose();
        GC.SuppressFinalize(this);
    }

    // The view the script moved to, whoever moved it: kept, then told to the host when it changed.
    internal Task HandleViewChangedAsync(double latitude, double longitude, int zoom) => InvokeAsync(async () =>
    {
        if (_disposed)
        {
            return;
        }

        var center = new OmniGeoPoint(latitude, longitude);
        var centerMoved = !Same(center, _center);
        var zoomMoved = zoom != _zoom;
        _center = center;
        _zoom = zoom;
        if (!centerMoved && !zoomMoved)
        {
            return;
        }

        StateHasChanged();
        if (centerMoved)
        {
            await CenterChanged.InvokeAsync(center);
        }

        if (zoomMoved)
        {
            await ZoomChanged.InvokeAsync(zoom);
        }
    });

    // A click on the map that was not a drag, at the place under the pointer.
    internal Task HandleMapClickAsync(double latitude, double longitude)
        => _disposed ? Task.CompletedTask : InvokeAsync(() => OnMapClick.InvokeAsync(new OmniGeoPoint(latitude, longitude)));

    // A length for an SVG attribute, written with a dot whatever the culture.
    internal static string Pixels(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    internal static OmniIconName IconOf(OmniMapMarker marker) => marker.Icon ?? marker.Kind switch
    {
        OmniMapMarkerKind.Start => OmniIconName.Play,
        OmniMapMarkerKind.Waypoint => OmniIconName.Circle,
        OmniMapMarkerKind.End => OmniIconName.Flag,
        _ => OmniIconName.MapPin
    };

    private static string LineClass(OmniMapLine line)
        => CssClassBuilder.Combine(["omni-map__line", $"omni-map__line--{line.Tone.ToString().ToLowerInvariant()}", line.Class]);

    private static string MarkerClass(OmniMapMarker marker)
        => $"omni-map__marker omni-map__marker--{marker.Kind.ToString().ToLowerInvariant()}";

    private static string? ColorOf(OmniMapLine line) => MapInput.IsColor(line.Color) ? line.Color!.Trim() : null;

    private static double[] Flatten(OmniMapLine line)
    {
        var values = new double[line.Points.Count * 2];
        for (var index = 0; index < line.Points.Count; index++)
        {
            values[index * 2] = line.Points[index].Latitude;
            values[index * 2 + 1] = line.Points[index].Longitude;
        }

        return values;
    }

    private static bool Same(OmniGeoPoint first, OmniGeoPoint second)
        => Math.Abs(first.Latitude - second.Latitude) < Tolerance && Math.Abs(first.Longitude - second.Longitude) < Tolerance;

    private static void EnsurePoint(OmniGeoPoint point, string what)
    {
        if (!point.IsFinite)
        {
            throw new ArgumentException($"OmniMap: {what} must have a numeric latitude and longitude, not ({point.Latitude}, {point.Longitude}).", what);
        }
    }

    // After the first set: what the host changed since the last one, and what the script must be told for it.
    private void NoteChanges(string options)
    {
        if (options != _seenOptions)
        {
            _pushOptions = true;
            var clamped = ClampZoom(_zoom);
            _pushView |= clamped != _zoom;
            _zoom = clamped;
        }

        if (!Same(Center, _seenCenter!.Value) || Zoom != _seenZoom)
        {
            var center = Center.ToMercatorRange();
            var zoom = ClampZoom(Zoom);
            if (!Same(center, _center) || zoom != _zoom)
            {
                _center = center;
                _zoom = zoom;
                _pushView = true;
            }
        }

        if (!ReferenceEquals(Lines, _seenLines) || !ReferenceEquals(Markers, _seenMarkers))
        {
            _pushContent = true;
            _pushFit |= FitToContent;
        }

        _pushFit |= FitToContent && !_seenFit;
    }

    private void ValidateContent()
    {
        for (var index = 0; index < Lines.Count; index++)
        {
            var line = Lines[index] ?? throw new ArgumentException($"OmniMap: line {index} is null.", nameof(Lines));
            if (line.Points is null || line.Points.Count < 2)
            {
                throw new ArgumentException($"OmniMap: line {index} must have two points at least.", nameof(Lines));
            }

            if (!double.IsFinite(line.Width) || line.Width <= 0 || line.Width > MaxLineWidth)
            {
                throw new ArgumentException($"OmniMap: the width of line {index} must be above zero and at most {MaxLineWidth}, not {line.Width}.", nameof(Lines));
            }

            foreach (var point in line.Points)
            {
                EnsurePoint(point, $"{nameof(Lines)}[{index}]");
            }
        }

        for (var index = 0; index < Markers.Count; index++)
        {
            var marker = Markers[index] ?? throw new ArgumentException($"OmniMap: marker {index} is null.", nameof(Markers));
            if (string.IsNullOrWhiteSpace(marker.Label))
            {
                throw new ArgumentException($"OmniMap: marker {index} must have a label, its accessible name.", nameof(Markers));
            }

            EnsurePoint(marker.Point, $"{nameof(Markers)}[{index}]");
        }
    }

    private int ClampZoom(int zoom) => Math.Clamp(zoom, EffectiveMinZoom, EffectiveMaxZoom);

    private async Task ZoomByAsync(int delta)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var module = await ModuleAsync();
        ObjectDisposedException.ThrowIf(module is null, this);
        try
        {
            await module!.InvokeVoidAsync("zoomBy", _root, delta);
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone: there is no view left to zoom.
        }
    }

    private Task<IJSObjectReference?> ModuleAsync()
        => _moduleTask ??= OmniScriptModule.ImportAsync(JavaScript, OmniModules.Map, () => _disposed);
}
