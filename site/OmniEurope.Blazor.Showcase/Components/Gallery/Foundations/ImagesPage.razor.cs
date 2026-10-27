using OmniEurope.Blazor.Showcase.Examples.Foundations;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Foundations;

/// <summary>The Images page: the settings of one image, then the three fits, the two loading modes and the sources.</summary>
public partial class ImagesPage
{
    private static readonly OmniImageFit[] AllFits = Enum.GetValues<OmniImageFit>();

    private static readonly OmniImageLoading[] AllLoadings = Enum.GetValues<OmniImageLoading>();

    [Inject]
    private IStringLocalizer<FoundationsStrings> Text { get; set; } = default!;

    private SampleImage Source { get; set; } = SampleImage.Portrait;

    private ImageBox Box { get; set; } = ImageBox.Landscape;

    private OmniImageFit Fit { get; set; } = OmniImageFit.Cover;

    private OmniImageLoading Loading { get; set; } = OmniImageLoading.Lazy;

    private IReadOnlyList<OmniOption<SampleImage>> SourceOptions =>
        [.. SampleImages.All.Select(sample => new OmniOption<SampleImage>(sample, SourceLabel(sample)))];

    private IReadOnlyList<OmniOption<ImageBox>> BoxOptions =>
        [.. SampleImages.Boxes.Select(box => new OmniOption<ImageBox>(box, BoxLabel(box)))];

    private IReadOnlyList<OmniOption<OmniImageFit>> FitOptions =>
        [.. AllFits.Select(fit => new OmniOption<OmniImageFit>(fit, FitLabel(fit)))];

    private IReadOnlyList<OmniOption<OmniImageLoading>> LoadingOptions =>
        [.. AllLoadings.Select(loading => new OmniOption<OmniImageLoading>(loading, LoadingLabel(loading)))];

    private string SourceLabel(SampleImage sample) => sample switch
    {
        SampleImage.Portrait => Text["ImagesSourcePortrait"],
        SampleImage.Landscape => Text["ImagesSourceLandscape"],
        SampleImage.Vector => Text["ImagesSourceVector"],
        _ => Text["ImagesSourceSquare"]
    };

    private string BoxLabel(ImageBox box) => box switch
    {
        ImageBox.Landscape => Text["ImagesBoxLandscape"],
        ImageBox.Portrait => Text["ImagesBoxPortrait"],
        ImageBox.Square => Text["ImagesBoxSquare"],
        _ => Text["ImagesBoxIntrinsic"]
    };

    private string FitLabel(OmniImageFit fit) => fit switch
    {
        OmniImageFit.Contain => Text["ImagesFitContain"],
        OmniImageFit.Cover => Text["ImagesFitCover"],
        _ => Text["ImagesFitNatural"]
    };

    private string LoadingLabel(OmniImageLoading loading) => loading == OmniImageLoading.Eager
        ? Text["ImagesLoadingEager"]
        : Text["ImagesLoadingLazy"];
}
