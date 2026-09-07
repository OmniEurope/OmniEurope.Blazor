namespace OmniEurope.Blazor.Internal;

/// <summary>Measured height of one rendered grid row, keyed by its absolute row index.</summary>
internal sealed class GridRowMeasurement
{
    public int Index { get; set; }

    public double Height { get; set; }
}
