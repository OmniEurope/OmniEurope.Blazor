using System.Globalization;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>A tile grid whose column count follows the width of the grid itself.</summary>
public partial class TileGridFluidExample
{
    private static string Tokens(double thousands) => $"{thousands.ToString("N1", CultureInfo.CurrentCulture)}k";
}
