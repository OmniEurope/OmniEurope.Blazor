using System.Globalization;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>Five stat tiles: with an icon and a detail, clickable, bare, and two with a help mark (plain and clickable).</summary>
public partial class StatTilesGalleryExample
{
    private static string FillRate => 0.83.ToString("P0", CultureInfo.CurrentCulture);

    private static string Delays => 7.ToString(CultureInfo.CurrentCulture);

    private static string Passengers => 1250.ToString("N0", CultureInfo.CurrentCulture);

    private static string Cancelled => 3.ToString(CultureInfo.CurrentCulture);

    private static string Diverted => 2.ToString(CultureInfo.CurrentCulture);

    private int Clicks { get; set; }

    private int HelpClicks { get; set; }

    private void CountClick() => Clicks++;

    private void CountHelpClick() => HelpClicks++;
}
