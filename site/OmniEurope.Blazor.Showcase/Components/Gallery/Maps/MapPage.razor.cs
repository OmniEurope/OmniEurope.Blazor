using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Maps;

/// <summary>The map page: whether the map frames the whole route.</summary>
public partial class MapPage
{
    [Inject]
    private IStringLocalizer<MapsStrings> Text { get; set; } = default!;

    private bool Fit { get; set; } = true;
}
