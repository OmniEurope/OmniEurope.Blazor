using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Foundations;

/// <summary>A single image: its source, the box asked of it, how it fits and when it loads are parameters.</summary>
public partial class ImageExample
{
    /// <summary>The illustration shown.</summary>
    [Parameter]
    public SampleImage Source { get; set; } = SampleImage.Portrait;

    /// <summary>The width and height asked of the image, or none.</summary>
    [Parameter]
    public ImageBox Box { get; set; } = ImageBox.Landscape;

    /// <summary>How the image fills a box of other proportions.</summary>
    [Parameter]
    public OmniImageFit Fit { get; set; } = OmniImageFit.Cover;

    /// <summary>When the browser loads the image.</summary>
    [Parameter]
    public OmniImageLoading Loading { get; set; } = OmniImageLoading.Lazy;

    /// <summary>The attributes the component writes, without the source, which is a long data address here.</summary>
    private string Markup
    {
        get
        {
            var size = SampleImages.WidthOf(Box) is { } width && SampleImages.HeightOf(Box) is { } height
                ? $" width=\"{width}\" height=\"{height}\""
                : string.Empty;
            return $"<img{size} loading=\"{Loading.ToString().ToLowerInvariant()}\" class=\"omni-image omni-image--{Fit.ToString().ToLowerInvariant()}\">";
        }
    }
}
