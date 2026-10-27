using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Scheduling family of the gallery: its pages, in menu order.</summary>
internal static class SchedulingFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyScheduling", OmniIconName.Timer, typeof(SchedulingStrings),
    [
    ]);
}
