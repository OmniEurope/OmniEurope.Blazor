using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Feedback family of the gallery: its pages, in menu order.</summary>
internal static class FeedbackFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyFeedback", OmniIconName.Bell, typeof(FeedbackStrings),
    [
        new("alerts", "AlertsTitle", "AlertsDescription", OmniIconName.Warning,
            ["OmniAlert"],
            [
                "Feedback/AlertExample.razor", "Feedback/AlertExample.razor.cs",
                "Feedback/AlertGalleryExample.razor", "Feedback/AlertGalleryExample.razor.cs",
                "Feedback/AlertSample.cs"
            ]),
        new("progress", "ProgressTitle", "ProgressDescription", OmniIconName.Timer,
            ["OmniProgressBar", "OmniLogoLoader", "OmniStatusBadge", "OmniStatusStrip"],
            [
                "Feedback/ProgressExample.razor", "Feedback/ProgressExample.razor.cs",
                "Feedback/ProgressSamplesExample.razor", "Feedback/ProgressSamplesExample.razor.cs",
                "Feedback/ProgressStepsExample.razor", "Feedback/ProgressStepsExample.razor.cs",
                "Feedback/LogoLoaderExample.razor", "Feedback/LogoLoaderExample.razor.cs",
                "Feedback/StatusExample.razor", "Feedback/StatusExample.razor.cs",
                "Feedback/ProgressSample.cs"
            ]),
        new("skeletons", "SkeletonsTitle", "SkeletonsDescription", OmniIconName.Rows,
            ["OmniSkeleton"],
            [
                "Feedback/SkeletonExample.razor", "Feedback/SkeletonExample.razor.cs",
                "Feedback/SkeletonShapesExample.razor", "Feedback/SkeletonShapesExample.razor.cs",
                "Feedback/SkeletonLinesExample.razor", "Feedback/SkeletonLinesExample.razor.cs",
                "Feedback/SkeletonComposedExample.razor", "Feedback/SkeletonComposedExample.razor.cs"
            ])
    ]);
}
