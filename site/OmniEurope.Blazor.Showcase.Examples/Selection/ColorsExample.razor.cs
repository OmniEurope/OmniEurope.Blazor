using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>A colour picker bound two ways, with the channels and the readable text colour read back from the value.</summary>
public partial class ColorsExample
{
    /// <summary>The bound colour, <c>#RRGGBB</c>; a host can set it, as a program would.</summary>
    [Parameter]
    public string Color { get; set; } = "#165DFF";

    /// <summary>Raised with the new colour when the picker changes it.</summary>
    [Parameter]
    public EventCallback<string> ColorChanged { get; set; }

    /// <summary>Whether the hexadecimal code is written next to the swatch.</summary>
    [Parameter]
    public bool ShowValue { get; set; } = true;

    /// <summary>Whether the picker is greyed out and cannot be opened.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    private string CurrentColor
    {
        get => Color;
        set
        {
            Color = value;
            _ = ColorChanged.InvokeAsync(value);
        }
    }

    private (int Red, int Green, int Blue) Channels => (Channel(1), Channel(3), Channel(5));

    private string RgbText => $"rgb({Channels.Red}, {Channels.Green}, {Channels.Blue})";

    /// <summary>Relative luminance in the WCAG sense: above 0.179, dark text reads better than light text.</summary>
    private string BrightnessText
    {
        get
        {
            var (red, green, blue) = Channels;
            var luminance = (0.2126 * Linear(red)) + (0.7152 * Linear(green)) + (0.0722 * Linear(blue));
            var kind = luminance > 0.179 ? "dark" : "light";
            return $"{kind} (luminance {luminance.ToString("0.000", CultureInfo.CurrentCulture)})";
        }
    }

    // The picker only gives validated #RRGGBB, so reading the channels has no other form to fear.
    private int Channel(int start) =>
        Color is { Length: 7 } && int.TryParse(Color.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;

    private static double Linear(int channel)
    {
        var scaled = channel / 255d;
        return scaled <= 0.03928 ? scaled / 12.92 : Math.Pow((scaled + 0.055) / 1.055, 2.4);
    }
}
