namespace OmniEurope.Blazor.Showcase.Examples.Overlays;

/// <summary>An overflow menu whose Insert and Change type entries unfold a list of their own.</summary>
public partial class OverflowMenuSubmenuExample
{
    private string? LastAction { get; set; }

    private void Choose(string action) => LastAction = action;
}
