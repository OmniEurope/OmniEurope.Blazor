namespace OmniEurope.Blazor.Components;

/// <summary>
/// A read-only view of a document, page by page: pictures of its pages, or its pages as HTML whose fields are
/// highlighted. A toolbar zooms, fits the width of the viewer and tells which page is in view.
/// </summary>
/// <remarks>
/// <para>
/// The component knows no document format. The host converts its document to <see cref="OmniDocumentPage"/> values,
/// each carrying exactly one source: the address of a picture, the bytes of a PNG, or HTML. HTML is sanitised again
/// with the package allow-list (see <see cref="SanitizerPolicy"/>): scripts, event handler attributes, styles and
/// <c>javascript:</c> addresses never reach the page. The only attribute added to the allow-list is
/// <c>data-oe-field</c>, which makes an element a field of the document.
/// </para>
/// <para>
/// A field is highlighted, takes the keyboard focus, shows its label in the package tooltip on hover and on focus,
/// and raises <see cref="OnFieldClick"/> with its tag on click and on Enter. Only HTML pages have fields: a picture of
/// a page has no coordinates to draw them at.
/// </para>
/// <para>
/// Zoom applies to pictures and HTML alike, from 25 % to 400 %. With <see cref="OmniDocumentFit.Width"/> the zoom is
/// computed from the width of the viewer by <c>omni-document-viewer.js</c>, which also tells which page is in view; the
/// script writes the sizes as custom properties, never as a style attribute, so the component runs under a strict CSP.
/// A host that serves pictures from a <c>data:</c> address (PNG bytes) must allow <c>data:</c> in <c>img-src</c>.
/// </para>
/// </remarks>
public partial class OmniDocumentViewer : IAsyncDisposable
{
    internal const double MinimumZoom = 0.25;
    internal const double MaximumZoom = 4;
    internal const double ZoomStep = 0.25;
    private const double Tolerance = 0.0001;

    private readonly string _generatedId = $"omni-document-viewer-{Guid.NewGuid():N}";
    private IReadOnlyList<DocumentPageView> _views = [];
    private IReadOnlyList<OmniDocumentPage>? _readPages;
    private OmniHtmlSanitizerPolicy? _hostPolicy;
    private OmniHtmlSanitizerPolicy _policy = DocumentViewerMarkup.BasePolicy;
    private bool _initialised;
    private double _zoom = 1;
    private double _observedZoom = 1;
    private OmniDocumentFit _fit = OmniDocumentFit.Width;
    private OmniDocumentFit _observedFit = OmniDocumentFit.Width;
    private int _current;
    private int _version;
    private ElementReference _viewport;
    private IJSObjectReference? _module;
    private Task<IJSObjectReference?>? _import;
    private DotNetObjectReference<DocumentViewerInteropBridge>? _reference;
    private OmniPackageTooltips? _tooltips;
    private bool _attached;
    private bool _disposed;
    private (double Zoom, bool Fitting, int Version) _pushed;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    /// <summary>
    /// The pages, in reading order. Each carries exactly one source (see <see cref="OmniDocumentPage"/>); a page that
    /// does not makes the component throw <see cref="ArgumentException"/>. The list is read again when the parameter is
    /// given another list, so replace it rather than changing it in place. Empty, the default, shows the localized
    /// "no page" text.
    /// </summary>
    [Parameter]
    public IReadOnlyList<OmniDocumentPage> Pages { get; set; } = [];

    /// <summary>
    /// The zoom, 1 for 100 %, kept between 0.25 and 4. With <see cref="Fit"/> set to <see cref="OmniDocumentFit.Width"/>
    /// it is the zoom the component computed, reported through <see cref="ZoomChanged"/> (bind it to read the fitted
    /// value); giving it another value then ends the fit and takes that zoom.
    /// </summary>
    [Parameter]
    public double Zoom { get; set; } = 1;

    /// <summary>Raised with the new zoom when the reader zooms in or out or presses fit width, and when the viewer is resized while fitting the width.</summary>
    [Parameter]
    public EventCallback<double> ZoomChanged { get; set; }

    /// <summary>
    /// How the pages are sized: to the width of the viewer (<see cref="OmniDocumentFit.Width"/>, the default) or at
    /// <see cref="Zoom"/>. The fit toolbar button sets it, and a zoom chosen on the toolbar ends it.
    /// </summary>
    [Parameter]
    public OmniDocumentFit Fit { get; set; } = OmniDocumentFit.Width;

    /// <summary>Raised with the new fit when the reader changes it with the toolbar: width when fit width is pressed, none when zooming ends it.</summary>
    [Parameter]
    public EventCallback<OmniDocumentFit> FitChanged { get; set; }

