using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>Card groups drawn from options with a check box, in columns, and without a check box.</summary>
public partial class SelectableCardsLayoutExample
{
    private IReadOnlyList<string> Features { get; set; } = ["cache"];

    private static IReadOnlyList<OmniOption<string>> FeatureOptions =>
    [
        new("cache", "Dependency cache"),
        new("artifacts", "Build artifacts"),
        new("gpu", "GPU runner", Disabled: true)
    ];
}
