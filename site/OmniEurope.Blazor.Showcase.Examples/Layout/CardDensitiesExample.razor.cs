using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>The same form in three cards, one per density.</summary>
public partial class CardDensitiesExample
{
    private string? ServerName { get; set; } = "srv-paris-02";

    private string? ServerId { get; set; } = "a9f3cb0";

    private string? ServerZone { get; set; } = "eu-west";

    private bool LockedRestore { get; set; } = true;

    private bool EmailAlerts { get; set; } = true;
}
