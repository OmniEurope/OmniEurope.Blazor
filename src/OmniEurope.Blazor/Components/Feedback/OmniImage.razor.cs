namespace OmniEurope.Blazor.Components;

/// <summary>
/// An image (<c>img</c>) that never overflows its container and keeps its aspect ratio, decoded
/// asynchronously and loaded lazily by default.
/// </summary>
public partial class OmniImage
{
    /// <summary>The address of the image (<c>src</c>). Required.</summary>
    [Parameter, EditorRequired]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// The text alternative (<c>alt</c>). Required; an empty string marks a purely decorative image,
    /// which assistive technologies then skip.
    /// </summary>
    [Parameter, EditorRequired]
    public string Alt { get; set; } = string.Empty;

    /// <summary>
    /// The intrinsic width in pixels (<c>width</c>), which lets the browser reserve the space before the
    /// image loads. Null, the default, writes no attribute; a value of zero or less throws.
    /// </summary>
    [Parameter]
    public int? Width { get; set; }

    /// <summary>
    /// The intrinsic height in pixels (<c>height</c>), for the aspect ratio reserved before the image
    /// loads. Null, the default, writes no attribute; a value of zero or less throws.
    /// </summary>
    [Parameter]
    public int? Height { get; set; }

    /// <summary>When the browser loads the image; <see cref="OmniImageLoading.Lazy"/> by default.</summary>
    [Parameter]
    public OmniImageLoading Loading { get; set; } = OmniImageLoading.Lazy;

    /// <summary>
    /// How the image fills its box; <see cref="OmniImageFit.Natural"/> by default. With
    /// <see cref="OmniImageFit.Contain"/> or <see cref="OmniImageFit.Cover"/> the box is the one
    /// <see cref="Width"/> and <see cref="Height"/> give (or a host class sizes), and it keeps that
    /// height when the container narrows it: the picture is letterboxed or cropped, never stretched. A
    /// natural image instead follows its own ratio.
    /// </summary>
    [Parameter]
    public OmniImageFit Fit { get; set; }

    /// <summary>
    /// Rejects a <see cref="Width"/> or a <see cref="Height"/> of zero or less with an
    /// <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (Width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Width), Width, "The width must be positive when provided.");
        }

        if (Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Height), Height, "The height must be positive when provided.");
        }
    }
}
