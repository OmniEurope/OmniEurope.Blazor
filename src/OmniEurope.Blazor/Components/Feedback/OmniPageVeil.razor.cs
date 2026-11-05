namespace OmniEurope.Blazor.Components;

/// <summary>
/// The wait of a page: a veil over the content of the page, slightly see-through (<see cref="Opacity"/>),
/// in one constant colour of the theme (light and dark), with the application's own logo at its centre. It replaces the veils an
/// application would otherwise lay by hand on each page.
/// </summary>
/// <remarks>
/// <para>
/// Nothing animates. The package has no logo and no default animation: the application gives the mark
/// it wants, still or animated by itself, through <see cref="Logo"/> (free markup: an <c>img</c>, an
/// inline <c>svg</c>, an <see cref="OmniIcon"/>) or <see cref="LogoSource"/> (an image address), and
/// passes its animated logo the same way. Without either, the veil is a plain layer.
/// </para>
/// <para>
/// The veil shows only after 100 ms of waiting, so a short wait shows nothing at all and cannot flicker;
/// shown, it has its full <see cref="Opacity"/> from its first frame. The delay is the stylesheet's (<c>--omni-page-veil-delay</c>),
/// not a timer: the element exists from the moment <see cref="Busy"/> is true, hidden, and a wait that ends
/// within the delay removes it unseen. It covers the pointer; it does not trap the keyboard focus, which
/// is the application's to take (mark the content behind <c>inert</c> when the page must not be reachable).
/// </para>
/// <para>
/// The wait is announced as a polite status in words (<see cref="Label"/>, the localized "Loading in
/// progress" by default); the logo is decorative.
/// </para>
/// <para>
/// Declared inside an <see cref="OmniMain"/> (the default <see cref="Scope"/>), the veil covers that main
/// area and nothing else: the header and the sidebar stay visible and usable. It draws nothing where it is
/// declared; the main area draws it over its own frame (the whole visible area of a
/// <see cref="OmniMain.Scrollable"/> one, which does not scroll with the content; the main element of any
/// other, the logo kept in the visible part of the window), so no ancestor of the veil can capture it.
/// Anywhere else, or with <see cref="OmniPageVeilScope.Viewport"/>, it is drawn in place and fixed over the
/// window at <c>--omni-z-overlay</c>: above the page, the header and the sidebars, under the notifications,
/// windows, tooltips and the blocking connection overlay. Over a main area it sits above the content and
/// under the sidebar drawer.
/// </para>
/// </remarks>
public partial class OmniPageVeil : IDisposable
{
    private OmniMain? _host;

    /// <summary>
    /// True while the page waits: the veil then exists and shows after the delay. False, the default,
    /// renders nothing.
    /// </summary>
    [Parameter]
    public bool Busy { get; set; }

    /// <summary>
    /// The logo as free markup, centred on the veil: an <c>img</c>, an inline <c>svg</c> (its
    /// <c>currentColor</c> follows the theme text colour) or an <see cref="OmniIcon"/>, sized to
    /// <c>--omni-page-veil-logo-size</c> (6rem by default). It wins over <see cref="LogoSource"/> when both are given.
    /// Null, the default, draws no markup logo.
    /// </summary>
    [Parameter]
    public RenderFragment? Logo { get; set; }

    /// <summary>
    /// The logo as an image address (<c>src</c> of an <c>img</c>), for an application whose logo is a file.
    /// Ignored when <see cref="Logo"/> is given; null or blank, the default, draws no image. Like
    /// <c>OmniImage.Source</c> and the header logo, the address is written as given, without
    /// <c>OmniUriPolicy</c>: an image address runs no script, and a <c>data:image/...</c> logo stays possible.
    /// </summary>
    [Parameter]
    public string? LogoSource { get; set; }

    /// <summary>
    /// How much of the window the veil covers: the content of the page (the default), the whole window, or
    /// only the element it is declared in (<see cref="OmniPageVeilScope.Parent"/>).
    /// </summary>
    [Parameter]
    public OmniPageVeilScope Scope { get; set; }

    /// <summary>
    /// How opaque the background of the veil is, in percent: 85 by default, 100 hides the page entirely, 0 leaves
    /// it fully visible. Rounded to a step of 5 and kept within 0 and 100, the stylesheet having one class per
    /// step. Only the background is see-through; the logo stays fully drawn.
    /// </summary>
    [Parameter]
    public int Opacity { get; set; } = 85;

    /// <summary>What is announced while the veil is up; the localized "Loading in progress" when null or blank.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>The main area this veil is declared in, if any: the one that draws it for <see cref="OmniPageVeilScope.Content"/>.</summary>
    [CascadingParameter]
    private OmniMain? Main { get; set; }

    /// <summary>Raised when a parameter was set, so the element, drawn by the main area, draws again.</summary>
    internal event Action? Changed;

    /// <summary>The element of the veil, keyed by the veil so the main area that draws it keeps it across renders.</summary>
    internal RenderFragment Surface => builder =>
    {
        builder.OpenComponent<OmniPageVeilSurface>(0);
        builder.SetKey(this);
        builder.AddComponentParameter(1, nameof(OmniPageVeilSurface.Veil), this);
        builder.CloseComponent();
    };

    internal bool HasLogo => Logo is not null || !string.IsNullOrWhiteSpace(LogoSource);

    internal string EffectiveLabel => LocalizeOr(Label, "LoadingLabel");

    /// <summary>The element's classes: the viewport veil is the base; a veil held by a main area adds how that area frames it, and the opacity is one class per step.</summary>
    internal string RootClass => Css("omni-page-veil", FrameClass, $"omni-page-veil--alpha-{OpacityBucket}");

    private string? FrameClass => Scope == OmniPageVeilScope.Parent
        ? "omni-page-veil--parent"
        : _host is null ? null : _host.Scrollable ? "omni-page-veil--frame" : "omni-page-veil--flow";

    private int OpacityBucket => Math.Clamp((int)(Math.Round(Opacity / 5d, MidpointRounding.AwayFromZero) * 5), 0, 100);

    /// <summary>Hands the veil to its main area (or takes it back) and tells the element to draw again.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        var host = Scope == OmniPageVeilScope.Content ? Main : null;
        if (!ReferenceEquals(host, _host))
        {
            _host?.RemoveVeil(this);
            _host = host;
            _host?.AddVeil(this);
        }

        Changed?.Invoke();
    }

    /// <summary>Takes the veil back from its main area.</summary>
    public void Dispose()
    {
        _host?.RemoveVeil(this);
        _host = null;
    }
}
