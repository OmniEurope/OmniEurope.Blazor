using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Media;

/// <summary>The media stage page: how many participants the scene shows.</summary>
public partial class MediaStagePage
{
    [Inject]
    private IStringLocalizer<MediaStrings> Text { get; set; } = default!;

    private double Count { get; set; } = 7;

    private void ChooseCount(double value) => Count = Math.Round(value);
}
