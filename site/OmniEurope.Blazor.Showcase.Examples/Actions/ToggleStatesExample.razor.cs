namespace OmniEurope.Blazor.Showcase.Examples.Actions;

/// <summary>A toggle button released and pressed, enabled, disabled and busy.</summary>
public partial class ToggleStatesExample
{
    private static readonly ToggleState[] States =
    [
        new("Released", false, false, false),
        new("Pressed", true, false, false),
        new("Disabled, released", false, true, false),
        new("Disabled, pressed", true, true, false),
        new("Busy, released", false, false, true),
        new("Busy, pressed", true, false, true)
    ];

    private sealed record ToggleState(string Caption, bool Pressed, bool Disabled, bool Busy);
}
