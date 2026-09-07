namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class NavigationDemo
{
    private static readonly string[] TabKeys = ["resume", "pieces", "historique"];

    private string? Tab { get; set; } = "resume";

    private int Step { get; set; } = 1;
}
