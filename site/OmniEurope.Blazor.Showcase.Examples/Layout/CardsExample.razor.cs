using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>A card whose header, footer and body length are parameters.</summary>
public partial class CardsExample
{
    /// <summary>What the header area holds; none leaves the area out.</summary>
    [Parameter]
    public CardHeaderKind Header { get; set; } = CardHeaderKind.TitleAndBadge;

    /// <summary>What the footer area holds; none leaves the area out.</summary>
    [Parameter]
    public CardFooterKind Footer { get; set; } = CardFooterKind.Actions;

    /// <summary>Whether the body holds a few more paragraphs.</summary>
    [Parameter]
    public bool LongBody { get; set; }

    private string? Last { get; set; }

    private bool HasHeader => Header != CardHeaderKind.None;

    private bool HasFooter => Footer != CardFooterKind.None;

    private void Record(string action) => Last = action;
}
