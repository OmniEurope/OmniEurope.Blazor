using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Feedback;

/// <summary>The Skeletons page: the settings of one skeleton, then every shape, line count and composition.</summary>
public partial class SkeletonsPage
{
    [Inject]
    private IStringLocalizer<FeedbackStrings> Text { get; set; } = default!;

    private OmniSkeletonShape Shape { get; set; } = OmniSkeletonShape.Text;

    /// <summary>The slider works in doubles; the skeleton takes an integer.</summary>
    private double LineCountValue { get; set; } = 3;

    private int LineCount => (int)LineCountValue;

    private bool Named { get; set; } = true;

    private bool Loaded { get; set; }

    private IReadOnlyList<OmniOption<OmniSkeletonShape>> ShapeOptions =>
    [
        new(OmniSkeletonShape.Text, Text["SkeletonsShapeText"]),
        new(OmniSkeletonShape.Rectangle, Text["SkeletonsShapeRectangle"]),
        new(OmniSkeletonShape.Circle, Text["SkeletonsShapeCircle"])
    ];
}
