using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Foundations;

/// <summary>The three fits, each on a portrait image in a landscape box and a landscape image in a portrait box.</summary>
public partial class ImageFitsExample
{
    private static readonly OmniImageFit[] Fits = Enum.GetValues<OmniImageFit>();

    private static string HintOf(OmniImageFit fit) => fit switch
    {
        OmniImageFit.Contain => "The whole image, with empty bands.",
        OmniImageFit.Cover => "The box filled, the image cropped.",
        _ => "No fitting: in an imposed box, the image is stretched."
    };
}
