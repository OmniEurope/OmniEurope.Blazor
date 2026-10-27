using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Foundations;

/// <summary>Every tone in every fill, and a row of counters.</summary>
public partial class BadgeMatrixExample
{
    private static readonly OmniTone[] Tones = Enum.GetValues<OmniTone>();

    private static readonly OmniFill[] Fills = Enum.GetValues<OmniFill>();

    // One digit, then two, three, four and five: the badge widens with the count and never goes under a square.
    private static int CounterFor(OmniTone tone) => (int)Math.Pow(5, (int)tone);
}
