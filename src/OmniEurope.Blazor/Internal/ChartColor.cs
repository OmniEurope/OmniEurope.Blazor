namespace OmniEurope.Blazor.Internal;

/// <summary>The palette of eight chart colours every chart part and the Gantt chart share.</summary>
internal static class ChartColor
{
    /// <summary>Number of colours in the palette; a larger index wraps around.</summary>
    internal const int Count = 8;

    /// <summary>The palette slot of a colour index, negative indexes counted by their magnitude.</summary>
    internal static int Slot(int colorIndex) => Math.Abs(colorIndex % Count);

    /// <summary>The class that paints an element with colour <paramref name="colorIndex"/> of the palette.</summary>
    internal static string Class(int colorIndex) => $"omni-chart-color-{Slot(colorIndex)}";
}
