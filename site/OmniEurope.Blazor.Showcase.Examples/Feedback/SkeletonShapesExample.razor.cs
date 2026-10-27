using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Feedback;

/// <summary>Every shape of a skeleton, then the line counts of the text shape.</summary>
public partial class SkeletonShapesExample
{
    private static readonly OmniSkeletonShape[] Shapes = [OmniSkeletonShape.Text, OmniSkeletonShape.Rectangle, OmniSkeletonShape.Circle];

    private static readonly int[] LineSamples = [1, 2, 5, 10];
}
