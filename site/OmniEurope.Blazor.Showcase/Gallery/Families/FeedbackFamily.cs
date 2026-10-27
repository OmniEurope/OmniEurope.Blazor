using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Feedback family of the gallery: its pages, in menu order.</summary>
internal static class FeedbackFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyFeedback", OmniIconName.Bell, typeof(FeedbackStrings),
    [
    ]);
}
