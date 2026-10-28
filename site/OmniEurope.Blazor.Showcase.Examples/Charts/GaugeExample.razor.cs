using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Charts;

/// <summary>An arc gauge whose value, scale, value format and colour are parameters.</summary>
public partial class GaugeExample
{
    /// <summary>The value the arc draws, between the bounds of the scale.</summary>
    [Parameter]
    public double Value { get; set; } = 72;

    /// <summary>The bounds of the scale.</summary>
    [Parameter]
    public GaugeScale Scale { get; set; } = GaugeScale.Percent;

    /// <summary>What the text in the middle of the gauge shows.</summary>
    [Parameter]
    public GaugeFormat Format { get; set; } = GaugeFormat.Unit;

    /// <summary>The rank of the arc colour in the eight-colour chart palette.</summary>
    [Parameter]
    public int ColorIndex { get; set; } = 1;

    private double Minimum => GaugeScales.Minimum(Scale);

    private double Maximum => GaugeScales.Maximum(Scale);

    private string Unit => Scale switch
    {
        GaugeScale.Files => "{0} files",
        GaugeScale.Temperature => "{0} °C",
        _ => "{0} %"
    };

    private string FormatGaugeValue(double value) => Format switch
    {
        GaugeFormat.Raw => value.ToString("0", CultureInfo.CurrentCulture),
        GaugeFormat.Share => ((value - Minimum) / (Maximum - Minimum)).ToString("P0", CultureInfo.CurrentCulture),
        _ => string.Format(CultureInfo.CurrentCulture, Unit, value.ToString("0", CultureInfo.CurrentCulture))
    };
}
