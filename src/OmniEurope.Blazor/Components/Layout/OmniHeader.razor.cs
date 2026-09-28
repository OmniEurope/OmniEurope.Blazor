namespace OmniEurope.Blazor.Components;

public partial class OmniHeader
{
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

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
    /// The logo as an image, before <see cref="Brand"/>, decorative
    /// (<c>aria-hidden</c>): the name says what it stands for. Null or blank, the default, renders no logo.
    /// </summary>
    [Parameter]
    public string? BrandLogo { get; set; }

    /// <summary>
    /// Where the brand leads: given an address, the logo and the name are one link, typically to the
    /// application's home page. Null or blank, the default, leaves the brand a plain label.
    /// </summary>
    [Parameter]
    public string? BrandHref { get; set; }

    private bool HasBrand => !string.IsNullOrWhiteSpace(Brand) || !string.IsNullOrWhiteSpace(BrandLogo);

    private string? SafeBrandHref => string.IsNullOrWhiteSpace(BrandHref) ? null : OmniUriPolicy.EnsureSafe(BrandHref, nameof(BrandHref));
}
