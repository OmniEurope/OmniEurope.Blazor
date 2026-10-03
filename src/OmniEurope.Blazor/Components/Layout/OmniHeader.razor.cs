namespace OmniEurope.Blazor.Components;

/// <summary>
/// The application bar at the top of a layout: the brand (logo and name, optionally a link home), then
/// the host's content (navigation, actions, window controls).
/// </summary>
public partial class OmniHeader
{
    /// <summary>What the header holds after the brand.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Keeps the header at the top of the page while it scrolls.</summary>
    [Parameter]
    public bool Sticky { get; set; }

    /// <summary>The header's colour: the surface by default, or the theme's accent band.</summary>
    [Parameter]
    public OmniHeaderTone Tone { get; set; } = OmniHeaderTone.Surface;

    /// <summary>
    /// The application's name, in bold at the start of the header, after a sidebar toggle if the
    /// header holds one. Null or blank, the default, renders no name.
    /// </summary>
    [Parameter]
    public string? Brand { get; set; }

    /// <summary>
    /// The logo as an image, before <see cref="Brand"/>. Beside a name it is decorative
    /// (<c>aria-hidden</c>): the name says what it stands for. Without a name it carries one itself, the
    /// localized "Home" when it is a link (<see cref="BrandHref"/>), else the localized "Logo", so a
    /// logo-only brand is never a nameless link. Null or blank, the default, renders no logo.
    /// </summary>
    [Parameter]
    public string? BrandLogo { get; set; }

    /// <summary>
    /// Where the brand leads: given an address, the logo and the name are one link, typically to the
    /// application's home page. Null or blank, the default, leaves the brand a plain label.
    /// </summary>
    [Parameter]
    public string? BrandHref { get; set; }

    /// <summary>
    /// The sidebar toggle (an <see cref="OmniSidebarToggle"/>), drawn by the header at its very start,
    /// before the brand, on the axis of the icons of the sidebar rail (recette R-037). The
    /// header places it itself, so no wrapper of the application can shift it: give the toggle here and
    /// the brand through <see cref="Brand"/>, <see cref="BrandLogo"/> and <see cref="BrandHref"/>, never
    /// inside an element of the application's own (whose padding would move the toggle off the axis).
    /// </summary>
    [Parameter]
    public RenderFragment? SidebarToggle { get; set; }

    private bool HasBrand => !string.IsNullOrWhiteSpace(Brand) || !string.IsNullOrWhiteSpace(BrandLogo);

    private bool HasName => !string.IsNullOrWhiteSpace(Brand);

    // Beside the name, the logo repeats it: an empty alt hides it. Alone, it names the brand, or the
    // link it is: where the link leads.
    private string LogoAlt => HasName ? string.Empty : Localize(SafeBrandHref is null ? "HeaderLogo" : "HeaderHome");

    private string? LogoHidden => HasName ? "true" : null;

    private string? SafeBrandHref => string.IsNullOrWhiteSpace(BrandHref) ? null : OmniUriPolicy.EnsureSafe(BrandHref, nameof(BrandHref));
}
