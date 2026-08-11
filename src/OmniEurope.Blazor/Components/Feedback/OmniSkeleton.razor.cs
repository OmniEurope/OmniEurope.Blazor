namespace OmniEurope.Blazor.Components;

public partial class OmniSkeleton
{
    [Parameter]
    public OmniSkeletonShape Shape { get; set; }

    [Parameter]
    public int LineCount { get; set; } = 1;

    [Parameter]
    public string? Label { get; set; }

    private int NormalizedLineCount => Shape == OmniSkeletonShape.Text
        ? Math.Clamp(LineCount, 1, 10)
        : 1;
}
