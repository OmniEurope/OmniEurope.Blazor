namespace OmniEurope.Blazor.Components;

/// <summary>
/// The wait of a whole page: an opaque veil fixed over the viewport, in one constant colour of the
/// theme (light and dark), with the application's own logo at its centre. It replaces the veils an
/// application would otherwise lay by hand on each page.
/// </summary>
/// <remarks>
/// <para>
/// Nothing animates. The package has no logo and no default animation: the application gives the mark
/// it wants, still or animated by itself, through <see cref="Logo"/> (free markup: an <c>img</c>, an
/// inline <c>svg</c>, an <see cref="OmniIcon"/>) or <see cref="LogoSource"/> (an image address), and
/// passes its animated logo the same way. Without either, the veil is a plain opaque page.
/// </para>
/// <para>
/// The veil shows only after 100 ms of waiting, so a short wait shows nothing at all and cannot flicker;
/// shown, it is fully opaque from its first frame. The delay is the stylesheet's (<c>--omni-page-veil-delay</c>),
/// not a timer: the element exists from the moment <see cref="Busy"/> is true, hidden, and a wait that ends
/// within the delay removes it unseen. It covers the pointer; it does not trap the keyboard focus, which
/// is the application's to take (mark the content behind <c>inert</c> when the page must not be reachable).
/// </para>
/// <para>
/// The wait is announced as a polite status in words (<see cref="Label"/>, the localized "Loading in
/// progress" by default); the logo is decorative. The veil sits at <c>--omni-z-overlay</c>: above the
/// page, the header and the sidebars, under the notifications, windows, tooltips and the blocking
/// connection overlay.
/// </para>
/// </remarks>
public partial class OmniPageVeil
{
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
    /// Ignored when <see cref="Logo"/> is given; null or blank, the default, draws no image.
    /// </summary>
    [Parameter]
    public string? LogoSource { get; set; }

    /// <summary>What is announced while the veil is up; the localized "Loading in progress" when null or blank.</summary>
    [Parameter]
    public string? Label { get; set; }

    private bool HasLogo => Logo is not null || !string.IsNullOrWhiteSpace(LogoSource);

    private string EffectiveLabel => LocalizeOr(Label, "LoadingLabel");
}
