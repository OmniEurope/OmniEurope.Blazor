namespace OmniEurope.Blazor.Showcase.Examples.Actions;

/// <summary>A split button at rest, disabled, busy, with an unavailable entry and without a menu.</summary>
public partial class SplitButtonStatesExample
{
    private string? LastAction { get; set; }

    private int ActionCount { get; set; }

    private void Choose(string action)
    {
        LastAction = action;
        ActionCount++;
    }
}