    /// <summary>Whether the toolbar is shown: zoom out, zoom in, fit width and the page indicator. True by default.</summary>
    [Parameter]
    public bool ShowToolbar { get; set; } = true;

    /// <summary>
    /// Whether the fields of the document are highlighted, focusable and clickable (the default). Fields exist on HTML
    /// pages only: a page given as a picture has no field coordinates, so nothing is highlighted on it. False draws
    /// the HTML pages as plain documents and never raises <see cref="OnFieldClick"/>.
    /// </summary>
    [Parameter]
    public bool HighlightFields { get; set; } = true;

    /// <summary>
    /// The label shown in the tooltip of each field, keyed by the tag of its <c>data-oe-field</c> attribute. A field whose
    /// tag is absent, or has a blank label, shows its tag. Null, the default, shows the tags.
    /// </summary>
    [Parameter]
    public IReadOnlyDictionary<string, string>? FieldLabels { get; set; }

    /// <summary>Raised with the tag of a field when it is clicked or when Enter is pressed on it. Never raised when <see cref="HighlightFields"/> is false.</summary>
    [Parameter]
    public EventCallback<string> OnFieldClick { get; set; }

    /// <summary>
    /// What the HTML pages may carry beyond the built-in allow-list (text, lists, tables, links to http, https, mailto and
    /// tel) and the <c>data-oe-field</c> attribute: extra elements, attributes and classes the host's converter writes.
    /// Merged with the default; it never allows a script, an event handler attribute or a style. A policy that
    /// names such an entry makes the component throw <see cref="ArgumentException"/>. Null, the default, adds nothing.
    /// </summary>
    [Parameter]
    public OmniHtmlSanitizerPolicy? SanitizerPolicy { get; set; }

    /// <summary>The accessible name of the pages area. Null, the default, is the localized "Document".</summary>
    [Parameter]
    public string? Label { get; set; }

    private string ViewportId => Id ?? _generatedId;

    private string EffectiveLabel => LocalizeOr(Label, "DocumentViewerLabel");

    private string ZoomText => _zoom.ToString("P0", CultureInfo.CurrentCulture);

    private string PageIndicator => Localize("DocumentViewerPageIndicator", _views[_current - 1].Page.Number, _views.Count);

    private string PageLabel(DocumentPageView view) => Localize("DocumentViewerPageLabel", view.Page.Number, _views.Count);

    private static string? Points(double? points) => points?.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>The zoom kept inside the range the viewer supports; a value that is not a number is 100 %.</summary>
    private static double ClampZoom(double zoom) => double.IsNaN(zoom) ? 1 : Math.Clamp(zoom, MinimumZoom, MaximumZoom);

    /// <summary>The next step of 25 % above or below the zoom, so that zooming from a fitted 87 % lands on 100 %.</summary>
    private static double NextZoom(double zoom, bool zoomIn)
    {
        var steps = zoom / ZoomStep;
        var target = zoomIn ? Math.Floor(steps + Tolerance) + 1 : Math.Ceiling(steps - Tolerance) - 1;
        return ClampZoom(Math.Round(target * ZoomStep, 2));
    }

    private RenderFragment Content(AngleSharp.Dom.IElement body) => (RenderTreeBuilder builder) =>
        DocumentViewerMarkup.Render(builder, body, HighlightFields, FieldLabels, this, RaiseFieldAsync);

    private Task RaiseFieldAsync(string tag) => OnFieldClick.InvokeAsync(tag);

    /// <summary>
    /// Reads the pages again when the list or the policy changed, adopts a changed <see cref="Zoom"/> or
    /// <see cref="Fit"/>, and keeps the current page inside the list.
    /// </summary>
    /// <exception cref="ArgumentNullException"><see cref="Pages"/> is null.</exception>
    /// <exception cref="ArgumentException">A page does not carry exactly one source, or <see cref="SanitizerPolicy"/> names an entry no policy can allow.</exception>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(Pages);
        AdoptZoomAndFit();
        var policyChanged = !_initialised || !ReferenceEquals(_hostPolicy, SanitizerPolicy);
        if (policyChanged)
        {
            _policy = DocumentViewerMarkup.PolicyFor(SanitizerPolicy);
            _hostPolicy = SanitizerPolicy;
        }

        if (policyChanged || !ReferenceEquals(_readPages, Pages))
        {
            _views = [.. Pages.Select(page => DocumentPageView.Read(
                page ?? throw new ArgumentException("Pages holds a null page.", nameof(Pages)), nameof(Pages), _policy))];
            _readPages = Pages;
            _version++;
        }

