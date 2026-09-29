using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Where an <see cref="OmniMindMap"/> is looked at from: the pan and zoom, the canvas size, and the
/// moves of the view (fit, zoom, centre, a resized canvas), each reported through
/// <see cref="OmniMindMap.ViewStateChanged"/>.
/// </summary>
internal sealed class MindMapViewport(OmniMindMap owner, MindMapModel model, MindMapLabelSizes sizes)
{
    private const double FitMaxZoom = 2;
    private const double FitPadding = 60;

    /// <summary>Horizontal offset of the map on the canvas, in pixels.</summary>
    public double PanX { get; private set; }

    /// <summary>Vertical offset of the map on the canvas, in pixels.</summary>
    public double PanY { get; private set; }

    /// <summary>Scale of the map on the canvas.</summary>
    public double Zoom { get; private set; } = 1;

    /// <summary>Width of the canvas in pixels.</summary>
    public double CanvasWidth { get; private set; } = 800;

    /// <summary>Height of the canvas in pixels.</summary>
    public double CanvasHeight { get; private set; } = 600;

    /// <summary>Whether the whole map is to be fitted in the canvas once it is drawn.</summary>
    public bool FitPending { get; set; }

    /// <summary>
    /// Whether a resized canvas fits the map again: true after the first fit, until the reader acts
    /// on the map or the host moves the view.
    /// </summary>
    public bool FollowsCanvas { get; set; }

    /// <summary>The view as it stands.</summary>
    public OmniMindMapViewState Current => new(PanX, PanY, Zoom);

    /// <summary>The map point at the middle of the canvas.</summary>
    public (double X, double Y) VisibleCenter => (((CanvasWidth / 2) - PanX) / Zoom, ((CanvasHeight / 2) - PanY) / Zoom);

    /// <summary>A zoom within the allowed range; a value that is not a number becomes 1.</summary>
    public static double ClampZoom(double zoom) =>
        double.IsFinite(zoom) ? Math.Clamp(zoom, OmniMindMap.MinZoom, OmniMindMap.MaxZoom) : 1;

    /// <summary>Where a map point sits on the canvas, in pixels.</summary>
    public (double Left, double Top) ToCanvas(double x, double y) => ((x * Zoom) + PanX, (y * Zoom) + PanY);

    /// <summary>Takes the canvas size the script read when it attached.</summary>
    public void SetCanvas(double width, double height)
    {
        CanvasWidth = width;
        CanvasHeight = height;
    }

    /// <summary>Takes a view the host set, which cancels a pending fit.</summary>
    public void Adopt(OmniMindMapViewState view)
    {
        if (view != Current)
        {
            FollowsCanvas = false;
        }

        PanX = view.PanX;
        PanY = view.PanY;
        Zoom = ClampZoom(view.Zoom);
        FitPending = false;
    }

    /// <summary>
    /// The reader has acted on the map, so the view is theirs from now on. Until then a resized canvas
    /// refits the map, because a container still laying out must not leave it cramped; after, it must
    /// not: selecting a node opens a properties panel beside the canvas and clearing the selection
    /// closes it, and each of those resizes used to refit, so every click zoomed the map in or out.
    /// The view then changes only by the wheel, a pinch, the zoom keys and the toolbar or menu actions.
    /// </summary>
    public void TakeFromCanvas() => FollowsCanvas = false;

    /// <summary>Moves the view, reporting it when it changed.</summary>
    public async Task SetAsync(double panX, double panY, double zoom)
    {
        zoom = ClampZoom(zoom);
        if (panX == PanX && panY == PanY && zoom == Zoom)
        {
            return;
        }

        PanX = panX;
        PanY = panY;
        Zoom = zoom;
        FollowsCanvas = false;
        owner.NotifyStateChanged();
        await owner.ViewStateChanged.InvokeAsync(Current);
    }

    /// <summary>
    /// Zooms and pans so every node is visible with a 60 pixel margin, never above 2x so a map of
    /// one node does not fill the screen. Returns whether the view changed.
    /// </summary>
    public async Task<bool> FitAsync()
    {
        if (model.Drawable.Count == 0)
        {
            return false;
        }

        double left = double.MaxValue, top = double.MaxValue, right = double.MinValue, bottom = double.MinValue;
        foreach (var node in model.Drawable)
        {
            var (width, height) = sizes.SizeOf(node);
            left = Math.Min(left, node.X - (width / 2));
            top = Math.Min(top, node.Y - (height / 2));
            right = Math.Max(right, node.X + (width / 2));
            bottom = Math.Max(bottom, node.Y + (height / 2));
        }

        var scaleX = (CanvasWidth - (FitPadding * 2)) / Math.Max(1, right - left);
        var scaleY = (CanvasHeight - (FitPadding * 2)) / Math.Max(1, bottom - top);
        var zoom = Math.Clamp(Math.Min(scaleX, scaleY), OmniMindMap.MinZoom, FitMaxZoom);
        var before = Current;
        await SetAsync(FitPadding - (left * zoom), FitPadding - (top * zoom), zoom);
        return before != Current;
    }

    /// <summary>Zooms by a factor around the middle of the canvas and announces the new zoom.</summary>
    public async Task ZoomByAsync(double factor)
    {
        var zoom = ClampZoom(Zoom * factor);
        var centerX = CanvasWidth / 2;
        var centerY = CanvasHeight / 2;
        await SetAsync(
            centerX - ((centerX - PanX) * (zoom / Zoom)),
            centerY - ((centerY - PanY) * (zoom / Zoom)),
            zoom);
        owner.Announce(owner.Text("MindMapAnnounceZoom", Math.Round(zoom * 100)));
    }

    /// <summary>Pans so a node sits in the middle of the canvas, keeping the zoom.</summary>
    public Task CenterOnAsync(OmniMindMapNode node) =>
        SetAsync((CanvasWidth / 2) - (node.X * Zoom), (CanvasHeight / 2) - (node.Y * Zoom), Zoom);

    /// <summary>Takes a new canvas size, fitting the map again while the view follows the canvas.</summary>
    public async Task ResizeAsync(double width, double height)
    {
        if (width <= 0 || height <= 0 || (width == CanvasWidth && height == CanvasHeight))
        {
            return;
        }

        CanvasWidth = width;
        CanvasHeight = height;
        if (FollowsCanvas)
        {
            await FitAsync();
            FollowsCanvas = true;
        }
    }
}
