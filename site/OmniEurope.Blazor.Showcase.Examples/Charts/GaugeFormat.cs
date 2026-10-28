namespace OmniEurope.Blazor.Showcase.Examples.Charts;

/// <summary>How the text in the middle of a gauge example writes the value.</summary>
public enum GaugeFormat
{
    /// <summary>The number alone.</summary>
    Raw,

    /// <summary>The share of the scale the value covers.</summary>
    Share,

    /// <summary>The number followed by the unit of the scale.</summary>
    Unit
}
