using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>The grid line choices the grid examples offer in their selector.</summary>
public static class DataGridLineOptions
{
    /// <summary>Every line setting, with its English label.</summary>
    public static IReadOnlyList<OmniOption<OmniDataGridLines>> All { get; } =
    [
        new(OmniDataGridLines.None, "None"),
        new(OmniDataGridLines.Horizontal, "Horizontal"),
        new(OmniDataGridLines.Vertical, "Vertical"),
        new(OmniDataGridLines.Both, "Both")
    ];
}
