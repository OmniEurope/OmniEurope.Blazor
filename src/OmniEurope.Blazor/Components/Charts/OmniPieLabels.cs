namespace OmniEurope.Blazor.Components;

/// <summary>What <see cref="OmniPieSeries.OutsideLabels"/> writes beside each slice, outside the disc.</summary>
public enum OmniPieLabels
{
    /// <summary>Nothing: the slices alone, their name and value in the hover text and the legend. The default.</summary>
    None,

    /// <summary>The name of the slice.</summary>
    Name,

    /// <summary>The name and the value of the slice (<see cref="OmniPieSeries.FormatValue"/>).</summary>
    NameAndValue,

    /// <summary>The name of the slice and its share of the whole, as a percentage.</summary>
    NameAndPercent
}
