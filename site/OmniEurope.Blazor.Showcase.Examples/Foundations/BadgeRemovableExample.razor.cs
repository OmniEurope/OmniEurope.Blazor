namespace OmniEurope.Blazor.Showcase.Examples.Foundations;

/// <summary>A stateful chip list: removing a removable badge takes it out of the list.</summary>
public partial class BadgeRemovableExample
{
    private List<string> Destinations { get; } = ["Paris", "Rome", "Lisbon", "Oslo"];

    private void Remove(string destination) => Destinations.Remove(destination);
}
