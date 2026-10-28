using System.Globalization;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>Three stat tiles: with an icon and a detail, clickable, and bare.</summary>
public partial class StatTilesGalleryExample
{
    private static string FillRate => 0.83.ToString("P0", CultureInfo.CurrentCulture);

    private static string Delays => 7.ToString(CultureInfo.CurrentCulture);

    private static string Passengers => 1250.ToString("N0", CultureInfo.CurrentCulture);

    private int Clicks { get; set; }

    private void CountClick() => Clicks++;
}
