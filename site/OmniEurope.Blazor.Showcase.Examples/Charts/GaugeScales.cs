namespace OmniEurope.Blazor.Showcase.Examples.Charts;

/// <summary>The bounds and step of each <see cref="GaugeScale"/>, for the examples and for the controls that set a value.</summary>
public static class GaugeScales
{
    /// <summary>The lowest value of a scale.</summary>
    /// <param name="scale">The scale.</param>
    /// <returns>Its minimum.</returns>
    public static double Minimum(GaugeScale scale) => scale == GaugeScale.Temperature ? -20 : 0;

    /// <summary>The highest value of a scale.</summary>
    /// <param name="scale">The scale.</param>
    /// <returns>Its maximum.</returns>
    public static double Maximum(GaugeScale scale) => scale switch
    {
        GaugeScale.Files => 500,
        GaugeScale.Temperature => 40,
        _ => 100
    };

    /// <summary>The gap between two values a control should offer on a scale.</summary>
    /// <param name="scale">The scale.</param>
    /// <returns>Its step.</returns>
    public static double Step(GaugeScale scale) => scale == GaugeScale.Files ? 10 : 1;
}
