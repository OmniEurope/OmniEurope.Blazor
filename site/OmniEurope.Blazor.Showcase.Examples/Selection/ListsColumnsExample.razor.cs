using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>A check box list of six short options in three columns.</summary>
public partial class ListsColumnsExample
{
    private IReadOnlyList<string> Levels { get; set; } = ["A1", "B1"];

    private static IReadOnlyList<OmniOption<string>> LevelOptions =>
    [
        new("A1", "A1"),
        new("A2", "A2"),
        new("B1", "B1"),
        new("B2", "B2"),
        new("C1", "C1"),
        new("C2", "C2")
    ];
}
