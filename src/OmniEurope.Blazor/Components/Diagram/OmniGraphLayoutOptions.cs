namespace OmniEurope.Blazor.Components;

/// <summary>How <see cref="OmniGraphLayout"/> spaces a layered drawing.</summary>
public sealed record OmniGraphLayoutOptions
{
    /// <summary>Which way the layers follow each other; left to right by default.</summary>
    public OmniGraphDirection Direction { get; init; } = OmniGraphDirection.LeftToRight;

    /// <summary>Gap between two boxes of the same layer; 40 by default.</summary>
    public double NodeSpacing { get; init; } = 40;

    /// <summary>Gap between two layers, from the thickest box of one to the next; 80 by default.</summary>
    public double LayerSpacing { get; init; } = 80;
}
