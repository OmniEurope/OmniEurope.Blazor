namespace OmniEurope.Blazor.Showcase.Examples.Navigation;

/// <summary>Fourteen tabs, enough for the strip to scroll.</summary>
public partial class TabsOverflowExample
{
    private static readonly IReadOnlyList<int> Numbers = [.. Enumerable.Range(1, 14)];

    private string? Selected { get; set; } = "document-1";
}
