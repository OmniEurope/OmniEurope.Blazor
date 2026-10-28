using System.Globalization;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>Sliders formatted as a percentage, without their value, disabled, and upright.</summary>
public partial class SliderGalleryExample
{
    private double Opacity { get; set; } = 0.8;

    private double Volume { get; set; } = 35;

    private double FrozenValue { get; set; } = 70;

    private double Temperature { get; set; } = 21.5;

    private static string FormatPercent(double value) => value.ToString("P0", CultureInfo.CurrentCulture);

    private static string FormatTemperature(double value) => $"{value.ToString("0.0", CultureInfo.CurrentCulture)} °C";
}
