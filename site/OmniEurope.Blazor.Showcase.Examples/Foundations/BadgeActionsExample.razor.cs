namespace OmniEurope.Blazor.Showcase.Examples.Foundations;

/// <summary>Badges that act: one-character badges, a badge that is a button counting its clicks and a badge that is a link.</summary>
public partial class BadgeActionsExample
{
    private int Clicks { get; set; }

    private void CountClick() => Clicks++;
}
