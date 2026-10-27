using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>A multiple-choice card group drawn from options, one of them disabled.</summary>
public partial class SelectableCardsOptionsExample
{
    private IReadOnlyList<string> Classes { get; set; } = ["economy"];

    private static IReadOnlyList<OmniOption<string>> ClassOptions =>
    [
        new("economy", "Economy"),
        new("premium", "Premium"),
        new("business", "Business", Disabled: true)
    ];
}
