namespace OmniEurope.Blazor.Internal;

/// <summary>Viewport geometry and rendered row heights reported by the grid script in one round trip.</summary>
internal sealed class GridViewportSnapshot
{
    public double ScrollTop { get; set; }

    public double ViewportHeight { get; set; }

    public double ScrollHeight { get; set; }

    public IReadOnlyList<GridRowMeasurement>? Rows { get; set; }
}
