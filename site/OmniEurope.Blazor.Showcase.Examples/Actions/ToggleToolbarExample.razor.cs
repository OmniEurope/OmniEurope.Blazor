namespace OmniEurope.Blazor.Showcase.Examples.Actions;

/// <summary>Four independent toggle buttons in a toolbar, with a count of the pressed ones.</summary>
public partial class ToggleToolbarExample
{
    private bool Favorite { get; set; } = true;

    private bool Alerts { get; set; }

    private bool Offline { get; set; }

    private bool Sync { get; set; } = true;

    private int PressedCount => new[] { Favorite, Alerts, Offline, Sync }.Count(pressed => pressed);
}
