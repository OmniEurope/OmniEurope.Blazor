using System.Globalization;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Layout;

/// <summary>The Stat tiles page: a tile shaped by its own settings, then the tiles of facts and the tile grids.</summary>
public partial class StatTilesPage
{
    [Inject]
    private IStringLocalizer<LayoutStrings> Text { get; set; } = default!;

    private string TileValue { get; set; } = 42.ToString(CultureInfo.CurrentCulture);

    private string TileTitle { get; set; } = "Destinations";

    private bool ShowDetail { get; set; } = true;

    private bool ShowIcon { get; set; } = true;

    private bool Clickable { get; set; } = true;
}