        _initialised = true;
        _current = _views.Count == 0 ? 0 : Math.Clamp(_current, 1, _views.Count);
    }

    // A zoom the host changes is taken at once and ends the fit; a fit it changes is taken as given, after the zoom, so
    // that giving both together gives the fit the last word. The first values are the starting state.
    private void AdoptZoomAndFit()
    {
        if (!_initialised)
        {
            _zoom = ClampZoom(Zoom);
            _fit = Fit;
        }
        else
        {
            // Equals, not ==: a NaN the host gave once must not hide the zoom it gives next.
            if (!Zoom.Equals(_observedZoom))
            {
                var requested = ClampZoom(Zoom);
                if (Math.Abs(requested - _zoom) > Tolerance)
                {
                    _zoom = requested;
                    _fit = OmniDocumentFit.None;
                }
            }

            if (Fit != _observedFit)
            {
                _fit = Fit;
            }
        }

        _observedZoom = Zoom;
        _observedFit = Fit;
    }

    private Task ZoomInAsync() => ZoomToAsync(NextZoom(_zoom, zoomIn: true));

    private Task ZoomOutAsync() => ZoomToAsync(NextZoom(_zoom, zoomIn: false));

    private async Task ZoomToAsync(double zoom)
    {
        var endsFit = _fit == OmniDocumentFit.Width;
        _zoom = zoom;
        _fit = OmniDocumentFit.None;
        if (endsFit)
        {
            await FitChanged.InvokeAsync(OmniDocumentFit.None);
        }

        await ZoomChanged.InvokeAsync(_zoom);
    }

    private async Task FitWidthAsync()
    {
        if (_fit == OmniDocumentFit.Width)
        {
            return;
        }

        _fit = OmniDocumentFit.Width;
        await FitChanged.InvokeAsync(OmniDocumentFit.Width);
    }

    /// <summary>
    /// What the script reports: the zoom it computed to fit the width (taken only while fitting, so a report in flight
    /// cannot undo a zoom the reader just chose) and the page in view.
    /// </summary>
    private async Task OnViewChangedAsync(double zoom, int page)
    {
        if (_disposed)
        {
            return;
        }

        var zoomChanged = false;
        if (_fit == OmniDocumentFit.Width && Math.Abs(ClampZoom(zoom) - _zoom) > Tolerance)
        {
            _zoom = ClampZoom(zoom);
            zoomChanged = true;
        }

        if (page >= 1 && page <= _views.Count)
        {
            _current = page;
        }

        if (zoomChanged)
        {
            await ZoomChanged.InvokeAsync(_zoom);
        }

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// On the first render turns the package tooltips on, so the label of a field shows on hover and on focus; after
    /// every render sends the script what it needs to lay the pages out: attached once, then the zoom, the fit and
    /// the pages whenever they changed. A lost circuit is ignored.
    /// </summary>
    /// <param name="firstRender">True on the first render of the component.</param>
    /// <returns>A task that completes once the script has what it needs.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (firstRender)
            {
                _tooltips = new OmniPackageTooltips(JavaScript);
                await _tooltips.InstallAsync();
            }

            await SynchronizeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the viewer with it.
        }
    }

    private async Task SynchronizeAsync()
    {
        _import ??= OmniScriptModule.ImportAsync(JavaScript, OmniModules.DocumentViewer, () => _disposed);
        _module = await _import;
        if (_module is null)
        {
            return;
        }

        var fitting = _fit == OmniDocumentFit.Width;
        if (!_attached)
        {
            _attached = true;
            _pushed = (_zoom, fitting, _version);
            _reference = DotNetObjectReference.Create(new DocumentViewerInteropBridge(OnViewChangedAsync));
            await _module.InvokeVoidAsync("attach", _viewport, _reference, _zoom, fitting, MinimumZoom, MaximumZoom);
            return;
        }

        if (_pushed != (_zoom, fitting, _version))
        {
            _pushed = (_zoom, fitting, _version);
            await _module.InvokeVoidAsync("update", _viewport, _zoom, fitting);
        }
    }

    /// <summary>Detaches the script, which releases its observer and listeners, and the package tooltips.</summary>
    /// <returns>A task that completes once the script is released; a lost circuit is ignored.</returns>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        try
        {
            if (_tooltips is not null)
            {
                await _tooltips.DisposeAsync();
            }

            // A script still loading is released by its loader when it arrives (see SynchronizeAsync).
            if (_module is not null)
            {
                await _module.InvokeVoidAsync("detach", _viewport);
                await _module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the module with it.
        }

        _reference?.Dispose();
        GC.SuppressFinalize(this);
    }
}
