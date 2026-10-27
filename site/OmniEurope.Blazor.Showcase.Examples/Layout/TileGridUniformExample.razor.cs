using System.Globalization;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>A tile grid with UniformSize: about the same width and exactly the same height for every tile.</summary>
public partial class TileGridUniformExample
{
    private static string Tokens => $"{12.4.ToString("N1", CultureInfo.CurrentCulture)}k";
}
