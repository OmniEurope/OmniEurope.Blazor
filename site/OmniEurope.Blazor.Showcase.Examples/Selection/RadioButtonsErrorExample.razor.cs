using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>A required deployment-strategy list that shows its error until an option is chosen.</summary>
public partial class RadioButtonsErrorExample
{
    private static readonly IReadOnlyList<OmniOption<string>> StrategyOptions =
    [
        new("rolling", "Rolling update"),
        new("blue-green", "Blue-green"),
        new("canary", "Canary", Disabled: true)
    ];

    /// <summary>None is chosen at first, so that the error shows.</summary>
    private string? Strategy { get; set; }

    private void Reset() => Strategy = null;
}
