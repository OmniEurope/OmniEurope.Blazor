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
    /// The logo: a short text (typically two letters) in a rounded square of the accent fill, before
    /// <see cref="Brand"/>. Decorative (<c>aria-hidden</c>), so the name says what it stands for.
    /// Null or blank, the default, renders no logo.
    /// </summary>
    [Parameter]
    public string? BrandMark { get; set; }

    private bool HasBrand => !string.IsNullOrWhiteSpace(Brand) || !string.IsNullOrWhiteSpace(BrandMark);
}
