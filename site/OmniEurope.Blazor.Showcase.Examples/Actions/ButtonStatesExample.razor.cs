namespace OmniEurope.Blazor.Showcase.Examples.Actions;

/// <summary>Every button variant at rest, disabled, busy and icon-only.</summary>
public partial class ButtonStatesExample
{
    private int ClickCount { get; set; }

    private void CountClick() => ClickCount++;
}
