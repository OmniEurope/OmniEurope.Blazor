using System.Globalization;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>A tile grid mixing stat tiles, navigation tiles, cards and a selectable card, some of them clickable.</summary>
public partial class TileGridExample
{
    private static string Reset => $"Resets at {new TimeOnly(18, 0).ToString("t", CultureInfo.CurrentCulture)}";

    private bool TileChoice { get; set; } = true;

    private int CardClicks { get; set; }

    private static string Tokens(double thousands) => $"{thousands.ToString("N1", CultureInfo.CurrentCulture)}k";
}
