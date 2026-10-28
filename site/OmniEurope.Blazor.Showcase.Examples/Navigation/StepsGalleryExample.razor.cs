namespace OmniEurope.Blazor.Showcase.Examples.Navigation;

/// <summary>Steps on the first, a middle and the last step, and with a disabled step.</summary>
public partial class StepsGalleryExample
{
    private static readonly IReadOnlyList<Sample> Samples =
    [
        new("first", "On the first step", 0, false),
        new("middle", "On a middle step", 1, false),
        new("last", "On the last step", 2, false),
        new("disabled", "With a disabled step", 0, true)
    ];

    private sealed record Sample(string Key, string Caption, int Value, bool DecisionDisabled);
}
