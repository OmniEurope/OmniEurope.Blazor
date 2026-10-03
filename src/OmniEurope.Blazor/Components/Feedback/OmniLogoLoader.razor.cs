namespace OmniEurope.Blazor.Components;

/// <summary>
/// A loading indicator drawn with the site's own logo, the same on every site: the mark floats
/// gently up and down while something loads, and never turns (a slow oscillation, not a rotation).
/// It stands still for a user who asked the system for less motion. The loading state is announced
/// in words (<see cref="Label"/>); the logo is decorative.
/// </summary>
public partial class OmniLogoLoader
{
    /// <summary>The logo: an <c>img</c>, an inline <c>svg</c> or an <see cref="OmniIcon"/>, sized by the loader.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>How large the logo is drawn: 1.5rem small, 3rem medium (the default), 5rem large.</summary>
    [Parameter]
    public OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    /// <summary>
    /// Says that the logo animates itself (an animated SVG with its own keyframes, the product's
    /// loading animation): the loader then plays it as it is instead of floating it. Everything else is
    /// unchanged (status role, label, sizes, centring). Under reduced motion the loader holds the CSS
    /// animations of the logo still as well, so a site needs no rule of its own; animations written in
    /// SVG markup (SMIL, <c>&lt;animate&gt;</c>) are outside CSS and keep running. False by default: the
    /// logo floats.
    /// </summary>
    [Parameter]
    public bool AnimatedMark { get; set; }

    /// <summary>What is announced while it shows; the localized "Loading in progress" when null or blank.</summary>
    [Parameter]
    public string? Label { get; set; }

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Localize("LoadingLabel") : Label;
}
